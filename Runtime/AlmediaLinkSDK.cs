using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AlmediaLink.Models;
using AlmediaLink.UI;
using NewApi = AlmediaSDK.Almedia;
using NewModels = AlmediaSDK;

namespace AlmediaLink
{
    /// <summary>
    /// The 1.x surface, kept for existing integrations. Every member forwards to
    /// <see cref="AlmediaSDK.Almedia"/>; values, cases and event order are those of 1.x.
    /// New integrations use <see cref="AlmediaSDK.Almedia"/>.
    /// </summary>
    public static class AlmediaLinkSDK
    {
        public static string Version => NewApi.Version;

        /// <summary>
        /// The SDK's current lifecycle status. Reads <see cref="AlmediaStatus.NotInitialized"/>
        /// until the native bridge reports its first terminal status, then tracks every
        /// transition. Use this from late-joining components (UI that mounts after the
        /// first <see cref="OnStatusChanged"/> has already fired) to recover the latest
        /// status without missing a beat.
        /// </summary>
        public static AlmediaStatus CurrentStatus => Compat.ToOldApi(NewApi.Legacy.Status);

        /// <summary>
        /// Why <see cref="CurrentStatus"/> is <see cref="AlmediaStatus.NotAvailable"/>. Non-null
        /// exactly while the status is NotAvailable and null in every other status. A missing or
        /// unrecognized wire reason reads as <see cref="AlmediaNotAvailableReason.Unknown"/>.
        /// Current before <see cref="OnStatusChanged"/> fires, so handlers can read it directly.
        /// </summary>
        public static AlmediaNotAvailableReason? NotAvailableReason => Compat.ToOldApi(NewApi.Legacy.Reason);

        /// <summary>
        /// Which SDK screens native can present right now. A fresh snapshot arrives with every
        /// status update - including updates where the status itself did not change, e.g. a
        /// linked player losing the reward hub between syncs. Reads all-false until the SDK
        /// is ready.
        /// </summary>
        public static AlmediaScreenAvailability ScreenAvailability => Compat.ToOldApi(NewApi.Legacy.Availability);

        /// <summary>
        /// The latest progress snapshot, or <c>null</c> before the first one arrives and after native
        /// clears it with the message stream token. The SDK keeps it in memory only. A scene that
        /// loads after <see cref="OnProgressUpdated"/> fired can read it. It is current before that
        /// event fires.
        /// </summary>
        public static AlmediaProgress Progress => Compat.ToOldApi(NewApi.Progress);

        private static readonly EventBridge<NewModels.LegacyStatus, AlmediaStatus> StatusBridge =
            new EventBridge<NewModels.LegacyStatus, AlmediaStatus>(nameof(OnStatusChanged), Compat.ToOldApi,
                h => NewApi.LegacyStatusChanged += h, h => NewApi.LegacyStatusChanged -= h);

        private static readonly EventBridge<NewModels.LegacyScreenAvailability, AlmediaScreenAvailability> AvailabilityBridge =
            new EventBridge<NewModels.LegacyScreenAvailability, AlmediaScreenAvailability>(nameof(OnScreenAvailabilityChanged), Compat.ToOldApi,
                h => NewApi.LegacyAvailabilityChanged += h, h => NewApi.LegacyAvailabilityChanged -= h);

        private static readonly EventBridge<string, string> LinkCompletedBridge =
            new EventBridge<string, string>(nameof(OnLinkCompleted), s => s,
                h => NewApi.OnLinkCompleted += h, h => NewApi.OnLinkCompleted -= h);

        private static readonly EventBridge<List<NewModels.AlmediaNotification>, List<AlmediaNotification>> NotificationsBridge =
            new EventBridge<List<NewModels.AlmediaNotification>, List<AlmediaNotification>>(nameof(OnNotificationsReceived), Compat.ToOldApi,
                h => NewApi.OnNotificationsReceived += h, h => NewApi.OnNotificationsReceived -= h);

        private static readonly EventBridge<NewModels.AlmediaInGameRewardGrant, AlmediaInGameRewardGrant> GrantBridge =
            new EventBridge<NewModels.AlmediaInGameRewardGrant, AlmediaInGameRewardGrant>(nameof(OnInGameRewardGrantRequested), Compat.ToOldApi,
                h => NewApi.OnInGameRewardGrantRequested += h, h => NewApi.OnInGameRewardGrantRequested -= h);

        private static readonly EventBridge<NewModels.AlmediaProgress, AlmediaProgress> ProgressBridge =
            new EventBridge<NewModels.AlmediaProgress, AlmediaProgress>(nameof(OnProgressUpdated), Compat.ToOldApi,
                h => NewApi.OnProgressUpdated += h, h => NewApi.OnProgressUpdated -= h);

