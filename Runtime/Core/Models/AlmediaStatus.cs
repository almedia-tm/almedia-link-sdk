using System;

namespace AlmediaSDK
{
    /// <summary>
    /// The player's status. Each case carries its own data. Read it from <see cref="Almedia.Status"/>
    /// and observe changes through <see cref="Almedia.OnStatusChanged"/>.
    /// Keep a default branch when you switch on it: a later version can add cases.
    /// </summary>
    /// <example>
    /// <code>
    /// switch (Almedia.Status)
    /// {
    ///     case AlmediaStatus.Eligible:
    ///         ShowLinkButton();
    ///         break;
    ///     case AlmediaStatus.Linked { CanShowRewardHub: true }:
    ///         ShowRewardHubButton();
    ///         break;
    ///     default:
    ///         HideEntryPoints();
    ///         break;
    /// }
    /// </code>
    /// </example>
    public abstract class AlmediaStatus : IEquatable<AlmediaStatus>
    {
        private AlmediaStatus() { }

        /// <summary>Before <see cref="Almedia.Initialize"/> resolves the first status.</summary>
        public sealed class NotInitialized : AlmediaStatus
        {
            public NotInitialized() { }
        }

        /// <summary>Linking is off for everyone: the server kill switch. Only re-initialization leaves it.</summary>
        public sealed class Disabled : AlmediaStatus
        {
            public Disabled() { }
        }

        /// <summary>The player can link. <see cref="Almedia.ShowLink"/> presents the link popup.</summary>
        public sealed class Eligible : AlmediaStatus
        {
            public Eligible() { }
        }

        /// <summary>The player has linked their account.</summary>
        public sealed class Linked : AlmediaStatus
        {
            public Linked(bool canShowRewardHub, bool canShowOffer)
            {
                CanShowRewardHub = canShowRewardHub;
                CanShowOffer = canShowOffer;
            }

            /// <summary>
            /// Whether <see cref="Almedia.ShowRewardHub"/> can present a screen right now. It can change
            /// while the player stays linked.
            /// </summary>
            public bool CanShowRewardHub { get; }

            /// <summary>
            /// Whether <see cref="Almedia.ShowOffer"/> can present a screen right now. It can change
            /// while the player stays linked.
            /// </summary>
            public bool CanShowOffer { get; }
        }

        /// <summary>Linking is not available to this player. Hide the entry point, whatever the reason.</summary>
        public sealed class NotAvailable : AlmediaStatus
        {
            /// <param name="rawReason">The reason as the server sent it. <see cref="Reason"/> is parsed from it.</param>
            public NotAvailable(string rawReason)
            {
                RawReason = rawReason ?? "";
                Reason = AlmediaNotAvailableReason.FromWire(RawReason);
            }

            /// <summary>The recognized reason, or <see cref="AlmediaNotAvailableReason.Unknown"/>.</summary>
            public AlmediaNotAvailableReason Reason { get; }

            /// <summary>Exactly what the server sent. Empty when it sent no reason.</summary>
            public string RawReason { get; }
        }

        /// <summary>The player is blocked. Unlike <see cref="NotAvailable"/>, retrying does not help.</summary>
        public sealed class Blocked : AlmediaStatus
        {
            public Blocked() { }
        }

        public bool Equals(AlmediaStatus other)
        {
            if (other is null || other.GetType() != GetType()) return false;
            switch (this)
            {
                case Linked linked:
                    var otherLinked = (Linked)other;
                    return linked.CanShowRewardHub == otherLinked.CanShowRewardHub
                           && linked.CanShowOffer == otherLinked.CanShowOffer;
                case NotAvailable notAvailable:
                    return string.Equals(notAvailable.RawReason, ((NotAvailable)other).RawReason, StringComparison.Ordinal);
                default:
                    return true;
            }
        }

        public override bool Equals(object obj) => Equals(obj as AlmediaStatus);

        public override int GetHashCode()
        {
            switch (this)
            {
                case Linked linked: return ValueEquality.Hash(GetType().Name, linked.CanShowRewardHub, linked.CanShowOffer);
                case NotAvailable notAvailable: return ValueEquality.Hash(GetType().Name, notAvailable.RawReason);
                default: return ValueEquality.Hash(GetType().Name);
            }
        }

        public static bool operator ==(AlmediaStatus a, AlmediaStatus b) => a is null ? b is null : a.Equals(b);

        public static bool operator !=(AlmediaStatus a, AlmediaStatus b) => !(a == b);

        // Must agree with isEquivalent on iOS and isEquivalentTo on Android.
        internal bool IsEquivalentTo(AlmediaStatus other)
            => Equals(other)
               || (this is NotAvailable a && a.Reason == AlmediaNotAvailableReason.Unknown
                   && other is NotAvailable b && b.Reason == AlmediaNotAvailableReason.Unknown);

        public override string ToString()
        {
            switch (this)
            {
                case Linked linked:
                    return $"Linked(canShowRewardHub: {linked.CanShowRewardHub}, canShowOffer: {linked.CanShowOffer})";
                case NotAvailable notAvailable:
                    return $"NotAvailable(reason: {notAvailable.Reason}, rawReason: {notAvailable.RawReason})";
                default:
                    return GetType().Name;
            }
        }

        internal static AlmediaStatus FromWire(StatusChangedResponse response, out bool recognized)
        {
            recognized = true;
            switch (response.status)
            {
                case "notInitialized": return new NotInitialized();
                case "disabled": return new Disabled();
                case "eligible": return new Eligible();
                case "linked": return new Linked(response.canShowRewardHub, response.canShowOffer);
                case "notAvailable": return new NotAvailable(response.reason);
                case "blocked": return new Blocked();
                default:
                    recognized = false;
                    return new NotInitialized();
            }
        }

        internal StatusChangedResponse ToWire()
        {
            var response = new StatusChangedResponse { reason = null, canShowRewardHub = false, canShowOffer = false };
            switch (this)
            {
                case Linked linked:
                    response.status = "linked";
                    response.canShowRewardHub = linked.CanShowRewardHub;
                    response.canShowOffer = linked.CanShowOffer;
                    break;
                case NotAvailable notAvailable:
                    response.status = "notAvailable";
                    response.reason = notAvailable.RawReason;
                    break;
                case Eligible _:
                    response.status = "eligible";
                    break;
                case Blocked _:
                    response.status = "blocked";
                    break;
                case Disabled _:
                    response.status = "disabled";
                    break;
                default:
                    response.status = "notInitialized";
                    break;
            }
            return response;
        }
    }
}
