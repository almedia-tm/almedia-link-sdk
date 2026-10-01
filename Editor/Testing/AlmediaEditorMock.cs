using System;
using System.Collections.Generic;
using System.Globalization;
using AlmediaSDK.Bridge;

namespace AlmediaSDK.Editor.Testing
{
    /// <summary>
    /// Editor-only test hook that drives <see cref="Almedia"/> into any state without a device or a
    /// backend. It does not exist in player builds, so code that references it compiles only in the
    /// editor. Every call goes through the same path as a native callback, synchronously.
    ///
    /// The first call puts the editor mock into manual mode for the rest of the play session: the
    /// simulated flows that <see cref="Almedia.Initialize"/> and the other calls start become no-ops,
    /// so they cannot race the emitted callbacks. A domain reload resets manual mode.
    /// </summary>
    public static class AlmediaEditorMock
    {
        /// <summary>
        /// Delivers <paramref name="status"/> as if native had reported it. <see cref="Almedia.Status"/>
        /// equals it afterwards, and <see cref="Almedia.OnStatusChanged"/> fires as it would for a native report.
        /// </summary>
        public static void EmitStatus(AlmediaStatus status)
        {
            if (status is null) throw new ArgumentNullException(nameof(status));
            Mock().EmitStatus(status);
        }

        /// <summary>Fires <see cref="Almedia.OnErrorOccurred"/> with the given code and message.</summary>
        public static void EmitError(AlmediaErrorCode code, string message)
        {
            if (code is null) throw new ArgumentNullException(nameof(code));
            Mock().EmitError(code, message);
        }

        /// <summary>Fires <see cref="Almedia.OnLinkCompleted"/> with the current UTC timestamp.</summary>
        public static void EmitLinkCompleted() => Mock().EmitLinkCompleted();

        /// <summary>
        /// Fires <see cref="Almedia.OnNotificationsReceived"/> with the given notifications. The SDK does
        /// not raise the event for an empty batch.
        /// </summary>
        public static void EmitNotifications(params AlmediaNotification[] notifications)
        {
            var bridge = Mock();
            var items = notifications == null
                ? Array.Empty<NotificationItem>()
                : Array.ConvertAll(notifications, ToItem);
            bridge.EmitNotifications(items);
        }

        /// <summary>
        /// Fires <see cref="Almedia.OnInGameRewardGrantRequested"/> with a generated grant id and the
        /// current UTC timestamp. The SDK drops a grant without rewards.
        /// </summary>
        public static void EmitInGameRewardGrant(params AlmediaInGameReward[] rewards)
            => EmitInGameRewardGrant(null, rewards);

        /// <summary>
        /// Fires <see cref="Almedia.OnInGameRewardGrantRequested"/> with an explicit grant id. Emit the
        /// same id twice to reproduce a redelivered grant. A null or empty <paramref name="id"/>
        /// generates one.
        /// </summary>
        public static void EmitInGameRewardGrant(string id, params AlmediaInGameReward[] rewards)
        {
            var bridge = Mock();
            var items = rewards == null
                ? Array.Empty<InGameRewardItem>()
                : Array.ConvertAll(rewards, ToItem);
            bridge.EmitInGameRewardGrant(new InGameRewardGrantResponse
            {
                id = IdOrNew(id),
                timestamp = DateTime.UtcNow.ToString("o"),
                rewards = items
            });
        }

        /// <summary>
        /// Applies <paramref name="progress"/> as the latest snapshot: <see cref="Almedia.Progress"/> holds
        /// it and <see cref="Almedia.OnProgressUpdated"/> fires. A null <paramref name="progress"/> models
        /// native clearing the snapshot.
        /// </summary>
        public static void EmitProgress(AlmediaProgress progress)
            => Mock().EmitProgress(progress == null ? null : ToResponse(progress));

        /// <summary>
        /// Fires <see cref="Almedia.OnTaskCompleted"/>. Emit the same <paramref name="id"/> twice to
        /// reproduce a replay. A null or empty <paramref name="id"/> generates one.
        /// </summary>
        public static void EmitTaskCompleted(string id, AlmediaCompletedTask task)
        {
            if (task is null) throw new ArgumentNullException(nameof(task));
            Mock().EmitTaskCompleted(new TaskCompletedResponse
            {
                id = IdOrNew(id),
                task = ToItem(task)
            });
        }

