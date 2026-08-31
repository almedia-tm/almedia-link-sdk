#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using AlmediaLink.Models;
using UnityEngine;

namespace AlmediaLink.Bridge
{
    internal class EditorMockBridge : INativeBridge
    {
        private const float StatusTransitionDelay = 0.1f;
        private const float LinkFlowDelay = 0.5f;
        private const float PollDelay = 0.2f;
        private const float ScreenCloseDelay = 0.3f;

        private readonly MonoBehaviour _host;
        private readonly List<Coroutine> _scheduled = new List<Coroutine>();
        private bool _manualMode;

        // Models the native "only one in-app screen at a time" guard so the arbitration is
        // exercisable in the editor. Auto-sim only; manual-mode tests drive closes directly.
        private bool _screenOpen;

        // Tracks the last status the mock emitted so Engage() can stand in for native's
        // state-based routing decision (native, not Unity, owns that switch).
        private AlmediaStatus _lastStatus = AlmediaStatus.NotInitialized;

        public EditorMockBridge(MonoBehaviour host)
        {
            _host = host;
        }

        // Flips the bridge into manual mode for the rest of the run. After this, every
        // auto-simulate entry point (Initialize/StartLinking/FetchNotifications/...) is a no-op
        // so test-driven Emit* calls can't be clobbered by stale canned coroutines. Idempotent;
        // the first call also cancels any pending auto-simulate scheduled before the flip
        // (e.g. the SimulateInitialize started by an AlmediaLinkSDK.Initialize call that
        // immediately precedes a facade Emit*).
        internal void EnterManualMode()
        {
            if (_manualMode) return;
            _manualMode = true;
            CancelPending();
        }

        public void Initialize(string json)
        {
            AlmediaLog.Debug("Editor mock: Initialize");
            if (_manualMode) { AlmediaLog.Debug("Editor mock: manual mode — Initialize is a no-op"); return; }

            Schedule(SimulateInitialize());
        }

        public void StartLinking(PlacementType placement)
        {
            AlmediaLog.Debug($"Editor mock: StartLinking (placement={placement.ToNativeString()})");
            if (_manualMode) { AlmediaLog.Debug("Editor mock: manual mode — StartLinking is a no-op"); return; }
            if (_screenOpen) { AlmediaLog.Debug("Editor mock: an in-app screen is already open — StartLinking is a no-op"); return; }
            _screenOpen = true;
            _host.gameObject.SendMessage("OnScreenPresented", BuildScreenPresentedJson(AlmediaScreen.Linking));
            Schedule(SimulateLinkFlow());
        }

        public void ShowRewardHub()
        {
            AlmediaLog.Debug("Editor mock: ShowRewardHub");
            if (_manualMode) { AlmediaLog.Debug("Editor mock: manual mode — ShowRewardHub is a no-op"); return; }
            if (_screenOpen) { AlmediaLog.Debug("Editor mock: an in-app screen is already open — ShowRewardHub is a no-op"); return; }
            SimulateScreenLifecycle(AlmediaScreen.RewardHub);
        }

        public void ShowOffer()
        {
            AlmediaLog.Debug("Editor mock: ShowOffer");
            if (_manualMode) { AlmediaLog.Debug("Editor mock: manual mode — ShowOffer is a no-op"); return; }
            if (_screenOpen) { AlmediaLog.Debug("Editor mock: an in-app screen is already open — ShowOffer is a no-op"); return; }
            SimulateScreenLifecycle(AlmediaScreen.Offer);
        }

        // Simulates native's matched-pair contract: OnScreenPresented synchronously (native
        // commits to presenting before the page loads) and exactly one OnScreenDismissed after
        // ScreenCloseDelay. Callers emit neither for guarded/no-op calls - they must return
        // before reaching this.
        private void SimulateScreenLifecycle(AlmediaScreen screen)
        {
            _screenOpen = true;
            _host.gameObject.SendMessage("OnScreenPresented", BuildScreenPresentedJson(screen));
            Schedule(SimulateScreenDismiss(screen));
        }

        public void Engage()
        {
            AlmediaLog.Debug("Editor mock: Engage");
            if (_manualMode) { AlmediaLog.Debug("Editor mock: manual mode — Engage is a no-op"); return; }

            // Stands in for native's routing decision, keyed off the last emitted status.
            switch (_lastStatus)
            {
                case AlmediaStatus.Eligible:
                    StartLinking(PlacementType.Popup);
                    break;
                case AlmediaStatus.Linked:
                    ShowRewardHub();
                    break;
                default:
                    AlmediaLog.Debug($"Editor mock: Engage is a no-op in status {_lastStatus}");
                    break;
            }
        }

