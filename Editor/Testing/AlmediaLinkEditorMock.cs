using System;
using System.Globalization;
using AlmediaSDK.Bridge;
using AlmediaLink.Models;
using NewModels = AlmediaSDK;

namespace AlmediaLink.Editor.Testing
{
    /// <summary>
    /// Editor-only test hook for driving AlmediaLinkSDK into any state (status, error,
    /// notifications, in-game reward grants, progress, native log) without going through a real device or
    /// backend. Lives in the AlmediaLink.Editor assembly (includePlatforms:["Editor"]) so
    /// the class does not exist in iOS/Android player builds at the assembly level
    /// host code that references it will fail to compile on a player target.
    ///
    /// The first call to any method here puts the underlying EditorMockBridge into manual
    /// mode for the rest of the play session: every subsequent auto-simulate path
    /// (Initialize/StartLinking/FetchNotifications/...) becomes a no-op so canned coroutines
    /// cannot race against test emissions. Manual mode is reset on domain reload.
    /// </summary>
    public static class AlmediaLinkEditorMock
    {
        /// <summary>
        /// Delivers a status transition. <see cref="AlmediaLinkSDK.CurrentStatus"/>,
        /// <see cref="AlmediaLinkSDK.NotAvailableReason"/>,
        /// <see cref="AlmediaLinkSDK.ScreenAvailability"/> and their events reflect the new
        /// values synchronously. <paramref name="reason"/> models the wire reason and is
        /// meaningful only with <see cref="AlmediaStatus.NotAvailable"/> ("holdout" maps to
        /// <see cref="AlmediaNotAvailableReason.Holdout"/>, "disabled" to
        /// <see cref="AlmediaNotAvailableReason.Disabled"/>, anything else to Unknown). Omitted
        /// availability flags default to (status == Linked), mirroring the happy-path native
        /// derivation; pass explicit values to model a linked player losing a screen.
        /// </summary>
        public static void EmitStatus(AlmediaStatus status, string reason = null,
            bool? canShowRewardHub = null, bool? canShowOffer = null)
            => Mock().EmitStatus(Compat.ToNewApi(status), reason, canShowRewardHub, canShowOffer);

        /// <summary>
        /// Fires <see cref="AlmediaLinkSDK.OnErrorOccurred"/> with the given code and message.
        /// Use this to exercise error-handling UI under every <see cref="AlmediaErrorCode"/> value.
        /// </summary>
        public static void EmitError(AlmediaErrorCode code, string message)
            => Mock().EmitError(Compat.ToNewApi(code), message);

        /// <summary>
        /// Fires <see cref="AlmediaLinkSDK.OnLinkCompleted"/> with the current UTC timestamp.
        /// Use this to test fresh-link UX without driving the full linking flow.
        /// </summary>
        public static void EmitLinkCompleted()
            => Mock().EmitLinkCompleted();

        /// <summary>
        /// Fires <see cref="AlmediaLinkSDK.OnNotificationsReceived"/> with the supplied items.
        /// Pass no arguments for an empty batch. Note: AlmediaLinkSDK short-circuits empty
        /// batches and does not raise the event in that case.
        /// </summary>
        public static void EmitNotifications(params MockNotification[] items)
        {
            var bridge = Mock();
            var converted = items == null
                ? Array.Empty<NewModels.NotificationItem>()
                : Array.ConvertAll(items, ToItem);
            bridge.EmitNotifications(converted);
        }

        /// <summary>
        /// Fires <see cref="AlmediaLinkSDK.OnInGameRewardGrantRequested"/> with a generated grant id and
        /// the current UTC timestamp. Pass at least one reward; the SDK drops a rewardless
        /// grant as malformed, which this can also exercise.
        /// </summary>
        public static void EmitInGameRewardGrant(params MockInGameReward[] rewards)
            => EmitInGameRewardGrant(null, rewards);

        /// <summary>
        /// Fires <see cref="AlmediaLinkSDK.OnInGameRewardGrantRequested"/> with an explicit grant id.
        /// Delivery on device is at-least-once, so call this twice with the same id to
        /// reproduce a redelivered grant and exercise host-side deduplication.
        /// A null or empty <paramref name="id"/> generates one.
        /// </summary>
        public static void EmitInGameRewardGrant(string id, params MockInGameReward[] rewards)
        {
            var bridge = Mock();
            var converted = rewards == null
                ? Array.Empty<NewModels.InGameRewardItem>()
                : Array.ConvertAll(rewards, ToRewardItem);
            bridge.EmitInGameRewardGrant(new NewModels.InGameRewardGrantResponse
            {
                id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id,
                timestamp = DateTime.UtcNow.ToString("o"),
                rewards = converted
            });
        }

