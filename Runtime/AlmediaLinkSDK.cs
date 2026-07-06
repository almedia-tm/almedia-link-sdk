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
        public static string Version => "1.0.1";

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

        internal static void TrackPromoClick()
        {
            if (!GuardInitialized()) return;
            _bridge.TrackPromoClick();
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

        private static void HandleShowATTPrePrompt()
        {
            AlmediaLog.Info("Native requested ATT pre-prompt.");
            AlmediaLinkUIManager.ShowATTPrePrompt();
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
            AlmediaLinkBridge.ShowATTPrePromptRequested += HandleShowATTPrePrompt;
        }

        private static void UnsubscribeFromBridge()
        {
            AlmediaLinkBridge.StatusChanged -= HandleStatusChanged;
            AlmediaLinkBridge.LinkCompleted -= HandleLinkCompleted;
            AlmediaLinkBridge.NotificationsReceived -= HandleNotificationsReceived;
            AlmediaLinkBridge.ErrorOccurred -= HandleErrorOccurred;
            AlmediaLinkBridge.ShowATTPrePromptRequested -= HandleShowATTPrePrompt;
        }
    }
}
