using System;

namespace AlmediaSDK
{
    // Member order mirrors AlmediaLink.Models.AlmediaStatus. Compat casts between the two.
    internal enum LegacyStatus
    {
        NotInitialized,
        Eligible,
        Linked,
        NotAvailable,
        Blocked,
        Disabled
    }

    // Member order mirrors AlmediaLink.Models.AlmediaNotAvailableReason. Compat casts between the two.
    internal enum LegacyNotAvailableReason
    {
        Unknown,
        Holdout,
        Disabled
    }

    internal readonly struct LegacyScreenAvailability : IEquatable<LegacyScreenAvailability>
    {
        public bool CanShowRewardHub { get; }

        public bool CanShowOffer { get; }

        public LegacyScreenAvailability(bool canShowRewardHub, bool canShowOffer)
        {
            CanShowRewardHub = canShowRewardHub;
            CanShowOffer = canShowOffer;
        }

        public bool Equals(LegacyScreenAvailability other)
            => CanShowRewardHub == other.CanShowRewardHub && CanShowOffer == other.CanShowOffer;

        public override bool Equals(object obj) => obj is LegacyScreenAvailability other && Equals(other);

        public override int GetHashCode() => (CanShowRewardHub ? 1 : 0) | (CanShowOffer ? 2 : 0);

        public static bool operator ==(LegacyScreenAvailability left, LegacyScreenAvailability right) => left.Equals(right);

        public static bool operator !=(LegacyScreenAvailability left, LegacyScreenAvailability right) => !left.Equals(right);

        // The 1.x log line "Screen availability changed: ..." prints this text.
        public override string ToString()
            => $"(canShowRewardHub: {CanShowRewardHub}, canShowOffer: {CanShowOffer})";
    }

    // What the 1.x surface reports for one status message. FromWire must keep the 1.2.1 rules:
    // AlmediaLinkSDK hosts see exactly these values, cases and changes.
    internal readonly struct LegacyStatusSnapshot
    {
        internal static readonly LegacyStatusSnapshot Initial =
            new LegacyStatusSnapshot(LegacyStatus.NotInitialized, null, default);

        public LegacyStatus Status { get; }

        public LegacyNotAvailableReason? Reason { get; }

        public LegacyScreenAvailability Availability { get; }

        private LegacyStatusSnapshot(LegacyStatus status, LegacyNotAvailableReason? reason, LegacyScreenAvailability availability)
        {
            Status = status;
            Reason = reason;
            Availability = availability;
        }

        internal static LegacyStatusSnapshot FromWire(StatusChangedResponse response)
        {
            StatusExtensions.TryFromString(response.status, out var status);
            var reason = status == LegacyStatus.NotAvailable
                ? StatusExtensions.ReasonFromWire(response.reason)
                : (LegacyNotAvailableReason?)null;
            var availability = status == LegacyStatus.NotInitialized
                ? default
                : new LegacyScreenAvailability(response.canShowRewardHub, response.canShowOffer);
            return new LegacyStatusSnapshot(status, reason, availability);
        }
    }

    internal static class StatusExtensions
    {
        public static LegacyStatus FromString(string value)
        {
            TryFromString(value, out var status);
            return status;
        }

        internal static bool TryFromString(string value, out LegacyStatus status)
        {
            switch (value)
            {
                case "notInitialized": status = LegacyStatus.NotInitialized; return true;
                case "eligible": status = LegacyStatus.Eligible; return true;
                case "linked": status = LegacyStatus.Linked; return true;
                case "notAvailable": status = LegacyStatus.NotAvailable; return true;
                case "blocked": status = LegacyStatus.Blocked; return true;
                case "disabled": status = LegacyStatus.Disabled; return true;
                default: status = LegacyStatus.NotInitialized; return false;
            }
        }

        internal static LegacyNotAvailableReason ReasonFromWire(string value)
        {
            switch (value)
            {
                case "holdout": return LegacyNotAvailableReason.Holdout;
                case "disabled": return LegacyNotAvailableReason.Disabled;
                default: return LegacyNotAvailableReason.Unknown;
            }
        }
    }
}
