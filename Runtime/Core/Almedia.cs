using System;
using System.Collections.Generic;
using UnityEngine;
using AlmediaSDK.Bridge;

namespace AlmediaSDK
{
    public static class Almedia
    {
        public static string Version => "1.3.0";

        /// <summary>
        /// The player's current status. Reads <see cref="AlmediaStatus.NotInitialized"/> until the
        /// native bridge reports the first status. Never null. A component that loads after
        /// <see cref="OnStatusChanged"/> fired reads the latest status here.
        /// </summary>
        public static AlmediaStatus Status => _status;

        /// <summary>
        /// The latest progress snapshot, or <c>null</c> before the first one arrives and after native
        /// clears it with the message stream token. The SDK keeps it in memory only. A scene that
        /// loads after <see cref="OnProgressUpdated"/> fired can read it. It is current before that
        /// event fires.
        /// </summary>
        public static AlmediaProgress Progress => _progress;

        /// <summary>
        /// Fires with the new status whenever <see cref="Status"/> changes, including a change inside a
        /// case, such as a linked player losing the reward hub. <see cref="Status"/> already holds the
        /// new value.
        /// </summary>
        public static event Action<AlmediaStatus> OnStatusChanged;
        public static event Action<string> OnLinkCompleted;
        public static event Action<List<AlmediaNotification>> OnNotificationsReceived;

        /// <summary>
        /// Fires when the backend instructs the game to grant in-game rewards. Credit
        /// the player and celebrate here. One event per grant: a grant is an atomic
        /// bundle of one or more rewards granted together (<see cref="AlmediaInGameRewardGrant.Rewards"/>),
        /// and three grants arriving at once raise three events.
        /// </summary>
        /// <remarks>
        /// Delivery is at-least-once. The server-to-server
        /// reward postback remains the authoritative record. Each grant has a unique
        /// <see cref="AlmediaInGameRewardGrant.Id"/> that a redelivery repeats, so deduplicate
        /// on it when a repeat credit matters to your economy.
        /// </remarks>
        public static event Action<AlmediaInGameRewardGrant> OnInGameRewardGrantRequested;

        /// <summary>
        /// Fires when <see cref="Progress"/> changes: a newer snapshot, or <c>null</c> when native
        /// clears the snapshot with the message stream token. The accessor already holds the new
        /// value. A handler can read the argument or the accessor. Render the progress UI here.
        /// </summary>
        public static event Action<AlmediaProgress> OnProgressUpdated;

        /// <summary>
        /// Fires when the server reports a completed task. The event is historical and best-effort.
        /// The task is not always in <see cref="Progress"/>, and the event does not update the
        /// snapshot. A replay can deliver it twice. Deduplicate on <see cref="AlmediaTaskCompletion.Id"/>.
        /// </summary>
        public static event Action<AlmediaTaskCompletion> OnTaskCompleted;

        /// <summary>
        /// Fires when the server reports a balance change. The event is historical and best-effort.
        /// The balance can differ from <see cref="Progress"/>, and the event does not update the
        /// snapshot. A replay can deliver it twice. Deduplicate on <see cref="AlmediaBalanceChange.Id"/>.
        /// </summary>
        public static event Action<AlmediaBalanceChange> OnBalanceChanged;

        public static event Action<AlmediaError> OnErrorOccurred;

        /// <summary>
        /// Fires when an SDK screen (linking webview, reward hub, offer) is now on top of the
        /// game - pause gameplay here. Fires when the native container commits to presenting,
        /// before the page loads. A call that opens nothing (wrong state, missing URL, a screen
        /// already open) fires neither this nor <see cref="OnScreenDismissed"/>. System-browser linking is not reported - only the
        /// link callbacks fire for it. Every emission is followed by exactly one matching
        /// <see cref="OnScreenDismissed"/>.
        /// </summary>
        public static event Action<AlmediaScreen> OnScreenPresented;

        /// <summary>
        /// Fires when the screen reported by <see cref="OnScreenPresented"/> is gone - resume
        /// gameplay here. Exactly one per <see cref="OnScreenPresented"/>. Carries how the screen
        /// was dismissed (<see cref="AlmediaInAppScreenResult.Completed"/>, Cancelled, or Failed with
        /// an <see cref="AlmediaError"/>). Native completes its sync-on-close before this fires;
        /// for webview linking it fires before the outcome link callbacks
        /// (<see cref="OnLinkCompleted"/> etc.).
        /// </summary>
        public static event Action<AlmediaScreen, AlmediaInAppScreenResult> OnScreenDismissed;

