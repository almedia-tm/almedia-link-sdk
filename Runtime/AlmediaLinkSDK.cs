using System;
using System.Collections.Generic;
using UnityEngine;
using AlmediaLink.Bridge;
using AlmediaLink.Models;
using AlmediaLink.UI;

namespace AlmediaLink
{
    public static class AlmediaLinkSDK
    {
        public static string Version => "1.1.0";

        /// <summary>
        /// The SDK's current lifecycle status. Reads <see cref="AlmediaStatus.NotInitialized"/>
        /// until the native bridge reports its first terminal status, then tracks every
        /// transition. Use this from late-joining components (UI that mounts after the
        /// first <see cref="OnStatusChanged"/> has already fired) to recover the latest
        /// status without missing a beat.
        /// </summary>
        public static AlmediaStatus CurrentStatus => _almediaStatus;

        public static event Action<AlmediaStatus> OnStatusChanged;
        public static event Action<string> OnLinkCompleted;
        public static event Action<List<AlmediaNotification>> OnNotificationsReceived;
        public static event Action<AlmediaError> OnErrorOccurred;

        /// <summary>
        /// Fires when an SDK screen (linking webview, reward hub, offer) is now on top of the
        /// game - pause gameplay here. Fires when the native container commits to presenting,
        /// before the page loads. A call that opens nothing (wrong state, missing URL, a screen
        /// already open, an <see cref="Engage"/> no-op) fires neither this nor
        /// <see cref="OnScreenDismissed"/>. System-browser linking is not reported - only the
        /// link callbacks fire for it. Every emission is followed by exactly one matching
        /// <see cref="OnScreenDismissed"/>.
        /// </summary>
        public static event Action<AlmediaScreen> OnScreenPresented;

        /// <summary>
        /// Fires when the screen reported by <see cref="OnScreenPresented"/> is gone - resume
        /// gameplay here. Exactly one per <see cref="OnScreenPresented"/>. Carries how the screen
        /// was dismissed (<see cref="InAppScreenResultType.Completed"/>, Cancelled, or Failed with
        /// an <see cref="AlmediaError"/>). Native completes its sync-on-close before this fires;
        /// for webview linking it fires before the outcome link callbacks
        /// (<see cref="OnLinkCompleted"/> etc.).
        /// </summary>
        public static event Action<AlmediaScreen, InAppScreenResult> OnScreenDismissed;

        public static event Action<AlmediaLogLevel, string> OnLog
        {
            add => AlmediaLog.OnLog += value;
            remove => AlmediaLog.OnLog -= value;
        }

        private static INativeBridge _bridge;
        private static AlmediaStatus _almediaStatus = AlmediaStatus.NotInitialized;
        private static ResolvedAlmediaLinkConfig _activeConfig;

        /// <summary>
        /// Boots the SDK with the given configuration and starts the status lifecycle.
        /// The result is delivered asynchronously through <see cref="OnStatusChanged"/> -
        /// do not call other SDK methods until the first transition out of
        /// <see cref="AlmediaStatus.NotInitialized"/> fires.
        /// </summary>
        /// <remarks>
        /// Safe to call more than once. A call with the same effective configuration is a
        /// no-op that preserves <see cref="CurrentStatus"/>; a call with a different
        /// configuration tears down the current session and re-initializes. Missing
        /// integration key, unsupported platform, and bridge-construction failures are
        /// reported through <see cref="OnErrorOccurred"/> rather than thrown.
        /// </remarks>
        public static void Initialize(AlmediaLinkConfig config)
        {
            config ??= new AlmediaLinkConfig();

            AlmediaLog.Info($"Initializing SDK v{Version}");

            var resolved = config.Resolve();

            if (!resolved.IsValid)
            {
                AlmediaLog.Error("Integration key is missing. Cannot initialize.");
                OnErrorOccurred?.Invoke(new AlmediaError(
                    AlmediaErrorCode.InvalidConfiguration,
                    "Integration key is missing."));
                return;
            }

            // A repeat call with the same effective configuration is a no-op: leave the
            // current status untouched. The native layer dedupes a same-config init without
            // re-emitting a status, so resetting here would strand us at NotInitialized.
            if (_activeConfig != null && _activeConfig.Equals(resolved))
            {
                AlmediaLog.Info("Already initialized with the same configuration; ignoring.");
                return;
            }

            _almediaStatus = AlmediaStatus.NotInitialized;

            try
            {
                _bridge = NativeBridgeFactory.Create();
            }
            catch (PlatformNotSupportedException)
            {
                AlmediaLog.Error($"AlmediaLink is not supported on {Application.platform}. SDK will be inactive.");
                OnErrorOccurred?.Invoke(new AlmediaError(
                    AlmediaErrorCode.InvalidConfiguration,
                    $"Platform {Application.platform} is not supported."));
                return;
            }
            catch (Exception e)
            {
                AlmediaLog.Error($"Unexpected failure creating native bridge: {e.GetType().Name}: {e.Message}");
                OnErrorOccurred?.Invoke(new AlmediaError(
                    AlmediaErrorCode.Unexpected,
                    $"Native bridge creation failed: {e.Message}"));
                return;
            }
            
            SubscribeToBridge();

            var request = InitializeRequest.FromResolvedConfig(resolved);
            var json = JsonUtility.ToJson(request);
            _bridge.Initialize(json);

            // Record only after a successful dispatch so a failed init (e.g. unsupported
            // platform above) leaves the cache null and a retry is not wrongly swallowed.
            _activeConfig = resolved;

            AlmediaLinkUIManager.Initialize();

            AlmediaLog.Info("SDK initialized. Waiting for native callback.");
        }