        /// <summary>
        /// Applies <paramref name="progress"/> as the latest snapshot. It uses the same bridge path as
        /// the native plugins. <see cref="AlmediaLinkSDK.Progress"/> holds the snapshot and
        /// <see cref="AlmediaLinkSDK.OnProgressUpdated"/> fires. Pass <c>null</c> to model native
        /// clearing the snapshot with the stream token: the accessor becomes <c>null</c> and the event
        /// fires with <c>null</c>. Pass a null <see cref="AlmediaProgress.Username"/> to model a
        /// snapshot that clears the previous username.
        /// </summary>
        public static void EmitProgress(AlmediaProgress progress)
            => Mock().EmitProgress(progress == null ? null : ToResponse(progress));

        /// <summary>
        /// Fires <see cref="AlmediaLinkSDK.OnTaskCompleted"/>. Delivery on a device is best-effort and
        /// can repeat. Call this method twice with the same <paramref name="id"/> to reproduce a replay
        /// and to test host-side deduplication. If <paramref name="id"/> is null or empty, the mock
        /// generates one.
        /// </summary>
        public static void EmitTaskCompleted(string id, AlmediaCompletedTask task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            Mock().EmitTaskCompleted(new NewModels.TaskCompletedResponse
            {
                id = IdOrNew(id),
                task = ToItem(task)
            });
        }

        /// <summary>
        /// Fires <see cref="AlmediaLinkSDK.OnBalanceChanged"/> with the balance after the change and
        /// the signed change. If <paramref name="id"/> is null or empty, the mock generates one.
        /// </summary>
        public static void EmitBalanceChanged(string id, AlmediaRewardPoints balance, AlmediaRewardPoints change)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (change == null) throw new ArgumentNullException(nameof(change));
            Mock().EmitBalanceChanged(new NewModels.BalanceChangedResponse
            {
                id = IdOrNew(id),
                balance = ToItem(balance),
                change = ToItem(change)
            });
        }

        /// <summary>
        /// Fires <see cref="AlmediaLinkSDK.OnScreenPresented"/> for the given screen, as if the
        /// native container had committed to presenting it. Pair it with a later
        /// <see cref="EmitScreenDismissed"/> to reproduce native's matched-pair contract.
        /// </summary>
        public static void EmitScreenPresented(AlmediaScreen screen)
            => Mock().EmitScreenPresented(Compat.ToNewApi(screen));

        /// <summary>
        /// Fires <see cref="AlmediaLinkSDK.OnScreenDismissed"/> for the given screen with the given
        /// result. Supply an error code and message only for
        /// <see cref="InAppScreenResultType.Failed"/>; they are ignored for completed/cancelled
        /// outcomes.
        /// </summary>
        public static void EmitScreenDismissed(AlmediaScreen screen, InAppScreenResultType result,
            AlmediaErrorCode errorCode = AlmediaErrorCode.Unknown, string errorMessage = null)
            => Mock().EmitScreenDismissed(Compat.ToNewApi(screen), Compat.ToNewApi(result, errorCode, errorMessage));

        /// <summary>
        /// Compatibility shim: the SDK no longer shows an ATT pre-prompt. Still flips the mock into
        /// manual mode (and still throws before <see cref="AlmediaLinkSDK.Initialize"/>) exactly like
        /// every other emit, but delivers nothing.
        /// </summary>
        [Obsolete("The SDK no longer shows an ATT pre-prompt; this emit has no effect.")]
        public static void EmitShowATTPrePrompt()
        {
            Mock();
            AlmediaLog.Warning("EmitShowATTPrePrompt is a no-op: the ATT pre-prompt was removed in 1.2.0.");
        }

        /// <summary>
        /// Delivers a forwarded log line through the same path the iOS/Android native plugins use.
        /// Subscribers of <see cref="AlmediaLinkSDK.OnLog"/> receive it as if it had come from native.
        /// </summary>
        public static void EmitNativeLog(AlmediaLogLevel level, string message)
            => Mock().EmitNativeLog(Compat.ToNewApi(level), message);

        /// <summary>
        /// Stops any pending auto-simulate coroutine. The first call to any other Emit* method
        /// invokes this internally as part of the manual-mode flip; call it explicitly when a
        /// test needs to assert that no callback fires after <see cref="AlmediaLinkSDK.Initialize"/>.
        /// </summary>
        public static void CancelPending()
            => Mock().CancelPending();

        private static EditorMockBridge Mock()
        {
            var bridge = NativeBridgeFactory.ActiveMock;
            if (bridge == null)
            {
                throw new InvalidOperationException(
                    "AlmediaLinkEditorMock: SDK not initialized. Call AlmediaLinkSDK.Initialize(...) first.");
            }
            bridge.EnterManualMode();
            return bridge;
        }

