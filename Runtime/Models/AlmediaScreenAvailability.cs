using System;

namespace AlmediaLink.Models
{
    /// <summary>
    /// Which SDK screens native can present right now. A fresh snapshot arrives with every status
    /// update and is exposed through <see cref="AlmediaLink.AlmediaLinkSDK.ScreenAvailability"/>;
    /// changes fire <see cref="AlmediaLink.AlmediaLinkSDK.OnScreenAvailabilityChanged"/>. Values
    /// can change while the status itself stays the same - a linked player can gain or lose a
    /// screen between syncs.
    /// </summary>
    public readonly struct AlmediaScreenAvailability : IEquatable<AlmediaScreenAvailability>
    {
        /// <summary>Whether <see cref="AlmediaLink.AlmediaLinkSDK.ShowRewardHub"/> can present a screen right now.</summary>
        public bool CanShowRewardHub { get; }

        /// <summary>Whether <see cref="AlmediaLink.AlmediaLinkSDK.ShowOffer"/> can present a screen right now.</summary>
        public bool CanShowOffer { get; }

        public AlmediaScreenAvailability(bool canShowRewardHub, bool canShowOffer)
        {
            CanShowRewardHub = canShowRewardHub;
            CanShowOffer = canShowOffer;
        }

        public bool Equals(AlmediaScreenAvailability other)
            => CanShowRewardHub == other.CanShowRewardHub && CanShowOffer == other.CanShowOffer;

        public override bool Equals(object obj)
            => obj is AlmediaScreenAvailability other && Equals(other);

        public override int GetHashCode()
            => (CanShowRewardHub ? 1 : 0) | (CanShowOffer ? 2 : 0);

        public static bool operator ==(AlmediaScreenAvailability left, AlmediaScreenAvailability right)
            => left.Equals(right);

        public static bool operator !=(AlmediaScreenAvailability left, AlmediaScreenAvailability right)
            => !left.Equals(right);

        public override string ToString()
            => $"(canShowRewardHub: {CanShowRewardHub}, canShowOffer: {CanShowOffer})";
    }
}
