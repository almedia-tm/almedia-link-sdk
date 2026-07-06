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
        private const float ATTDelay = 0.3f;

        private readonly MonoBehaviour _host;
        private readonly List<Coroutine> _scheduled = new List<Coroutine>();
        private bool _manualMode;

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

            var request = JsonUtility.FromJson<InitializeRequest>(json);
            if (request.canRunConsentFlow)
            {
                AlmediaLog.Debug("Editor mock: canRunConsentFlow=true, showing ATT pre-prompt");
                Schedule(SimulateShowATTPrePrompt());
                return;
            }

            Schedule(SimulateInitialize());
        }

        public void StartLinking(PlacementType placement)
        {
            AlmediaLog.Debug($"Editor mock: StartLinking (placement={placement.ToNativeString()})");
            if (_manualMode) { AlmediaLog.Debug("Editor mock: manual mode — StartLinking is a no-op"); return; }
            Schedule(SimulateLinkFlow());
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

        public void ContinueWithATT()
        {
            AlmediaLog.Debug("Editor mock: ContinueWithATT");
            if (_manualMode) { AlmediaLog.Debug("Editor mock: manual mode — ContinueWithATT is a no-op"); return; }
            Schedule(SimulateInitialize());
        }

        public void SkipATT()
        {
            AlmediaLog.Debug("Editor mock: SkipATT");
            if (_manualMode) { AlmediaLog.Debug("Editor mock: manual mode — SkipATT is a no-op"); return; }
            Schedule(SimulateInitialize());
        }

        public void TrackPromoLoad(PromoState state) => AlmediaLog.Debug($"Editor mock: TrackPromoLoad state={state.ToNativeString()}");
        public void TrackPromoClick() => AlmediaLog.Debug("Editor mock: TrackPromoClick");
        public void TrackPopupShow() => AlmediaLog.Debug("Editor mock: TrackPopupShow");
        public void TrackPopupDismiss() => AlmediaLog.Debug("Editor mock: TrackPopupDismiss");
        public void TrackPopupCtaClick() => AlmediaLog.Debug("Editor mock: TrackPopupCtaClick");
        public void TrackNotificationsShow(string notificationIdsJson) => AlmediaLog.Debug($"Editor mock: TrackNotificationsShow {notificationIdsJson}");
        public void TrackNotificationClick(string notificationId) => AlmediaLog.Debug($"Editor mock: TrackNotificationClick id={notificationId}");
        public void TrackATTPreliminaryShow() => AlmediaLog.Debug("Editor mock: TrackATTPreliminaryShow");

        // === Emit primitives — test hooks reachable via AlmediaLinkEditorMock facade ===
        // Each builds the JSON shape the native plugin would send and dispatches via the
        // same gameObject.SendMessage path the SimulateX coroutines use; that path is
        // synchronous on the calling frame.

        internal void EmitStatus(AlmediaStatus status)
        {
            var json = JsonUtility.ToJson(new StatusChangedResponse { status = StatusToNative(status) });
            _host.gameObject.SendMessage("OnStatusChanged", json);
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

        internal void EmitShowATTPrePrompt()
        {
            _host.gameObject.SendMessage("ShowATTPrePrompt", "{}");
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

        private IEnumerator SimulateShowATTPrePrompt()
        {
            yield return new WaitForSeconds(ATTDelay);
            _host.gameObject.SendMessage("ShowATTPrePrompt", "{}");
        }

        private IEnumerator SimulateInitialize()
        {
            yield return new WaitForSeconds(StatusTransitionDelay);
            SendStatusChanged("eligible");
        }

        private IEnumerator SimulateLinkFlow()
        {
            yield return new WaitForSeconds(LinkFlowDelay);
            SendStatusChanged("linked");

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
                        type = "reward"
                    },
                    new NotificationItem
                    {
                        id = "mock-2",
                        title = "Level Complete",
                        message = "You completed level 3!",
                        timestamp = DateTime.UtcNow.AddHours(-1).ToString("o"),
                        type = "status"
                    },
                    new NotificationItem
                    {
                        id = "mock-3",
                        title = "Daily Bonus",
                        message = "Claim your $0.25 daily bonus!",
                        timestamp = DateTime.UtcNow.ToString("o"),
                        type = "reward"
                    }
                }
            };
            _host.gameObject.SendMessage("OnNotifications", JsonUtility.ToJson(response));
        }

        private void SendStatusChanged(string status)
        {
            var json = JsonUtility.ToJson(new StatusChangedResponse { status = status });
            _host.gameObject.SendMessage("OnStatusChanged", json);
        }

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