        private static NewModels.NotificationItem ToItem(MockNotification n) => new NewModels.NotificationItem
        {
            id = n.Id ?? "",
            title = n.Title ?? "",
            message = n.Message ?? "",
            timestamp = n.Timestamp ?? "",
            type = n.Display ?? "",
            iconUrl = n.IconUrl ?? ""
        };

        private static NewModels.InGameRewardItem ToRewardItem(MockInGameReward r) => new NewModels.InGameRewardItem
        {
            amount = r.Amount,
            code = r.Code ?? ""
        };

        private static string IdOrNew(string id) => string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;

        private static NewModels.ProgressResponse ToResponse(AlmediaProgress p) => new NewModels.ProgressResponse
        {
            id = p.Id ?? "",
            timestamp = p.Timestamp ?? "",
            username = p.Username ?? "",
            balance = ToItem(p.Balance),
            earned = ToItem(p.Earned),
            pending = ToItems(p.Pending),
            completed = ToItems(p.Completed),
            expired = ToItems(p.Expired)
        };

        private static NewModels.TaskItem[] ToItems(System.Collections.Generic.IReadOnlyList<AlmediaTask> tasks)
        {
            var items = new NewModels.TaskItem[tasks.Count];
            for (int i = 0; i < items.Length; i++) items[i] = ToItem(tasks[i]);
            return items;
        }

        private static NewModels.CompletedTaskItem[] ToItems(System.Collections.Generic.IReadOnlyList<AlmediaCompletedTask> tasks)
        {
            var items = new NewModels.CompletedTaskItem[tasks.Count];
            for (int i = 0; i < items.Length; i++) items[i] = ToItem(tasks[i]);
            return items;
        }

        private static NewModels.CompletedTaskItem ToItem(AlmediaCompletedTask c) => new NewModels.CompletedTaskItem
        {
            task = ToItem(c.Task),
            completedAt = c.Timestamp ?? "",
            actualReward = ToItem(c.ActualReward)
        };

        private static NewModels.TaskItem ToItem(AlmediaTask t) => new NewModels.TaskItem
        {
            id = t.Id ?? "",
            kind = t.Kind ?? "",
            title = t.Title ?? "",
            reward = ToItem(t.Reward),
            hasProgress = t.Progress != null,
            progress = t.Progress == null
                ? new NewModels.TaskProgressItem()
                : new NewModels.TaskProgressItem { value = t.Progress.Value, target = t.Progress.Target },
            rewardDropsAt = t.RewardDropsAtTimestamp ?? ""
        };

        private static NewModels.RewardPointsItem ToItem(AlmediaRewardPoints r) => new NewModels.RewardPointsItem
        {
            coins = r.Coins,
            inPlayerCurrency = ToItem(r.InPlayerCurrency),
            inUsd = ToItem(r.InUsd)
        };

        // decimal.ToString never uses exponent form, so this is the plain notation the natives send.
        private static NewModels.MoneyItem ToItem(AlmediaMoney m) => new NewModels.MoneyItem
        {
            amount = m.Amount.ToString(CultureInfo.InvariantCulture),
            currency = m.Currency ?? ""
        };
    }

    /// <summary>
    /// Public test-facing notification shape; converted to the internal NotificationItem
    /// DTO when emitted. Field-named so calls stay readable as the protocol evolves.
    /// </summary>
    public readonly struct MockNotification
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Message;
        public readonly string Display;
        public readonly string Timestamp;
        public readonly string IconUrl;

        /// <summary>Alias of <see cref="Display"/>.</summary>
        [Obsolete("Since 1.2.0 the wire field carries the presentation hint (\"popup\"/\"tray\"). Use Display.")]
        public string Type => Display;

        /// <summary>
        /// Constructs a notification for <see cref="AlmediaLinkEditorMock.EmitNotifications"/>.
        /// <paramref name="display"/> models the wire presentation hint ("popup" or "tray";
        /// native never forwards anything else). A null <paramref name="timestamp"/> defaults
        /// to the current UTC time in ISO-8601 (round-trip "o" format), matching the format
        /// the backend emits. A null <paramref name="iconUrl"/> models the omitted wire key.
        /// </summary>
        public MockNotification(string id, string title, string message, string display,
            string timestamp = null, string iconUrl = null)
        {
            Id = id;
            Title = title;
            Message = message;
            Display = display;
            Timestamp = timestamp ?? DateTime.UtcNow.ToString("o");
            IconUrl = iconUrl;
        }
    }

    /// <summary>
    /// One reward line item for <see cref="AlmediaLinkEditorMock.EmitInGameRewardGrant(MockInGameReward[])"/>.
    /// </summary>
    public readonly struct MockInGameReward
    {
        public readonly double Amount;
        public readonly string Code;

        public MockInGameReward(double amount, string code)
        {
            Amount = amount;
            Code = code;
        }
    }
}