        private static readonly EventBridge<NewModels.AlmediaTaskCompletion, AlmediaTaskCompletion> TaskCompletedBridge =
            new EventBridge<NewModels.AlmediaTaskCompletion, AlmediaTaskCompletion>(nameof(OnTaskCompleted), Compat.ToOldApi,
                h => NewApi.OnTaskCompleted += h, h => NewApi.OnTaskCompleted -= h);

        private static readonly EventBridge<NewModels.AlmediaBalanceChange, AlmediaBalanceChange> BalanceChangedBridge =
            new EventBridge<NewModels.AlmediaBalanceChange, AlmediaBalanceChange>(nameof(OnBalanceChanged), Compat.ToOldApi,
                h => NewApi.OnBalanceChanged += h, h => NewApi.OnBalanceChanged -= h);

        private static readonly EventBridge<NewModels.AlmediaError, AlmediaError> ErrorBridge =
            new EventBridge<NewModels.AlmediaError, AlmediaError>(nameof(OnErrorOccurred), Compat.ToOldApi,
                h => NewApi.OnErrorOccurred += h, h => NewApi.OnErrorOccurred -= h);

        private static readonly EventBridge<NewModels.AlmediaScreen, AlmediaScreen> PresentedBridge =
            new EventBridge<NewModels.AlmediaScreen, AlmediaScreen>(nameof(OnScreenPresented), Compat.ToOldApi,
                h => NewApi.OnScreenPresented += h, h => NewApi.OnScreenPresented -= h);

        private static readonly EventBridge<NewModels.AlmediaScreen, NewModels.AlmediaInAppScreenResult, AlmediaScreen, InAppScreenResult> DismissedBridge =
            new EventBridge<NewModels.AlmediaScreen, NewModels.AlmediaInAppScreenResult, AlmediaScreen, InAppScreenResult>(nameof(OnScreenDismissed), Compat.ToOldApi, Compat.ToOldApi,
                h => NewApi.OnScreenDismissed += h, h => NewApi.OnScreenDismissed -= h);

        private static readonly EventBridge<AlmediaSDK.AlmediaLogLevel, string, AlmediaLogLevel, string> LogBridge =
            new EventBridge<AlmediaSDK.AlmediaLogLevel, string, AlmediaLogLevel, string>(nameof(OnLog), Compat.ToOldApi, s => s,
                h => NewApi.OnLog += h, h => NewApi.OnLog -= h);

        public static event Action<AlmediaStatus> OnStatusChanged
        {
            add => StatusBridge.Add(value);
            remove => StatusBridge.Remove(value);
        }

        /// <summary>
        /// Fires when <see cref="ScreenAvailability"/> changed. When one native update changes
        /// both status and availability, <see cref="OnStatusChanged"/> fires first; every
        /// snapshot (<see cref="CurrentStatus"/>, <see cref="NotAvailableReason"/>,
        /// <see cref="ScreenAvailability"/>) is current before either event fires.
        /// </summary>
        public static event Action<AlmediaScreenAvailability> OnScreenAvailabilityChanged
        {
            add => AvailabilityBridge.Add(value);
            remove => AvailabilityBridge.Remove(value);
        }

        public static event Action<string> OnLinkCompleted
        {
            add => LinkCompletedBridge.Add(value);
            remove => LinkCompletedBridge.Remove(value);
        }

        public static event Action<List<AlmediaNotification>> OnNotificationsReceived
        {
            add => NotificationsBridge.Add(value);
            remove => NotificationsBridge.Remove(value);
        }

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
        public static event Action<AlmediaInGameRewardGrant> OnInGameRewardGrantRequested
        {
            add => GrantBridge.Add(value);
            remove => GrantBridge.Remove(value);
        }

        /// <summary>
        /// Fires when <see cref="Progress"/> changes: a newer snapshot, or <c>null</c> when native
        /// clears the snapshot with the message stream token. The accessor already holds the new
        /// value. A handler can read the argument or the accessor. Render the progress UI here.
        /// </summary>
        public static event Action<AlmediaProgress> OnProgressUpdated
        {
            add => ProgressBridge.Add(value);
            remove => ProgressBridge.Remove(value);
        }

        /// <summary>
        /// Fires when the server reports a completed task. The event is historical and best-effort.
        /// The task is not always in <see cref="Progress"/>, and the event does not update the
        /// snapshot. A replay can deliver it twice. Deduplicate on <see cref="AlmediaTaskCompletion.Id"/>.
        /// </summary>
        public static event Action<AlmediaTaskCompletion> OnTaskCompleted
        {
            add => TaskCompletedBridge.Add(value);
            remove => TaskCompletedBridge.Remove(value);
        }

