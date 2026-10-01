using System;
using System.Collections.Generic;
using System.Linq;

namespace AlmediaSDK
{
    /// <summary>
    /// A part of the Link experience a game can declare it hides from its players, through
    /// <see cref="AlmediaConfig.DisabledFeatures"/> or the settings asset. The backend enforces
    /// the declaration; the SDK only reports it. Only the values declared here exist.
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

        /// <summary>Every feature this SDK version knows, in declaration order.</summary>
        internal static readonly IReadOnlyList<AlmediaFeature> All = new[] { Linking, RewardHub, Offer, Notifications };

        // Wire string shared with the iOS and Android plugins. Equality compares it.
        private readonly string _wireName;

        private AlmediaFeature(string wireName) => _wireName = wireName;

        internal string WireName => _wireName;

        internal static bool TryFromWireName(string value, out AlmediaFeature feature)
        {
            feature = All.FirstOrDefault(known => known._wireName == value);
            return feature != null;
        }

        /// <summary>
        /// Sorted, deduplicated, comma-joined wire names, e.g. <c>offer,reward_hub</c>. Only members
        /// of <see cref="All"/> pass, so a null entry or a value forged through reflection is dropped.
        /// </summary>
        internal static string ToCanonicalString(IEnumerable<AlmediaFeature> features)
            => string.Join(",", features
                .Where(f => TryFromWireName(f?._wireName, out _))
                .Select(f => f._wireName)
                .Distinct()
                .OrderBy(s => s, StringComparer.Ordinal));

        public bool Equals(AlmediaFeature other)
            => other != null && string.Equals(_wireName, other._wireName, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as AlmediaFeature);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_wireName);
        public static bool operator ==(AlmediaFeature a, AlmediaFeature b) => a is null ? b is null : a.Equals(b);
        public static bool operator !=(AlmediaFeature a, AlmediaFeature b) => !(a == b);
        public override string ToString() => _wireName;
    }
}