        public static event Action<AlmediaLogLevel, string> OnLog
        {
            add => AlmediaLog.OnLog += value;
            remove => AlmediaLog.OnLog -= value;
        }

        private static INativeBridge _bridge;
        private static AlmediaStatus _status = new AlmediaStatus.NotInitialized();
        private static LegacyStatusSnapshot _legacy = LegacyStatusSnapshot.Initial;
        private static AlmediaProgress _progress;
        private static ResolvedAlmediaConfig _activeConfig;
        private static bool _raisingInitializeError;

        /// <summary>Raised after every successful <see cref="Initialize"/>. Subscriptions survive the domain-reload reset.</summary>
        internal static event Action Initialized;

        /// <summary>Raised by the domain-reload reset before the state is cleared. Subscriptions survive it.</summary>
        internal static event Action Shutdown;

        /// <summary>The values the 1.x surface reports. Current before any status event fires.</summary>
        internal static LegacyStatusSnapshot Legacy => _legacy;

        /// <summary>Raised in the cases 1.x raises OnStatusChanged, after <see cref="OnStatusChanged"/>.</summary>
        internal static event Action<LegacyStatus> LegacyStatusChanged;

        /// <summary>Raised in the cases 1.x raises OnScreenAvailabilityChanged, after <see cref="LegacyStatusChanged"/>.</summary>
        internal static event Action<LegacyScreenAvailability> LegacyAvailabilityChanged;

        /// <summary>Presents the link popup for <see cref="ShowLink"/>. Set by the UI assembly; survives the domain-reload reset.</summary>
        internal static Action LinkPopupPresenter;

        internal static bool IsInitialized => _activeConfig != null;

        /// <summary>
        /// Boots the SDK with the given configuration and starts the status lifecycle.
        /// The result is delivered asynchronously through <see cref="OnStatusChanged"/> -
        /// do not call other SDK methods until the first transition out of
        /// <see cref="AlmediaStatus.NotInitialized"/> fires.
        /// </summary>
        /// <remarks>
        /// Safe to call more than once. A call with the same effective configuration is a
        /// no-op that preserves <see cref="Status"/>. A call with a different configuration
        /// resets <see cref="Status"/> to <see cref="AlmediaStatus.NotInitialized"/>, raising
        /// <see cref="OnStatusChanged"/>, and re-initializes. Missing
        /// integration key, unsupported platform, and bridge-construction failures are
        /// reported through <see cref="OnErrorOccurred"/> rather than thrown.
        /// </remarks>
        public static void Initialize(AlmediaConfig config)
        {
            if (_raisingInitializeError)
            {
                AlmediaLog.Warning("Initialize was called from an OnErrorOccurred handler that Initialize raised. Ignoring this call.");
                return;
            }

            config ??= new AlmediaConfig();

            AlmediaLog.Info($"Initializing SDK v{Version}");

            ResolvedAlmediaConfig resolved;
            try
            {
                resolved = config.Resolve();
            }
            catch (Exception e)
            {
                AlmediaLog.Error($"Configuration could not be resolved: {e.GetType().Name}: {e.Message}");
                RaiseInitializeError(new AlmediaError(
                    AlmediaErrorCode.InvalidConfiguration,
                    "Configuration could not be resolved."));
                return;
            }

            if (!resolved.IsValid)
            {
                AlmediaLog.Error("Integration key is missing. Cannot initialize.");
                RaiseInitializeError(new AlmediaError(
                    AlmediaErrorCode.InvalidConfiguration,
                    "Integration key is missing."));
                return;
            }

#if UNITY_EDITOR
            WarnAboutOversizedMeta(resolved);
#endif

            // A repeat call with the same effective configuration is a no-op: leave the
            // current status untouched. The native layer dedupes a same-config init without
            // re-emitting a status, so resetting here would strand us at NotInitialized.
            if (_activeConfig != null && _activeConfig.Equals(resolved))
            {
                AlmediaLog.Info("Already initialized with the same configuration; ignoring.");
                return;
            }

            var previousStatus = _status;
            _status = new AlmediaStatus.NotInitialized();
            _legacy = LegacyStatusSnapshot.Initial;
            _progress = null;

            try
            {
                if (!TryCreateBridge()) return;

                SubscribeToBridge();

                var request = InitializeRequest.FromResolvedConfig(resolved);
                var json = JsonUtility.ToJson(request);
                _bridge.Initialize(json);

                // Record only after a successful dispatch so a failed init (e.g. unsupported
                // platform above) leaves the cache null and a retry is not wrongly swallowed.
                _activeConfig = resolved;
            }
            finally
            {
                // The 1.x events must not fire for this reset.
                if (previousStatus != _status)
                    EventDispatch.Raise(nameof(OnStatusChanged), OnStatusChanged, _status);
            }

            EventDispatch.Raise(nameof(Initialized), Initialized);

            AlmediaLog.Info("SDK initialized. Waiting for native callback.");
        }