        /// <summary>
        /// Fires when the server reports a balance change. The event is historical and best-effort.
        /// The balance can differ from <see cref="Progress"/>, and the event does not update the
        /// snapshot. A replay can deliver it twice. Deduplicate on <see cref="AlmediaBalanceChange.Id"/>.
        /// </summary>
        public static event Action<AlmediaBalanceChange> OnBalanceChanged
        {
            add => BalanceChangedBridge.Add(value);
            remove => BalanceChangedBridge.Remove(value);
        }

        public static event Action<AlmediaError> OnErrorOccurred
        {
            add => ErrorBridge.Add(value);
            remove => ErrorBridge.Remove(value);
        }

        /// <summary>
        /// Fires when an SDK screen (linking webview, reward hub, offer) is now on top of the
        /// game - pause gameplay here. Fires when the native container commits to presenting,
        /// before the page loads. A call that opens nothing (wrong state, missing URL, a screen
        /// already open, an <see cref="Engage"/> no-op) fires neither this nor
        /// <see cref="OnScreenDismissed"/>. System-browser linking is not reported - only the
        /// link callbacks fire for it. Every emission is followed by exactly one matching
        /// <see cref="OnScreenDismissed"/>.
        /// </summary>
        public static event Action<AlmediaScreen> OnScreenPresented
        {
            add => PresentedBridge.Add(value);
            remove => PresentedBridge.Remove(value);
        }

        /// <summary>
        /// Fires when the screen reported by <see cref="OnScreenPresented"/> is gone - resume
        /// gameplay here. Exactly one per <see cref="OnScreenPresented"/>. Carries how the screen
        /// was dismissed (<see cref="InAppScreenResultType.Completed"/>, Cancelled, or Failed with
        /// an <see cref="AlmediaError"/>). Native completes its sync-on-close before this fires;
        /// for webview linking it fires before the outcome link callbacks
        /// (<see cref="OnLinkCompleted"/> etc.).
        /// </summary>
        public static event Action<AlmediaScreen, InAppScreenResult> OnScreenDismissed
        {
            add => DismissedBridge.Add(value);
            remove => DismissedBridge.Remove(value);
        }

        public static event Action<AlmediaLogLevel, string> OnLog
        {
            add => LogBridge.Add(value);
            remove => LogBridge.Remove(value);
        }

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
            EnsureWired();
            NewApi.Initialize((config ?? new AlmediaLinkConfig()).ToNewApi());
        }

        // The LinkButton prefab's weak init: a no-op once the host has initialized,
        // and inert unless the settings asset explicitly opts in.
        internal static void InitializeIfNeeded()
        {
            EnsureWired();
            if (NewApi.IsInitialized || _pendingAutoInit) return;

            var settings = AlmediaLinkSettings.Load();
            if (settings == null || !settings.AutoInitializeFromPrefab) return;

            try
            {
                AlmediaSDK.Bridge.NativeBridgeFactory.Create();
            }
            catch (PlatformNotSupportedException)
            {
                AlmediaLog.Warning($"Almedia SDK is not supported on {Application.platform}; the Link button stays hidden.");
                return;
            }
            catch (Exception e)
            {
                AlmediaLog.Error($"Unexpected failure creating native bridge: {e.GetType().Name}: {e.Message}");
                return;
            }

            _pendingAutoInit = true;
            AlmediaSDK.Bridge.NativeBridgeFactory.Bridge.StartCoroutine(DeferredAutoInit());
        }

        private static IEnumerator DeferredAutoInit()
        {
            yield return null;
            _pendingAutoInit = false;
            if (NewApi.IsInitialized) yield break;

            AlmediaLog.Info("Initializing from a LinkButton prefab (no host Initialize call).");
            Initialize(new AlmediaLinkConfig());
        }

        /// <summary>
        /// Presents the link popup to a player who can link. The popup's button starts linking. The
        /// popup comes from the Link Popup slot in Almedia > Settings, or from the deprecated
        /// LinkPopupOverride while that slot is empty. If neither is assigned, linking starts directly.
        /// </summary>
        /// <remarks>
        /// No-op (with a warning) until the SDK is ready. Does nothing, with a log, while
        /// <see cref="CurrentStatus"/> is not <see cref="AlmediaStatus.Eligible"/> or a link popup is open.
        /// </remarks>
        public static void ShowLink() => NewApi.ShowLink();