        public void FetchNotifications()
        {
            AlmediaLog.Debug("Editor mock: FetchNotifications");
            if (_manualMode) { AlmediaLog.Debug("Editor mock: manual mode — FetchNotifications is a no-op"); return; }
            Schedule(SimulatePollNotifications());
        }

        public void StartNotificationPolling()
        {
            AlmediaLog.Debug("Editor mock: StartNotificationPolling");
        }

        public void StopNotificationPolling()
        {
            AlmediaLog.Debug("Editor mock: StopNotificationPolling");
        }

        public void TrackPromoLoad(PromoState state) => AlmediaLog.Debug($"Editor mock: TrackPromoLoad state={state.ToNativeString()}");
        public void TrackPromoClick(PromoState state) => AlmediaLog.Debug($"Editor mock: TrackPromoClick state={state.ToNativeString()}");
        public void TrackPopupShow() => AlmediaLog.Debug("Editor mock: TrackPopupShow");
        public void TrackPopupDismiss() => AlmediaLog.Debug("Editor mock: TrackPopupDismiss");
        public void TrackPopupCtaClick() => AlmediaLog.Debug("Editor mock: TrackPopupCtaClick");
        public void TrackNotificationsShow(string notificationIdsJson) => AlmediaLog.Debug($"Editor mock: TrackNotificationsShow {notificationIdsJson}");
        public void TrackNotificationClick(string notificationId) => AlmediaLog.Debug($"Editor mock: TrackNotificationClick id={notificationId}");
        public void NotifyPlayerQuitting() => AlmediaLog.Debug("Editor mock: NotifyPlayerQuitting");

        // === Emit primitives — test hooks reachable via AlmediaLinkEditorMock facade ===
        // Each builds the JSON shape the native plugin would send and dispatches via the
        // same gameObject.SendMessage path the SimulateX coroutines use; that path is
        // synchronous on the calling frame.

        internal void EmitStatus(AlmediaStatus status, string reason = null,
            bool? canShowRewardHub = null, bool? canShowOffer = null)
        {
            _lastStatus = status;

            bool linked = status == AlmediaStatus.Linked;
            SendStatus(StatusToNative(status), reason, canShowRewardHub ?? linked, canShowOffer ?? linked);
        }

        internal void EmitError(AlmediaErrorCode code, string message)
        {
            var json = JsonUtility.ToJson(new ErrorCallbackResponse
            {
                code = ErrorCodeToNative(code),
                message = message ?? ""
            });
            _host.gameObject.SendMessage("OnError", json);
        }

        internal void EmitLinkCompleted()
        {
            var json = JsonUtility.ToJson(new LinkCompletedResponse
            {
                linkedAt = DateTime.UtcNow.ToString("o")
            });
            _host.gameObject.SendMessage("OnLinkCompleted", json);
        }

        internal void EmitNotifications(NotificationItem[] items)
        {
            var json = JsonUtility.ToJson(new NotificationsReceivedResponse
            {
                notifications = items ?? Array.Empty<NotificationItem>()
            });
            _host.gameObject.SendMessage("OnNotifications", json);
        }

        internal void EmitInGameRewardGrant(InGameRewardGrantResponse response)
        {
            _host.gameObject.SendMessage("OnInGameRewardGrantRequested", JsonUtility.ToJson(response));
        }

        internal void EmitScreenPresented(AlmediaScreen screen)
        {
            _host.gameObject.SendMessage("OnScreenPresented", BuildScreenPresentedJson(screen));
        }

        internal void EmitScreenDismissed(AlmediaScreen screen, InAppScreenResultType result,
            AlmediaErrorCode errorCode = AlmediaErrorCode.Unknown, string errorMessage = null)
        {
            _host.gameObject.SendMessage("OnScreenDismissed",
                BuildScreenDismissedJson(screen, result, errorCode, errorMessage));
        }

        internal void EmitNativeLog(AlmediaLogLevel level, string message)
        {
            var json = JsonUtility.ToJson(new NativeLogResponse
            {
                level = LogLevelToNative(level),
                message = message ?? ""
            });
            _host.gameObject.SendMessage("OnNativeLog", json);
        }