        /// <summary>Opens the account-linking flow.</summary>
        /// <remarks>No-op (with a warning) until the SDK is ready - the first
        /// <see cref="OnStatusChanged"/> must have fired.</remarks>
        public static void StartLinking(PlacementType placement = PlacementType.Popup)
        {
            if (!GuardReady()) return;
            AlmediaLog.Info($"Starting link flow (placement: {placement})");
            _bridge.StartLinking(placement);
        }

        /// <summary>Opens the reward progression screen in a webview.</summary>
        /// <remarks>
        /// No-op (with a warning) until the SDK is ready. Native additionally requires a linked user
        /// with a live progression URL; when that is absent the call is a no-op there, reported through
        /// <see cref="OnLog"/>, and neither lifecycle event fires. When the screen appears,
        /// <see cref="OnScreenPresented"/> fires with <see cref="AlmediaScreen.RewardHub"/> and its
        /// dismissal is delivered via <see cref="OnScreenDismissed"/>.
        /// </remarks>
        public static void ShowRewardHub()
        {
            if (!GuardReady()) return;
            AlmediaLog.Info("Showing reward hub");
            _bridge.ShowRewardHub();
        }

        /// <summary>Opens the offer screen in a webview.</summary>
        /// <remarks>
        /// No-op (with a warning) until the SDK is ready. Native additionally requires a linked user
        /// with a live offer URL - that URL can come and go between syncs, so a call that worked
        /// earlier may later be a no-op there, reported through <see cref="OnLog"/>, and neither
        /// lifecycle event fires. When the screen appears, <see cref="OnScreenPresented"/> fires with
        /// <see cref="AlmediaScreen.Offer"/> and its dismissal is delivered via
        /// <see cref="OnScreenDismissed"/>.
        /// </remarks>
        public static void ShowOffer()
        {
            if (!GuardReady()) return;
            AlmediaLog.Info("Showing offer");
            _bridge.ShowOffer();
        }

        /// <summary>
        /// Context-aware entry point. Forwards to native, which routes on the player's state -
        /// starts linking when eligible, opens the reward hub when linked, and no-ops (with a log
        /// through <see cref="OnLog"/>) otherwise.
        /// </summary>
        /// <remarks>No-op (with a warning) until the SDK is ready.</remarks>
        public static void Engage()
        {
            if (!GuardReady()) return;
            AlmediaLog.Info("Engage requested");
            _bridge.Engage();
        }

        /// <summary>Issues a one-shot notification fetch.</summary>
        /// <remarks>No-op (with a warning) until the SDK is ready - the first
        /// <see cref="OnStatusChanged"/> must have fired.</remarks>
        public static void FetchNotifications()
        {
            if (!GuardReady()) return;
            _bridge.FetchNotifications();
        }

        /// <summary>Resumes the notification polling loop.</summary>
        /// <remarks>No-op (with a warning) until the SDK is ready - the first
        /// <see cref="OnStatusChanged"/> must have fired.</remarks>
        public static void StartNotificationPolling()
        {
            if (!GuardReady()) return;
            _bridge.StartNotificationPolling();
        }

        public static void StopNotificationPolling()
        {
            if (!GuardInitialized()) return;
            _bridge.StopNotificationPolling();
        }

        internal static void ContinueWithATT()
        {
            if (!GuardInitialized()) return;
            _bridge.ContinueWithATT();
        }

        internal static void SkipATT()
        {
            if (!GuardInitialized()) return;
            _bridge.SkipATT();
        }

        internal static void TrackPromoLoad(PromoState state)
        {
            if (!GuardInitialized()) return;
            _bridge.TrackPromoLoad(state);
        }

        internal static void TrackPromoClick(PromoState state)
        {
            if (!GuardInitialized()) return;
            _bridge.TrackPromoClick(state);
        }

        internal static void TrackPopupShow()
        {
            if (!GuardInitialized()) return;
            _bridge.TrackPopupShow();
        }

        internal static void TrackPopupDismiss()
        {
            if (!GuardInitialized()) return;
            _bridge.TrackPopupDismiss();
        }