        private static bool TryCreateBridge()
        {
            try
            {
                _bridge = NativeBridgeFactory.Create();
                return true;
            }
            catch (PlatformNotSupportedException)
            {
                AlmediaLog.Error($"Almedia SDK is not supported on {Application.platform}. SDK will be inactive.");
                RaiseInitializeError(new AlmediaError(
                    AlmediaErrorCode.InvalidConfiguration,
                    $"Platform {Application.platform} is not supported."));
                return false;
            }
            catch (Exception e)
            {
                AlmediaLog.Error($"Unexpected failure creating native bridge: {e.GetType().Name}: {e.Message}");
                RaiseInitializeError(new AlmediaError(
                    AlmediaErrorCode.Unexpected,
                    $"Native bridge creation failed: {e.Message}"));
                return false;
            }
        }

        private static void RaiseInitializeError(AlmediaError error)
        {
            _raisingInitializeError = true;
            try
            {
                EventDispatch.Raise(nameof(OnErrorOccurred), OnErrorOccurred, error);
            }
            finally
            {
                _raisingInitializeError = false;
            }
        }

#if UNITY_EDITOR
        // Oversized meta is still forwarded: dropping it is the natives' call, and on a
        // device they warn about it themselves.
        private static void WarnAboutOversizedMeta(ResolvedAlmediaConfig config)
        {
            var oversized = config.OversizedMetaKeys();
            if (oversized.Count == 0) return;

            AlmediaLog.Warning(
                $"Dropped from the linking link - over {ResolvedAlmediaConfig.MetaMaxBytes} UTF-8 bytes: " +
                string.Join(", ", oversized));
        }
#endif

        /// <summary>
        /// Presents the link popup to a player who can link. The popup's button starts linking. The
        /// popup comes from the Link Popup slot in Almedia > Settings, or from the deprecated
        /// LinkPopupOverride while that slot is empty. With neither assigned, linking starts directly.
        /// </summary>
        /// <remarks>
        /// No-op (with a warning) until the SDK is ready. Does nothing, with a log, unless
        /// <see cref="Status"/> is <see cref="AlmediaStatus.Eligible"/>, and while a link popup is open.
        /// </remarks>
        public static void ShowLink()
        {
            if (!GuardReady()) return;
            if (!(_status is AlmediaStatus.Eligible))
            {
                AlmediaLog.Info($"ShowLink is a no-op in status {_status}");
                return;
            }

            AlmediaLog.Info("ShowLink requested");
            if (LinkPopupPresenter != null)
            {
                LinkPopupPresenter();
                return;
            }

            AlmediaLog.Warning("ShowLink: the link popup UI is not loaded; linking starts directly.");
            StartLinking(AlmediaPlacementType.Popup);
        }

        /// <summary>
        /// Opens the account-linking flow directly, without the link popup. Prefer
        /// <see cref="ShowLink"/>. Call this from the button of a popup that does not use
        /// LinkPopupController.
        /// </summary>
        /// <remarks>No-op (with a warning) until the SDK is ready - the first
        /// <see cref="OnStatusChanged"/> must have fired.</remarks>
        public static void StartLinking() => StartLinking(AlmediaPlacementType.Popup);

