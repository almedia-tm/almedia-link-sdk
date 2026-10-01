using System;

namespace AlmediaLink.Models
{
    /// <summary>A task the player can do in this game to earn a reward.</summary>
    public sealed class AlmediaTask : IEquatable<AlmediaTask>
    {
        /// <summary>An opaque identifier, stable across snapshots and reward decreases. It identifies the task, not one completion.</summary>
        public string Id { get; }

        /// <summary>
        /// Groups similar tasks, one of <see cref="AlmediaTaskKind"/>. The set is open: show an unknown value
        /// as <see cref="AlmediaTaskKind.Main"/>.
        /// </summary>
        public string Kind { get; }

        /// <summary>The title to show the player.</summary>
        public string Title { get; }

        /// <summary>The reward for one completion.</summary>
        public AlmediaRewardPoints Reward { get; }

        /// <summary>The player's progress, or <c>null</c> for a yes-or-no task.</summary>
        public AlmediaTaskProgress Progress { get; }

        /// <summary>
        /// The raw ISO 8601 time of the next <see cref="Reward"/> decrease, or <c>null</c> when none is
        /// scheduled. For a countdown, use <see cref="RewardDropsAt"/>.
        /// </summary>
        public string RewardDropsAtTimestamp { get; }

        /// <summary>
        /// The parsed time of the next <see cref="Reward"/> decrease as a UTC <see cref="DateTimeOffset"/>.
        /// <c>null</c> when none is scheduled or <see cref="RewardDropsAtTimestamp"/> is not valid ISO 8601.
        /// </summary>
        public DateTimeOffset? RewardDropsAt { get; }

        public AlmediaTask(string id, string kind, string title, AlmediaRewardPoints reward, AlmediaTaskProgress progress = null,
            string rewardDropsAtTimestamp = null)
        {
            Id = id;
            Kind = kind;
            Title = title;
            Reward = reward;
            Progress = progress;
            // Native omits the key when unset, which JsonUtility reads as "".
            RewardDropsAtTimestamp = string.IsNullOrEmpty(rewardDropsAtTimestamp) ? null : rewardDropsAtTimestamp;
            RewardDropsAt = AlmediaNotification.ParseTimestamp(RewardDropsAtTimestamp);
        }

        public bool Equals(AlmediaTask other)
            => other != null
               && string.Equals(Id, other.Id, StringComparison.Ordinal)
               && string.Equals(Kind, other.Kind, StringComparison.Ordinal)
               && string.Equals(Title, other.Title, StringComparison.Ordinal)
               && Equals(Reward, other.Reward)
               && Equals(Progress, other.Progress)
               && string.Equals(RewardDropsAtTimestamp, other.RewardDropsAtTimestamp, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as AlmediaTask);

        public override int GetHashCode() => AlmediaSDK.ValueEquality.Hash(Id, Kind, Title, Reward, Progress, RewardDropsAtTimestamp);

        public static bool operator ==(AlmediaTask a, AlmediaTask b) => a is null ? b is null : a.Equals(b);

        public static bool operator !=(AlmediaTask a, AlmediaTask b) => !(a == b);
    }

    /// <summary>The known values of <see cref="AlmediaTask.Kind"/>. The server can add more.</summary>
    public static class AlmediaTaskKind
    {
        /// <summary>A regular task.</summary>
        public const string Main = "main";

        /// <summary>Pays when the player makes a purchase.</summary>
        public const string PurchaseBonus = "purchaseBonus";

        /// <summary>Its reward decreases on a schedule. See <see cref="AlmediaTask.RewardDropsAt"/>.</summary>
        public const string Burning = "burning";

        /// <summary>A purchase bonus whose reward decreases on a schedule.</summary>
        public const string BurningPurchaseBonus = "burningPurchaseBonus";

        /// <summary>Must be completed before a deadline.</summary>
        public const string TimeLimited = "timeLimited";

        /// <summary>Can be completed once a day.</summary>
        public const string Daily = "daily";

        /// <summary>Pays for time played.</summary>
        public const string Play = "play";

        /// <summary>Can be completed any number of times.</summary>
        public const string Unlimited = "unlimited";
    }

    /// <summary>The progress of an <see cref="AlmediaTask"/>.</summary>
    public sealed class AlmediaTaskProgress : IEquatable<AlmediaTaskProgress>
    {
        /// <summary>The value reached so far.</summary>
        public long Value { get; }

        /// <summary>The value needed to complete the task.</summary>
        public long Target { get; }

        public AlmediaTaskProgress(long value, long target)
        {
            Value = value;
            Target = target;
        }

        public override string ToString() => $"{Value}/{Target}";

        public bool Equals(AlmediaTaskProgress other)
            => other != null
               && Value == other.Value
               && Target == other.Target;

        public override bool Equals(object obj) => Equals(obj as AlmediaTaskProgress);

        public override int GetHashCode() => AlmediaSDK.ValueEquality.Hash(Value, Target);

        public static bool operator ==(AlmediaTaskProgress a, AlmediaTaskProgress b) => a is null ? b is null : a.Equals(b);

        public static bool operator !=(AlmediaTaskProgress a, AlmediaTaskProgress b) => !(a == b);
    }

    /// <summary>One completion of an <see cref="AlmediaTask"/>. A player can complete the same task more than once.</summary>
    public sealed class AlmediaCompletedTask : IEquatable<AlmediaCompletedTask>
    {
        /// <summary>The completed task.</summary>
        public AlmediaTask Task { get; }

        /// <summary>The raw ISO 8601 timestamp string of the completion. For display, use <see cref="CompletedAt"/>.</summary>
        public string Timestamp { get; }

        /// <summary>
        /// The parsed completion time as a UTC <see cref="DateTimeOffset"/>. <c>null</c> when
        /// <see cref="Timestamp"/> is empty or is not valid ISO 8601.
        /// </summary>
        public DateTimeOffset? CompletedAt { get; }

        /// <summary>
        /// The reward this completion paid. It differs from <see cref="AlmediaTask.Reward"/> when the reward
        /// decreased before completion, or when Freecash adjusted the credit.
        /// </summary>
        public AlmediaRewardPoints ActualReward { get; }

        public AlmediaCompletedTask(AlmediaTask task, string timestamp, AlmediaRewardPoints actualReward)
            : this(task, timestamp, AlmediaNotification.ParseTimestamp(timestamp), actualReward)
        {
        }

        internal AlmediaCompletedTask(AlmediaTask task, string timestamp, DateTimeOffset? completedAt, AlmediaRewardPoints actualReward)
        {
            Task = task;
            Timestamp = timestamp;
            CompletedAt = completedAt;
            ActualReward = actualReward;
        }

        public bool Equals(AlmediaCompletedTask other)
            => other != null
               && Equals(Task, other.Task)
               && string.Equals(Timestamp, other.Timestamp, StringComparison.Ordinal)
               && Equals(ActualReward, other.ActualReward);

        public override bool Equals(object obj) => Equals(obj as AlmediaCompletedTask);

        public override int GetHashCode() => AlmediaSDK.ValueEquality.Hash(Task, Timestamp, ActualReward);

        public static bool operator ==(AlmediaCompletedTask a, AlmediaCompletedTask b) => a is null ? b is null : a.Equals(b);

        public static bool operator !=(AlmediaCompletedTask a, AlmediaCompletedTask b) => !(a == b);
    }
}