        // === Coroutine scheduling with safe cleanup ===
        // The Wrap() coroutine IS the handle tracked in _scheduled — StopCoroutine on it
        // disposes the iterator and runs the finally, so cancellation and natural completion
        // both go through the same cleanup path. Exceptions inside inner also propagate
        // through the finally, so a throwing simulate-routine cannot leak the handle.
        // CancelPending snapshots before iterating so a coroutine that re-enters (e.g. a
        // status callback that calls CancelPending) cannot mutate the live list mid-loop.
        // The `completed` flag closes a narrow race where Wrap finishes synchronously
        // (no yields) and removeSelf runs before _scheduled.Add — preventing a stale handle
        // from sticking around forever.

        private void Schedule(IEnumerator routine)
        {
            Coroutine handle = null;
            bool completed = false;
            Action removeSelf = () =>
            {
                completed = true;
                if (handle != null) _scheduled.Remove(handle);
            };
            handle = _host.StartCoroutine(Wrap(routine, removeSelf));
            if (!completed) _scheduled.Add(handle);
        }

        private static IEnumerator Wrap(IEnumerator inner, Action onComplete)
        {
            try
            {
                while (inner.MoveNext()) yield return inner.Current;
            }
            finally
            {
                onComplete();
            }
        }

        internal void CancelPending()
        {
            var snapshot = _scheduled.ToArray();
            _scheduled.Clear();
            foreach (var c in snapshot)
                if (c != null) _host.StopCoroutine(c);
        }

        private IEnumerator SimulateInitialize()
        {
            yield return new WaitForSeconds(StatusTransitionDelay);
            SendStatusChanged("eligible");
        }

        private IEnumerator SimulateScreenDismiss(AlmediaScreen screen)
        {
            yield return new WaitForSeconds(ScreenCloseDelay);
            _screenOpen = false;
            _host.gameObject.SendMessage("OnScreenDismissed",
                BuildScreenDismissedJson(screen, InAppScreenResultType.Completed, AlmediaErrorCode.Unknown, null));
        }

        // Models the WEBVIEW linking strategy so pause/resume wiring is exercisable in the
        // editor (production configured with the system-browser strategy fires no pair for
        // linking). StartLinking already sent OnScreenPresented on the commit-to-present;
        // the rest follows the documented contract: the pre-dismissal status refresh, then
        // OnScreenDismissed, then the outcome link callback.
        private IEnumerator SimulateLinkFlow()
        {
            yield return new WaitForSeconds(LinkFlowDelay);
            SendStatusChanged("linked");

            _screenOpen = false;
            _host.gameObject.SendMessage("OnScreenDismissed",
                BuildScreenDismissedJson(AlmediaScreen.Linking, InAppScreenResultType.Completed, AlmediaErrorCode.Unknown, null));

            yield return new WaitForSeconds(StatusTransitionDelay);
            var json = JsonUtility.ToJson(new LinkCompletedResponse
            {
                linkedAt = DateTime.UtcNow.ToString("o")
            });
            _host.gameObject.SendMessage("OnLinkCompleted", json);
        }

        private IEnumerator SimulatePollNotifications()
        {
            yield return new WaitForSeconds(PollDelay);
            var response = new NotificationsReceivedResponse
            {
                notifications = new[]
                {
                    new NotificationItem
                    {
                        id = "mock-1",
                        title = "Mock Reward",
                        message = "You earned a mock reward!",
                        timestamp = DateTime.UtcNow.AddMinutes(-5).ToString("o"),
                        type = "popup"
                    },
                    new NotificationItem
                    {
                        id = "mock-2",
                        title = "Level Complete",
                        message = "You completed level 3!",
                        timestamp = DateTime.UtcNow.AddHours(-1).ToString("o"),
                        type = "tray"
                    },
                    new NotificationItem
                    {
                        id = "mock-3",
                        title = "Daily Bonus",
                        message = "Claim your $0.25 daily bonus!",
                        timestamp = DateTime.UtcNow.ToString("o"),
                        type = "popup"
                    }
                }
            };
            _host.gameObject.SendMessage("OnNotifications", JsonUtility.ToJson(response));
        }

        private void SendStatusChanged(string status)
        {
            if (StatusExtensions.TryFromString(status, out var parsed)) _lastStatus = parsed;
            bool linked = _lastStatus == AlmediaStatus.Linked;
            SendStatus(status, null, linked, linked);
        }

