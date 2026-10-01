using System;
using System.Collections.Generic;

namespace AlmediaLink.Models
{
    /// <summary>
    /// A part of the Link experience a game can declare it hides from its players, through
    /// <see cref="AlmediaLinkConfig.DisabledFeatures"/> or the settings asset. The backend
    /// enforces the declaration; the SDK only reports it. Only the values declared here exist.
    /// </summary>
    public sealed class AlmediaFeature : IEquatable<AlmediaFeature>
    {
        /// <summary>The linking entry point. An already-linked player is unaffected.</summary>
        public static readonly AlmediaFeature Linking = new AlmediaFeature("linking");

        /// <summary>The reward hub screen.</summary>
        public static readonly AlmediaFeature RewardHub = new AlmediaFeature("reward_hub");

        /// <summary>The offer screen.</summary>
        public static readonly AlmediaFeature Offer = new AlmediaFeature("offer");

        /// <summary>Almedia notifications. The rest of the message stream is untouched.</summary>
        public static readonly AlmediaFeature Notifications = new AlmediaFeature("notifications");

        internal static readonly IReadOnlyList<AlmediaFeature> All = new[] { Linking, RewardHub, Offer, Notifications };

        private readonly string _wireName;

        private AlmediaFeature(string wireName) => _wireName = wireName;

        internal string WireName => _wireName;

        public bool Equals(AlmediaFeature other)
            => other != null && string.Equals(_wireName, other._wireName, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as AlmediaFeature);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_wireName);
        public static bool operator ==(AlmediaFeature a, AlmediaFeature b) => a is null ? b is null : a.Equals(b);
        public static bool operator !=(AlmediaFeature a, AlmediaFeature b) => !(a == b);
        public override string ToString() => _wireName;
    }
}