        internal static void TrackPopupCtaClick()
        {
            if (!GuardInitialized()) return;
            _bridge.TrackPopupCtaClick();
        }

        internal static void TrackNotificationsShow(string notificationIdsJson)
        {
            if (!GuardInitialized()) return;
            _bridge.TrackNotificationsShow(notificationIdsJson);
        }

        internal static void TrackNotificationClick(string notificationId)
        {
            if (!GuardInitialized()) return;
            _bridge.TrackNotificationClick(notificationId);
        }

        internal static void TrackATTPreliminaryShow()
        {
            if (!GuardInitialized()) return;
            _bridge.TrackATTPreliminaryShow();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnDomainReload()
        {
            OnStatusChanged = null;
            OnLinkCompleted = null;
            OnNotificationsReceived = null;
            OnErrorOccurred = null;
            OnScreenPresented = null;
            OnScreenDismissed = null;
            AlmediaLog.ClearSubscribers();
            AlmediaLinkUIManager.Cleanup();
            _bridge = null;
            _almediaStatus = AlmediaStatus.NotInitialized;
            _activeConfig = null;
        }

        private static void HandleStatusChanged(StatusChangedResponse response)
        {
            if (!StatusExtensions.TryFromString(response.status, out _almediaStatus))
                AlmediaLog.Warning($"Unrecognized status '{response.status}' from native; treating as NotInitialized.");
            AlmediaLog.Info($"Status changed: {_almediaStatus}");
            OnStatusChanged?.Invoke(_almediaStatus);
        }

        private static void HandleLinkCompleted(LinkCompletedResponse response)
        {
            AlmediaLog.Info($"Link completed at {response.linkedAt}");
            OnLinkCompleted?.Invoke(response.linkedAt);
        }

        private static void HandleNotificationsReceived(NotificationsReceivedResponse response)
        {
            if (response.notifications == null || response.notifications.Length == 0) return;
            AlmediaLog.Debug($"Received {response.notifications.Length} notification(s)");
            
            var list = new List<AlmediaNotification>(response.notifications.Length);
            
            foreach (var item in response.notifications)
            {
                list.Add(AlmediaNotification.FromNotificationItem(item));
            }
            
            OnNotificationsReceived?.Invoke(list);
        }

        private static void HandleErrorOccurred(ErrorCallbackResponse response)
        {
            AlmediaLog.Error($"Error from native: {response.code} - {response.message}");
            OnErrorOccurred?.Invoke(AlmediaError.FromCallback(response));
        }

        private static void HandleScreenPresented(AlmediaScreen screen)
        {
            AlmediaLog.Info($"Screen presented: {screen}");
            OnScreenPresented?.Invoke(screen);
        }

        private static void HandleScreenDismissed(AlmediaScreen screen, ScreenDismissedResponse response)
        {
            var result = InAppScreenResult.FromResponse(response);
            AlmediaLog.Info($"Screen dismissed: {screen} ({result.Type})");
            OnScreenDismissed?.Invoke(screen, result);
        }

        private static bool GuardInitialized()
        {
            if (_bridge == null)
            {
                AlmediaLog.Warning("SDK not initialized. Call Initialize() first.");
                return false;
            }
            return true;
        }

        // Initialize is fire-and-forget: the bridge exists immediately, but the SDK is only
        // usable once the first status callback arrives. Operations whose effect depends on a
        // resolved status guard on this; native still enforces the status-specific rules.
        private static bool GuardReady()
        {
            if (!GuardInitialized()) return false;
            if (_almediaStatus == AlmediaStatus.NotInitialized)
            {
                AlmediaLog.Warning("SDK not ready yet. Wait for the first OnStatusChanged before calling SDK methods.");
                return false;
            }
            return true;
        }

        private static void SubscribeToBridge()
        {
            UnsubscribeFromBridge();
            AlmediaLinkBridge.StatusChanged += HandleStatusChanged;
            AlmediaLinkBridge.LinkCompleted += HandleLinkCompleted;
            AlmediaLinkBridge.NotificationsReceived += HandleNotificationsReceived;
            AlmediaLinkBridge.ErrorOccurred += HandleErrorOccurred;
            AlmediaLinkBridge.ScreenPresented += HandleScreenPresented;
            AlmediaLinkBridge.ScreenDismissed += HandleScreenDismissed;
        }

        private static void UnsubscribeFromBridge()
        {
            AlmediaLinkBridge.StatusChanged -= HandleStatusChanged;
            AlmediaLinkBridge.LinkCompleted -= HandleLinkCompleted;
            AlmediaLinkBridge.NotificationsReceived -= HandleNotificationsReceived;
            AlmediaLinkBridge.ErrorOccurred -= HandleErrorOccurred;
            AlmediaLinkBridge.ScreenPresented -= HandleScreenPresented;
            AlmediaLinkBridge.ScreenDismissed -= HandleScreenDismissed;
        }
    }
}