        /// <summary>
        /// Opens the account-linking flow directly and reports where it started from. Prefer
        /// <see cref="ShowLink"/>. A null <paramref name="placement"/> reads as
        /// <see cref="AlmediaPlacementType.Popup"/>.
        /// </summary>
        /// <remarks>No-op (with a warning) until the SDK is ready - the first
        /// <see cref="OnStatusChanged"/> must have fired.</remarks>
        public static void StartLinking(AlmediaPlacementType placement)
        {
            if (!GuardReady()) return;
            placement = placement ?? AlmediaPlacementType.Popup;
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
        [Obsolete("Use ShowLink() while the player can link and ShowRewardHub() once linked.", true)]
        public static void Engage() => EngageCore();

        internal static void EngageCore()
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetOnDomainReload()
        {
            OnStatusChanged = null;
            LegacyStatusChanged = null;
            LegacyAvailabilityChanged = null;
            OnLinkCompleted = null;
            OnNotificationsReceived = null;
            OnInGameRewardGrantRequested = null;
            OnProgressUpdated = null;
            OnTaskCompleted = null;
            OnBalanceChanged = null;
            OnErrorOccurred = null;
            OnScreenPresented = null;
            OnScreenDismissed = null;
            AlmediaLog.ClearSubscribers();
            EventDispatch.Raise(nameof(Shutdown), Shutdown);
            _bridge = null;
            _status = new AlmediaStatus.NotInitialized();
            _legacy = LegacyStatusSnapshot.Initial;
            _progress = null;
            _activeConfig = null;
        }

        private static void HandleStatusChanged(StatusChangedResponse response)
        {
            var status = AlmediaStatus.FromWire(response, out bool recognized);
            if (!recognized)
                AlmediaLog.Warning($"Unrecognized status '{response.status}' from native; treating as NotInitialized.");
            var legacy = LegacyStatusSnapshot.FromWire(response);

            bool statusChanged = !status.IsEquivalentTo(_status);
            bool legacyStatusChanged = legacy.Status != _legacy.Status || legacy.Reason != _legacy.Reason;
            bool legacyAvailabilityChanged = legacy.Availability != _legacy.Availability;

            // Both surfaces are current before any event fires: a handler on either may read both.
            _status = status;
            _legacy = legacy;

            if (statusChanged)
                EventDispatch.Raise(nameof(OnStatusChanged), OnStatusChanged, status);

            if (legacyStatusChanged)
            {
                AlmediaLog.Info(legacy.Reason == null
                    ? $"Status changed: {legacy.Status}"
                    : $"Status changed: {legacy.Status} (reason: {legacy.Reason})");

                EventDispatch.Raise(nameof(LegacyStatusChanged), LegacyStatusChanged, legacy.Status);
            }

            if (legacyAvailabilityChanged)
            {
                AlmediaLog.Info($"Screen availability changed: {legacy.Availability}");

                EventDispatch.Raise(nameof(LegacyAvailabilityChanged), LegacyAvailabilityChanged, legacy.Availability);
            }
        }

        private static void HandleLinkCompleted(LinkCompletedResponse response)
        {
            AlmediaLog.Info($"Link completed at {response.linkedAt}");
            EventDispatch.Raise(nameof(OnLinkCompleted), OnLinkCompleted, response.linkedAt);
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
            
            EventDispatch.Raise(nameof(OnNotificationsReceived), OnNotificationsReceived, list);
        }

        private static void HandleInGameRewardGrantRequested(InGameRewardGrantResponse response)
        {
            if (string.IsNullOrEmpty(response.id))
            {
                AlmediaLog.Warning("Dropping in-game reward grant with no id.");
                return;
            }
            if (response.rewards == null || response.rewards.Length == 0)
            {
                AlmediaLog.Warning($"Dropping in-game reward grant '{response.id}' with no rewards.");
                return;
            }
            AlmediaLog.Info($"In-game reward grant received: {response.id} ({response.rewards.Length} reward(s))");
            var handlers = OnInGameRewardGrantRequested;
            EventDispatch.Raise(nameof(OnInGameRewardGrantRequested), handlers,
                AlmediaInGameRewardGrant.FromResponse(response));
            if (handlers != null) _bridge.TrackInGameRewardGrantDelivered(response.id);
        }

        // Native owns the snapshot and pushes every transition, including the clear, so every
        // arrival replaces it here. An unreadable payload is a wire fault the host cannot act on:
        // logged at Warning, never surfaced through OnErrorOccurred, never thrown back into
        // UnitySendMessage, and the previous snapshot stands.
        private static void HandleProgressUpdated(ProgressUpdatedResponse response)
        {
            if (!response.hasProgress)
            {
                _progress = null;
                AlmediaLog.Debug("Progress cleared");
                EventDispatch.Raise(nameof(OnProgressUpdated), OnProgressUpdated, (AlmediaProgress)null);
                return;
            }

            var snapshot = response.progress;
            if (string.IsNullOrEmpty(snapshot.id))
            {
                AlmediaLog.Warning("Dropping progress snapshot with no id.");
                return;
            }

            AlmediaProgress progress;
            try
            {
                progress = AlmediaProgress.FromResponse(snapshot);
            }
            catch (Exception e)
            {
                AlmediaLog.Warning($"Dropping progress snapshot '{snapshot.id}': {e.GetType().Name}: {e.Message}");
                return;
            }

            _progress = progress;
            AlmediaLog.Debug($"Progress updated: {progress.Id}");
            EventDispatch.Raise(nameof(OnProgressUpdated), OnProgressUpdated, progress);
        }

        private static void HandleTaskCompleted(TaskCompletedResponse response)
        {
            if (string.IsNullOrEmpty(response.id))
            {
                AlmediaLog.Warning("Dropping task completion with no id.");
                return;
            }

            AlmediaTaskCompletion completion;
            try
            {
                completion = AlmediaTaskCompletion.FromResponse(response);
            }
            catch (Exception e)
            {
                AlmediaLog.Warning($"Dropping task completion '{response.id}': {e.GetType().Name}: {e.Message}");
                return;
            }

            AlmediaLog.Info($"Task completed: {completion.Id} ({completion.Task.Task.Id})");
            EventDispatch.Raise(nameof(OnTaskCompleted), OnTaskCompleted, completion);
        }

        private static void HandleBalanceChanged(BalanceChangedResponse response)
        {
            if (string.IsNullOrEmpty(response.id))
            {
                AlmediaLog.Warning("Dropping balance change with no id.");
                return;
            }

            AlmediaBalanceChange change;
            try
            {
                change = AlmediaBalanceChange.FromResponse(response);
            }
            catch (Exception e)
            {
                AlmediaLog.Warning($"Dropping balance change '{response.id}': {e.GetType().Name}: {e.Message}");
                return;
            }

            AlmediaLog.Info($"Balance changed: {change.Id} ({change.Change.Coins} coins)");
            EventDispatch.Raise(nameof(OnBalanceChanged), OnBalanceChanged, change);
        }

        private static void HandleErrorOccurred(ErrorCallbackResponse response)
        {
            AlmediaLog.Error($"Error from native: {response.code} - {response.message}");
            EventDispatch.Raise(nameof(OnErrorOccurred), OnErrorOccurred, AlmediaError.FromCallback(response));
        }

        private static void HandleScreenPresented(AlmediaScreen screen)
        {
            AlmediaLog.Info($"Screen presented: {screen}");
            EventDispatch.Raise(nameof(OnScreenPresented), OnScreenPresented, screen);
        }

        private static void HandleScreenDismissed(AlmediaScreen screen, ScreenDismissedResponse response)
        {
            var result = AlmediaInAppScreenResult.FromResponse(response);
            AlmediaLog.Info($"Screen dismissed: {screen} ({result})");
            EventDispatch.Raise(nameof(OnScreenDismissed), OnScreenDismissed, screen, result);
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
            if (_status is AlmediaStatus.NotInitialized)
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
            AlmediaLinkBridge.InGameRewardGrantRequested += HandleInGameRewardGrantRequested;
            AlmediaLinkBridge.ProgressUpdated += HandleProgressUpdated;
            AlmediaLinkBridge.TaskCompleted += HandleTaskCompleted;
            AlmediaLinkBridge.BalanceChanged += HandleBalanceChanged;
            AlmediaLinkBridge.ErrorOccurred += HandleErrorOccurred;
            AlmediaLinkBridge.ScreenPresented += HandleScreenPresented;
            AlmediaLinkBridge.ScreenDismissed += HandleScreenDismissed;
        }

        private static void UnsubscribeFromBridge()
        {
            AlmediaLinkBridge.StatusChanged -= HandleStatusChanged;
            AlmediaLinkBridge.LinkCompleted -= HandleLinkCompleted;
            AlmediaLinkBridge.NotificationsReceived -= HandleNotificationsReceived;
            AlmediaLinkBridge.InGameRewardGrantRequested -= HandleInGameRewardGrantRequested;
            AlmediaLinkBridge.ProgressUpdated -= HandleProgressUpdated;
            AlmediaLinkBridge.TaskCompleted -= HandleTaskCompleted;
            AlmediaLinkBridge.BalanceChanged -= HandleBalanceChanged;
            AlmediaLinkBridge.ErrorOccurred -= HandleErrorOccurred;
            AlmediaLinkBridge.ScreenPresented -= HandleScreenPresented;
            AlmediaLinkBridge.ScreenDismissed -= HandleScreenDismissed;
        }
    }
}
