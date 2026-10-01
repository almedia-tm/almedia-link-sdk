#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AlmediaSDK.Bridge
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
        private LegacyStatus _lastStatus = LegacyStatus.NotInitialized;

        public EditorMockBridge(MonoBehaviour host)
        {
            _host = host;
        }

        // Flips the bridge into manual mode for the rest of the run. After this, every
        // auto-simulate entry point (Initialize/StartLinking/FetchNotifications/...) is a no-op
        // so test-driven Emit* calls can't be clobbered by stale canned coroutines. Idempotent;
        // the first call also cancels any pending auto-simulate scheduled before the flip
        // (e.g. the SimulateInitialize started by an Almedia.Initialize call that
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

        public void StartLinking(AlmediaPlacementType placement)
        {
            AlmediaLog.Debug($"Editor mock: StartLinking (placement={placement.WireName})");
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
                case LegacyStatus.Eligible:
                    StartLinking(AlmediaPlacementType.Popup);
                    break;
                case LegacyStatus.Linked:
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
        public void TrackInGameRewardGrantDelivered(string grantId) => AlmediaLog.Debug($"Editor mock: TrackInGameRewardGrantDelivered id={grantId}");
        public void NotifyPlayerQuitting() => AlmediaLog.Debug("Editor mock: NotifyPlayerQuitting");

        // === Emit primitives — test hooks reachable via AlmediaLinkEditorMock facade ===
        // Each builds the JSON shape the native plugin would send and dispatches via the
        // same gameObject.SendMessage path the SimulateX coroutines use; that path is
        // synchronous on the calling frame.

        internal void EmitStatus(LegacyStatus status, string reason = null,
            bool? canShowRewardHub = null, bool? canShowOffer = null)
        {
            _lastStatus = status;

            bool linked = status == LegacyStatus.Linked;
            SendStatus(StatusToNative(status), reason, canShowRewardHub ?? linked, canShowOffer ?? linked);
        }

        internal void EmitStatus(AlmediaStatus status)
        {
            var wire = (status ?? new AlmediaStatus.NotInitialized()).ToWire();
            StatusExtensions.TryFromString(wire.status, out _lastStatus);
            SendStatus(wire.status, wire.reason, wire.canShowRewardHub, wire.canShowOffer);
        }

        internal void EmitError(AlmediaErrorCode code, string message)
        {
            var json = JsonUtility.ToJson(new ErrorCallbackResponse
            {
                code = (code ?? AlmediaErrorCode.Unknown).WireName,
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

        // A null snapshot is the clear. Hand-built so the wire matches native exactly: no progress key.
        internal void EmitProgress(ProgressResponse snapshot)
        {
            var json = snapshot == null
                ? "{\"hasProgress\":false}"
                : JsonUtility.ToJson(new ProgressUpdatedResponse { hasProgress = true, progress = snapshot });
            _host.gameObject.SendMessage("OnProgressUpdated", json);
        }

        internal void EmitTaskCompleted(TaskCompletedResponse response)
        {
            _host.gameObject.SendMessage("OnTaskCompleted", JsonUtility.ToJson(response));
        }

        internal void EmitBalanceChanged(BalanceChangedResponse response)
        {
            _host.gameObject.SendMessage("OnBalanceChanged", JsonUtility.ToJson(response));
        }

        internal void EmitScreenPresented(AlmediaScreen screen)
        {
            _host.gameObject.SendMessage("OnScreenPresented", BuildScreenPresentedJson(screen));
        }

        internal void EmitScreenDismissed(AlmediaScreen screen, AlmediaInAppScreenResult result)
        {
            _host.gameObject.SendMessage("OnScreenDismissed", BuildScreenDismissedJson(screen, result));
        }

        internal void EmitNativeLog(AlmediaLogLevel level, string message)
        {
            var json = JsonUtility.ToJson(new NativeLogResponse
            {
                level = (level ?? AlmediaLogLevel.Debug).WireName,
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
                BuildScreenDismissedJson(screen, new AlmediaInAppScreenResult.Completed()));
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
                BuildScreenDismissedJson(AlmediaScreen.Linking, new AlmediaInAppScreenResult.Completed()));

            yield return new WaitForSeconds(StatusTransitionDelay);
            var json = JsonUtility.ToJson(new LinkCompletedResponse
            {
                linkedAt = DateTime.UtcNow.ToString("o")
            });
            _host.gameObject.SendMessage("OnLinkCompleted", json);

            // The first poll after linking brings the first snapshot.
            yield return new WaitForSeconds(PollDelay);
            EmitProgress(MockSnapshot());
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

            // On device the snapshot rides every messages answer.
            EmitProgress(MockSnapshot());
        }

        // Fresh id and timestamp each time, so it is always the strictly newer snapshot native would push.
        private static ProgressResponse MockSnapshot() => new ProgressResponse
        {
            id = Guid.NewGuid().ToString("N"),
            timestamp = DateTime.UtcNow.ToString("o"),
            username = "mock_player",
            balance = MockPoints(12500, "11.50", "12.50"),
            earned = MockPoints(6000, "5.52", "6.00"),
            pending = new[]
            {
                MockTask("mock-task-1", "Reach level 10", 2000, "1.84", "2.00", new TaskProgressItem { value = 3, target = 10 },
                    AlmediaTaskKind.Burning, DateTime.UtcNow.AddHours(2).ToString("o"))
            },
            completed = new[]
            {
                new CompletedTaskItem
                {
                    task = MockTask("mock-task-2", "Reach level 3", 400, "0.37", "0.40", null),
                    completedAt = DateTime.UtcNow.AddHours(-1).ToString("o"),
                    actualReward = MockPoints(400, "0.37", "0.40")
                }
            },
            expired = new[] { MockTask("mock-task-3", "Play 7 days in a row", 1000, "0.92", "1.00", null) }
        };

        private static TaskItem MockTask(string id, string title, long coins, string eur, string usd, TaskProgressItem progress,
            string kind = AlmediaTaskKind.Main, string rewardDropsAt = "") => new TaskItem
        {
            id = id,
            kind = kind,
            rewardDropsAt = rewardDropsAt,
            title = title,
            reward = MockPoints(coins, eur, usd),
            hasProgress = progress != null,
            progress = progress ?? new TaskProgressItem()
        };

        private static RewardPointsItem MockPoints(long coins, string eur, string usd) => new RewardPointsItem
        {
            coins = coins,
            inPlayerCurrency = new MoneyItem { amount = eur, currency = "EUR" },
            inUsd = new MoneyItem { amount = usd, currency = "USD" }
        };

        private void SendStatusChanged(string status)
        {
            if (StatusExtensions.TryFromString(status, out var parsed)) _lastStatus = parsed;
            bool linked = _lastStatus == LegacyStatus.Linked;
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
            return $"{{\"screen\":\"{screen.WireName}\"}}";
        }

        // Reproduces the exact native wire shape for a screen-dismissed callback, including the
        // literal `"error":null` for non-failed outcomes. JsonUtility cannot emit a null nested
        // object, so hand-building the string is what actually exercises the real parse path (and
        // the null trap).
        internal static string BuildScreenDismissedJson(AlmediaScreen screen, AlmediaInAppScreenResult result)
        {
            result = result ?? new AlmediaInAppScreenResult.Cancelled();
            var screenStr = screen.WireName;
            var resultStr = result.WireName;
            if (!(result is AlmediaInAppScreenResult.Failed failed))
                return $"{{\"screen\":\"{screenStr}\",\"result\":\"{resultStr}\",\"error\":null}}";

            var code = failed.Error.Code.WireName;
            var message = EscapeJson(failed.Error.Message ?? "");
            return $"{{\"screen\":\"{screenStr}\",\"result\":\"{resultStr}\",\"error\":{{\"code\":\"{code}\",\"message\":\"{message}\"}}}}";
        }

        private static string EscapeJson(string s)
        {
            var escaped = new StringBuilder(s.Length);
            foreach (var c in s)
            {
                if (c == '\\' || c == '"') escaped.Append('\\').Append(c);
                else if (c < ' ') escaped.Append("\\u").Append(((int)c).ToString("x4"));
                else escaped.Append(c);
            }
            return escaped.ToString();
        }

        // Reverse map of StatusExtensions.TryFromString. The two must stay in sync.
        private static string StatusToNative(LegacyStatus status)
        {
            switch (status)
            {
                case LegacyStatus.NotInitialized: return "notInitialized";
                case LegacyStatus.Eligible: return "eligible";
                case LegacyStatus.Linked: return "linked";
                case LegacyStatus.NotAvailable: return "notAvailable";
                case LegacyStatus.Blocked: return "blocked";
                case LegacyStatus.Disabled: return "disabled";
                default: throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown LegacyStatus value; add a reverse mapping in EditorMockBridge.");
            }
        }
    }
}
#endif
