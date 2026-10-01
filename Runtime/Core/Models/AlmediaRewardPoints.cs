using System;

namespace AlmediaSDK
{
    /// <summary>
    /// A balance or a reward in coins, with the display values the server supplied. Coins are
    /// canonical. The money values are for display. All three values are signed. A reversal is
    /// negative.
    /// </summary>
    public sealed class AlmediaRewardPoints : IEquatable<AlmediaRewardPoints>
    {
        /// <summary>The canonical amount.</summary>
        public long Coins { get; }

        /// <summary>
        /// The amount in the player's currency. If this SDK version does not support that currency,
        /// this is the <see cref="InUsd"/> value with the USD code.
        /// </summary>
        public AlmediaMoney InPlayerCurrency { get; }

        /// <summary>The amount in USD.</summary>
        public AlmediaMoney InUsd { get; }

        public AlmediaRewardPoints(long coins, AlmediaMoney inPlayerCurrency, AlmediaMoney inUsd)
        {
            Coins = coins;
            InPlayerCurrency = inPlayerCurrency;
            InUsd = inUsd;
        }

        internal static AlmediaRewardPoints FromItem(RewardPointsItem item)
            => new AlmediaRewardPoints(item.coins, AlmediaMoney.FromItem(item.inPlayerCurrency), AlmediaMoney.FromItem(item.inUsd));

        public override string ToString() => $"{Coins} coins ({InPlayerCurrency}, {InUsd})";

        public bool Equals(AlmediaRewardPoints other)
            => other != null
               && Coins == other.Coins
               && Equals(InPlayerCurrency, other.InPlayerCurrency)
               && Equals(InUsd, other.InUsd);

        public override bool Equals(object obj) => Equals(obj as AlmediaRewardPoints);

        public override int GetHashCode() => ValueEquality.Hash(Coins, InPlayerCurrency, InUsd);

        public static bool operator ==(AlmediaRewardPoints a, AlmediaRewardPoints b) => a is null ? b is null : a.Equals(b);

        public static bool operator !=(AlmediaRewardPoints a, AlmediaRewardPoints b) => !(a == b);
    }
}
