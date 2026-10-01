using System;
using System.Collections.Generic;

namespace AlmediaSDK
{
    /// <summary>Where the linking flow starts from. <see cref="Almedia.StartLinking(AlmediaPlacementType)"/> reports it.</summary>
    public sealed class AlmediaPlacementType : IEquatable<AlmediaPlacementType>
    {
        public static readonly AlmediaPlacementType Popup = new AlmediaPlacementType("popup", nameof(Popup));
        public static readonly AlmediaPlacementType RewardHub = new AlmediaPlacementType("reward_hub", nameof(RewardHub));
        public static readonly AlmediaPlacementType Banner = new AlmediaPlacementType("banner", nameof(Banner));

        internal static readonly IReadOnlyList<AlmediaPlacementType> All = new[] { Popup, RewardHub, Banner };

        private readonly string _wireName;
        private readonly string _name;

        private AlmediaPlacementType(string wireName, string name)
        {
            _wireName = wireName;
            _name = name;
        }

        internal string WireName => _wireName;

        public bool Equals(AlmediaPlacementType other)
            => !(other is null) && string.Equals(_wireName, other._wireName, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as AlmediaPlacementType);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_wireName);

        public static bool operator ==(AlmediaPlacementType a, AlmediaPlacementType b) => a is null ? b is null : a.Equals(b);

        public static bool operator !=(AlmediaPlacementType a, AlmediaPlacementType b) => !(a == b);

        public override string ToString() => _name;
    }
}
