using System;
using System.Collections.Generic;

namespace AlmediaLink.Models
{
    /// <summary>
    /// A snapshot of the player's progress in this game. <see cref="AlmediaLinkSDK.Progress"/> exposes it
    /// and <see cref="AlmediaLinkSDK.OnProgressUpdated"/> delivers it. A newer snapshot replaces the
    /// previous one completely. The SDK does not merge snapshots.
    /// </summary>
    public sealed class AlmediaProgress : IEquatable<AlmediaProgress>
    {
        /// <summary>An opaque snapshot identifier. It has no order. Compare <see cref="BuiltAt"/> instead.</summary>
        public string Id { get; }

        /// <summary>The raw ISO 8601 timestamp string of when the server built the snapshot. For display, use <see cref="BuiltAt"/>.</summary>
        public string Timestamp { get; }

        /// <summary>
        /// The parsed build time as a UTC <see cref="DateTimeOffset"/>. <c>null</c> when
        /// <see cref="Timestamp"/> is empty or is not valid ISO 8601.
        /// </summary>
        public DateTimeOffset? BuiltAt { get; }

        /// <summary>
        /// The player's username, or <c>null</c> when the server sent none. A newer snapshot without
        /// a username clears the previous value.
        /// </summary>
        public string Username { get; }

        /// <summary>The player's total balance.</summary>
        public AlmediaRewardPoints Balance { get; }

        /// <summary>The total the player earned in this game.</summary>
        public AlmediaRewardPoints Earned { get; }

        /// <summary>Tasks the player can still do. Not more than one entry for each task.</summary>
        public IReadOnlyList<AlmediaTask> Pending { get; }

        /// <summary>One entry for each completion, newest first. The server limits the number of entries.</summary>
        public IReadOnlyList<AlmediaCompletedTask> Completed { get; }

        /// <summary>Tasks the player can no longer complete. The server can limit the number of entries.</summary>
        public IReadOnlyList<AlmediaTask> Expired { get; }

        public AlmediaProgress(string id, string timestamp, string username, AlmediaRewardPoints balance, AlmediaRewardPoints earned,
            IReadOnlyList<AlmediaTask> pending, IReadOnlyList<AlmediaCompletedTask> completed, IReadOnlyList<AlmediaTask> expired)
            : this(id, timestamp, AlmediaNotification.ParseTimestamp(timestamp), username, balance, earned, pending, completed, expired)
        {
        }

        internal AlmediaProgress(string id, string timestamp, DateTimeOffset? builtAt, string username, AlmediaRewardPoints balance,
            AlmediaRewardPoints earned, IReadOnlyList<AlmediaTask> pending, IReadOnlyList<AlmediaCompletedTask> completed, IReadOnlyList<AlmediaTask> expired)
        {
            Id = id;
            Timestamp = timestamp;
            BuiltAt = builtAt;
            Username = string.IsNullOrEmpty(username) ? null : username;
            Balance = balance;
            Earned = earned;
            Pending = Copy(pending);
            Completed = Copy(completed);
            Expired = Copy(expired);
        }

        private static T[] Copy<T>(IReadOnlyList<T> items)
        {
            if (items == null) return Array.Empty<T>();
            var result = new T[items.Count];
            for (int i = 0; i < result.Length; i++) result[i] = items[i];
            return result;
        }

        public bool Equals(AlmediaProgress other)
            => other != null
               && string.Equals(Id, other.Id, StringComparison.Ordinal)
               && string.Equals(Timestamp, other.Timestamp, StringComparison.Ordinal)
               && string.Equals(Username, other.Username, StringComparison.Ordinal)
               && Equals(Balance, other.Balance)
               && Equals(Earned, other.Earned)
               && AlmediaSDK.ValueEquality.ListEquals(Pending, other.Pending)
               && AlmediaSDK.ValueEquality.ListEquals(Completed, other.Completed)
               && AlmediaSDK.ValueEquality.ListEquals(Expired, other.Expired);

        public override bool Equals(object obj) => Equals(obj as AlmediaProgress);

        public override int GetHashCode()
            => AlmediaSDK.ValueEquality.Hash(Id, Timestamp, Username, Balance, Earned,
                AlmediaSDK.ValueEquality.ListHash(Pending), AlmediaSDK.ValueEquality.ListHash(Completed), AlmediaSDK.ValueEquality.ListHash(Expired));

        public static bool operator ==(AlmediaProgress a, AlmediaProgress b) => a is null ? b is null : a.Equals(b);

        public static bool operator !=(AlmediaProgress a, AlmediaProgress b) => !(a == b);
    }
}