        /// <summary>Opens the account-linking flow directly, without the link popup.</summary>
        /// <remarks>No-op (with a warning) until the SDK is ready - the first
        /// <see cref="OnStatusChanged"/> must have fired.</remarks>
        public static void StartLinking(PlacementType placement = PlacementType.Popup)
            => NewApi.StartLinking(Compat.ToNewApi(placement));

        /// <summary>Opens the reward progression screen in a webview.</summary>
        /// <remarks>
        /// No-op (with a warning) until the SDK is ready. Native additionally requires a linked user
        /// with a live progression URL; when that is absent the call is a no-op there, reported through
        /// <see cref="OnLog"/>, and neither lifecycle event fires. When the screen appears,
        /// <see cref="OnScreenPresented"/> fires with <see cref="AlmediaScreen.RewardHub"/> and its
        /// dismissal is delivered via <see cref="OnScreenDismissed"/>.
        /// </remarks>
        public static void ShowRewardHub() => NewApi.ShowRewardHub();

        /// <summary>Opens the offer screen in a webview.</summary>
        /// <remarks>
        /// No-op (with a warning) until the SDK is ready. Native additionally requires a linked user
        /// with a live offer URL - that URL can come and go between syncs, so a call that worked
        /// earlier may later be a no-op there, reported through <see cref="OnLog"/>, and neither
        /// lifecycle event fires. When the screen appears, <see cref="OnScreenPresented"/> fires with
        /// <see cref="AlmediaScreen.Offer"/> and its dismissal is delivered via
        /// <see cref="OnScreenDismissed"/>.
        /// </remarks>
        public static void ShowOffer() => NewApi.ShowOffer();

        /// <summary>
        /// Context-aware entry point. Forwards to native, which routes on the player's state -
        /// starts linking when eligible, opens the reward hub when linked, and no-ops (with a log
        /// through <see cref="OnLog"/>) otherwise.
        /// </summary>
        /// <remarks>No-op (with a warning) until the SDK is ready.</remarks>
        public static void Engage() => NewApi.EngageCore();

        /// <summary>Issues a one-shot notification fetch.</summary>
        /// <remarks>No-op (with a warning) until the SDK is ready - the first
        /// <see cref="OnStatusChanged"/> must have fired.</remarks>
        public static void FetchNotifications() => NewApi.FetchNotifications();

        /// <summary>Resumes the notification polling loop.</summary>
        /// <remarks>No-op (with a warning) until the SDK is ready - the first
        /// <see cref="OnStatusChanged"/> must have fired.</remarks>
        public static void StartNotificationPolling() => NewApi.StartNotificationPolling();

        public static void StopNotificationPolling() => NewApi.StopNotificationPolling();

        internal static void TrackPromoLoad(PromoState state) => NewApi.TrackPromoLoad(Compat.ToNewApi(state));
        internal static void TrackPromoClick(PromoState state) => NewApi.TrackPromoClick(Compat.ToNewApi(state));
        internal static void TrackPopupShow() => NewApi.TrackPopupShow();
        internal static void TrackPopupDismiss() => NewApi.TrackPopupDismiss();
        internal static void TrackPopupCtaClick() => NewApi.TrackPopupCtaClick();
        internal static void TrackNotificationsShow(string notificationIdsJson) => NewApi.TrackNotificationsShow(notificationIdsJson);
        internal static void TrackNotificationClick(string notificationId) => NewApi.TrackNotificationClick(notificationId);

        private static bool _pendingAutoInit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetOnDomainReload()
        {
            StatusBridge.Clear();
            AvailabilityBridge.Clear();
            LinkCompletedBridge.Clear();
            NotificationsBridge.Clear();
            GrantBridge.Clear();
            ProgressBridge.Clear();
            TaskCompletedBridge.Clear();
            BalanceChangedBridge.Clear();
            ErrorBridge.Clear();
            PresentedBridge.Clear();
            DismissedBridge.Clear();
            LogBridge.Clear();
            _pendingAutoInit = false;
            EnsureWired();
        }

        private static void EnsureWired()
        {
            NewApi.Initialized -= AlmediaLinkUIManager.Initialize;
            NewApi.Initialized += AlmediaLinkUIManager.Initialize;
            NewApi.Shutdown -= AlmediaLinkUIManager.Cleanup;
            NewApi.Shutdown += AlmediaLinkUIManager.Cleanup;
            NewApi.LinkPopupPresenter = AlmediaLinkUIManager.ShowConfiguredLinkPopup;
            AlmediaSDK.AlmediaConfig.DefaultsProvider = SettingsDefaults;
        }

        private static AlmediaSDK.AlmediaConfigDefaults SettingsDefaults()
            => AlmediaLinkSettings.ToDefaults(AlmediaLinkSettings.Load());
    }
}