        /// <summary>
        /// Fires <see cref="Almedia.OnBalanceChanged"/> with the balance after the change and the signed
        /// change. A null or empty <paramref name="id"/> generates one.
        /// </summary>
        public static void EmitBalanceChanged(string id, AlmediaRewardPoints balance, AlmediaRewardPoints change)
        {
            if (balance is null) throw new ArgumentNullException(nameof(balance));
            if (change is null) throw new ArgumentNullException(nameof(change));
            Mock().EmitBalanceChanged(new BalanceChangedResponse
            {
                id = IdOrNew(id),
                balance = ToItem(balance),
                change = ToItem(change)
            });
        }

        /// <summary>
        /// Fires <see cref="Almedia.OnScreenPresented"/>. Follow it with <see cref="EmitScreenDismissed"/>:
        /// native reports every presented screen as dismissed exactly once.
        /// </summary>
        public static void EmitScreenPresented(AlmediaScreen screen)
        {
            if (screen is null) throw new ArgumentNullException(nameof(screen));
            Mock().EmitScreenPresented(screen);
        }

        /// <summary>Fires <see cref="Almedia.OnScreenDismissed"/> with the given result.</summary>
        public static void EmitScreenDismissed(AlmediaScreen screen, AlmediaInAppScreenResult result)
        {
            if (screen is null) throw new ArgumentNullException(nameof(screen));
            if (result is null) throw new ArgumentNullException(nameof(result));
            Mock().EmitScreenDismissed(screen, result);
        }

        /// <summary>Delivers a log line through the path the native plugins use for theirs.</summary>
        public static void EmitNativeLog(AlmediaLogLevel level, string message)
        {
            if (level is null) throw new ArgumentNullException(nameof(level));
            Mock().EmitNativeLog(level, message);
        }

        /// <summary>
        /// Stops any pending simulated flow. Every other call here does this on its first use. Call it
        /// explicitly to assert that nothing fires after <see cref="Almedia.Initialize"/>.
        /// </summary>
        public static void CancelPending() => Mock().CancelPending();

        private static EditorMockBridge Mock()
        {
            var bridge = NativeBridgeFactory.ActiveMock;
            if (bridge == null)
            {
                throw new InvalidOperationException(
                    "AlmediaEditorMock: SDK not initialized. Call Almedia.Initialize(...) first.");
            }
            bridge.EnterManualMode();
            return bridge;
        }

        private static string IdOrNew(string id) => string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;

        private static NotificationItem ToItem(AlmediaNotification n) => new NotificationItem
        {
            id = n.Id ?? "",
            title = n.Title ?? "",
            message = n.Message ?? "",
            timestamp = n.Timestamp ?? "",
            type = n.Display ?? "",
            iconUrl = n.IconUrl ?? ""
        };

        private static InGameRewardItem ToItem(AlmediaInGameReward r) => new InGameRewardItem
        {
            amount = r.Amount,
            code = r.Code ?? ""
        };

        private static ProgressResponse ToResponse(AlmediaProgress p) => new ProgressResponse
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

        private static TaskItem[] ToItems(IReadOnlyList<AlmediaTask> tasks)
        {
            var items = new TaskItem[tasks.Count];
            for (int i = 0; i < items.Length; i++) items[i] = ToItem(tasks[i]);
            return items;
        }

        private static CompletedTaskItem[] ToItems(IReadOnlyList<AlmediaCompletedTask> tasks)
        {
            var items = new CompletedTaskItem[tasks.Count];
            for (int i = 0; i < items.Length; i++) items[i] = ToItem(tasks[i]);
            return items;
        }

        private static CompletedTaskItem ToItem(AlmediaCompletedTask c) => new CompletedTaskItem
        {
            task = ToItem(c.Task),
            completedAt = c.Timestamp ?? "",
            actualReward = ToItem(c.ActualReward)
        };

        private static TaskItem ToItem(AlmediaTask t) => new TaskItem
        {
            id = t.Id ?? "",
            kind = t.Kind ?? "",
            title = t.Title ?? "",
            reward = ToItem(t.Reward),
            hasProgress = t.Progress != null,
            progress = t.Progress == null
                ? new TaskProgressItem()
                : new TaskProgressItem { value = t.Progress.Value, target = t.Progress.Target },
            rewardDropsAt = t.RewardDropsAtTimestamp ?? ""
        };

        private static RewardPointsItem ToItem(AlmediaRewardPoints r) => new RewardPointsItem
        {
            coins = r.Coins,
            inPlayerCurrency = ToItem(r.InPlayerCurrency),
            inUsd = ToItem(r.InUsd)
        };

        private static MoneyItem ToItem(AlmediaMoney m) => new MoneyItem
        {
            amount = m.Amount.ToString(CultureInfo.InvariantCulture),
            currency = m.Currency ?? ""
        };
    }
}
