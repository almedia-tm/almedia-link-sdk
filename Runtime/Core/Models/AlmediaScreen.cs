using System;
using System.Collections.Generic;

namespace AlmediaSDK
{
    /// <summary>
    /// The SDK-owned in-app screens whose lifecycle is reported through
    /// <see cref="Almedia.OnScreenPresented"/> and
    /// <see cref="Almedia.OnScreenDismissed"/>.
    /// </summary>
    public sealed class AlmediaScreen : IEquatable<AlmediaScreen>
    {
        /// <summary>The account-linking flow shown in an in-app webview. System-browser linking is not reported.</summary>
        public static readonly AlmediaScreen Linking = new AlmediaScreen("linking", nameof(Linking));

        /// <summary>The reward progression screen opened by <see cref="Almedia.ShowRewardHub"/>.</summary>
        public static readonly AlmediaScreen RewardHub = new AlmediaScreen("reward_hub", nameof(RewardHub));

        /// <summary>The offer screen opened by <see cref="Almedia.ShowOffer"/>.</summary>
        public static readonly AlmediaScreen Offer = new AlmediaScreen("offer", nameof(Offer));

        internal static readonly IReadOnlyList<AlmediaScreen> All = new[] { Linking, RewardHub, Offer };

        private readonly string _wireName;
        private readonly string _name;

        private AlmediaScreen(string wireName, string name)
        {
            _wireName = wireName;
            _name = name;
        }

        internal string WireName => _wireName;

        // An unknown value must be dropped by the caller (warning, no event).
        internal static bool TryFromWire(string value, out AlmediaScreen screen)
        {
            switch (value)
            {
                case "linking": screen = Linking; return true;
                case "reward_hub": screen = RewardHub; return true;
                case "offer": screen = Offer; return true;
                default: screen = null; return false;
            }
        }

        public bool Equals(AlmediaScreen other)
            => !(other is null) && string.Equals(_wireName, other._wireName, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as AlmediaScreen);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_wireName);

        public static bool operator ==(AlmediaScreen a, AlmediaScreen b) => a is null ? b is null : a.Equals(b);

        public static bool operator !=(AlmediaScreen a, AlmediaScreen b) => !(a == b);

        public override string ToString() => _name;
    }
}