        // Reproduces the native wire shape for a status callback: the reason field appears only
        // when native has one to report, while the availability fields always ride along.
        private void SendStatus(string status, string reason, bool canShowRewardHub, bool canShowOffer)
        {
            var reasonPart = reason == null ? "" : $"\"reason\":\"{EscapeJson(reason)}\",";
            var json = "{" +
                $"\"status\":\"{EscapeJson(status)}\",{reasonPart}" +
                $"\"canShowRewardHub\":{(canShowRewardHub ? "true" : "false")}," +
                $"\"canShowOffer\":{(canShowOffer ? "true" : "false")}" +
                "}";
            _host.gameObject.SendMessage("OnStatusChanged", json);
        }

        internal static string BuildScreenPresentedJson(AlmediaScreen screen)
        {
            return $"{{\"screen\":\"{screen.ToNativeString()}\"}}";
        }

        // Reproduces the exact native wire shape for a screen-dismissed callback, including the
        // literal `"error":null` for non-failed outcomes. JsonUtility cannot emit a null nested
        // object, so hand-building the string is what actually exercises the real parse path (and
        // the null trap).
        internal static string BuildScreenDismissedJson(AlmediaScreen screen, InAppScreenResultType result,
            AlmediaErrorCode errorCode, string errorMessage)
        {
            var screenStr = screen.ToNativeString();
            var resultStr = ResultToNative(result);
            if (result != InAppScreenResultType.Failed)
                return $"{{\"screen\":\"{screenStr}\",\"result\":\"{resultStr}\",\"error\":null}}";

            var code = ErrorCodeToNative(errorCode);
            var message = EscapeJson(errorMessage ?? "");
            return $"{{\"screen\":\"{screenStr}\",\"result\":\"{resultStr}\",\"error\":{{\"code\":\"{code}\",\"message\":\"{message}\"}}}}";
        }

        private static string ResultToNative(InAppScreenResultType result)
        {
            switch (result)
            {
                case InAppScreenResultType.Completed: return "completed";
                case InAppScreenResultType.Cancelled: return "cancelled";
                case InAppScreenResultType.Failed: return "failed";
                default: throw new ArgumentOutOfRangeException(nameof(result), result, "Unknown InAppScreenResultType value; add a mapping in EditorMockBridge.");
            }
        }

        private static string EscapeJson(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        // Reverse maps for the native string contracts. Must stay in sync with the
        // forward maps in StatusExtensions.TryFromString, AlmediaError.MapErrorCode,
        // and AlmediaLog.ParseLevel. Round-trip tests cover every enum value so a new
        // entry added without updating these throws/fails CI.

        private static string StatusToNative(AlmediaStatus status)
        {
            switch (status)
            {
                case AlmediaStatus.NotInitialized: return "notInitialized";
                case AlmediaStatus.Eligible: return "eligible";
                case AlmediaStatus.Linked: return "linked";
                case AlmediaStatus.NotAvailable: return "notAvailable";
                case AlmediaStatus.Blocked: return "blocked";
                case AlmediaStatus.Disabled: return "disabled";
                default: throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown AlmediaStatus value; add a reverse mapping in EditorMockBridge.");
            }
        }

        private static string ErrorCodeToNative(AlmediaErrorCode code)
        {
            switch (code)
            {
                case AlmediaErrorCode.Unknown: return "unknown";
                case AlmediaErrorCode.InvalidConfiguration: return "invalidConfiguration";
                case AlmediaErrorCode.NetworkFailure: return "networkFailure";
                case AlmediaErrorCode.ServerError: return "serverError";
                case AlmediaErrorCode.RateLimited: return "rateLimited";
                case AlmediaErrorCode.Disabled: return "disabled";
                case AlmediaErrorCode.LinkingFailed: return "linkingFailed";
                case AlmediaErrorCode.InvalidState: return "invalidState";
                case AlmediaErrorCode.Unexpected: return "unexpected";
                default: throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown AlmediaErrorCode value; add a reverse mapping in EditorMockBridge.");
            }
        }

        private static string LogLevelToNative(AlmediaLogLevel level)
        {
            switch (level)
            {
                case AlmediaLogLevel.Verbose: return "verbose";
                case AlmediaLogLevel.Debug: return "debug";
                case AlmediaLogLevel.Info: return "info";
                case AlmediaLogLevel.Warning: return "warning";
                case AlmediaLogLevel.Error: return "error";
                default: throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown AlmediaLogLevel value; add a reverse mapping in EditorMockBridge.");
            }
        }
    }
}
#endif
