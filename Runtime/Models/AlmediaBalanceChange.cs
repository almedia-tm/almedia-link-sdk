using System;

namespace AlmediaLink.Models
{
    /// <summary>
    /// A balance change the server reports. <see cref="AlmediaLinkSDK.OnBalanceChanged"/> delivers it.
    /// The event is historical and best-effort. <see cref="Balance"/> can differ from <see cref="AlmediaLinkSDK.Progress"/>,
    /// and a replay can deliver the same change twice. Deduplicate on <see cref="Id"/>.
    /// </summary>
    public sealed class AlmediaBalanceChange : IEquatable<AlmediaBalanceChange>
    {
        /// <summary>The server-generated message identifier. A replay carries the same identifier.</summary>
        public string Id { get; }

        /// <summary>The balance after the change.</summary>
        public AlmediaRewardPoints Balance { get; }

        /// <summary>The change. A reversal is negative.</summary>
        public AlmediaRewardPoints Change { get; }

        public AlmediaBalanceChange(string id, AlmediaRewardPoints balance, AlmediaRewardPoints change)
        {
            Id = id;
            Balance = balance;
            Change = change;
        }

        public bool Equals(AlmediaBalanceChange other)
            => other != null
               && string.Equals(Id, other.Id, StringComparison.Ordinal)
               && Equals(Balance, other.Balance)
               && Equals(Change, other.Change);

        public override bool Equals(object obj) => Equals(obj as AlmediaBalanceChange);

        public override int GetHashCode() => AlmediaSDK.ValueEquality.Hash(Id, Balance, Change);

        public static bool operator ==(AlmediaBalanceChange a, AlmediaBalanceChange b) => a is null ? b is null : a.Equals(b);

        public static bool operator !=(AlmediaBalanceChange a, AlmediaBalanceChange b) => !(a == b);
    }
}
