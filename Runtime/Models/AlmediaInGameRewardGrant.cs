using System;
using System.Collections.Generic;

namespace AlmediaLink.Models
{
    /// <summary>
    /// An in-game-reward grant delivered via <c>AlmediaLinkSDK.OnInGameRewardGrantRequested</c>.
    /// A grant is an atomic bundle: every reward in <see cref="Rewards"/> was granted
    /// together and belongs to one celebration.
    /// </summary>
    public class AlmediaInGameRewardGrant
    {
        /// <summary>
        /// Server-issued and unique per grant; a redelivery repeats it. Deduplicate on
        /// this value when a repeat credit matters to your economy.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Raw ISO 8601 timestamp string of when the backend issued the grant. For
        /// display logic prefer <see cref="ReceivedAt"/>.
        /// </summary>
        public string Timestamp { get; }

        /// <summary>
        /// The parsed timestamp as a UTC <see cref="DateTimeOffset"/>, or <c>null</c>
        /// when <see cref="Timestamp"/> is empty or cannot be parsed as ISO 8601.
        /// </summary>
        public DateTimeOffset? ReceivedAt { get; }

        /// <summary>The rewards in this grant. At least one entry.</summary>
        public IReadOnlyList<AlmediaInGameReward> Rewards { get; }

        public AlmediaInGameRewardGrant(string id, string timestamp, params AlmediaInGameReward[] rewards)
        {
            if (rewards == null || rewards.Length == 0)
                throw new ArgumentException("A grant carries at least one reward.", nameof(rewards));

            Id = id;
            Timestamp = timestamp;
            ReceivedAt = AlmediaNotification.ParseTimestamp(timestamp);
            Rewards = (AlmediaInGameReward[])rewards.Clone();
        }

        internal static AlmediaInGameRewardGrant FromResponse(InGameRewardGrantResponse response)
        {
            var items = response.rewards ?? Array.Empty<InGameRewardItem>();
            var rewards = new AlmediaInGameReward[items.Length];
            for (int i = 0; i < items.Length; i++)
                rewards[i] = new AlmediaInGameReward(items[i].amount, items[i].code);
            return new AlmediaInGameRewardGrant(response.id, response.timestamp, rewards);
        }
    }

    /// <summary>One reward line item of an <see cref="AlmediaInGameRewardGrant"/>.</summary>
    public class AlmediaInGameReward
    {
        /// <summary>
        /// Amount to credit.
        /// </summary>
        public double Amount { get; }

        /// <summary>Reward code agreed with Almedia, e.g. <c>"gems"</c>.</summary>
        public string Code { get; }

        public AlmediaInGameReward(double amount, string code)
        {
            Amount = amount;
            Code = code;
        }
    }
}
