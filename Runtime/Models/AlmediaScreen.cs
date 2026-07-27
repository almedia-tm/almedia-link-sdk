using System;

namespace AlmediaLink.Models
{
    /// <summary>
    /// The SDK-owned in-app screens whose lifecycle is reported through
    /// <see cref="AlmediaLinkSDK.OnScreenPresented"/> and
    /// <see cref="AlmediaLinkSDK.OnScreenDismissed"/>.
    /// </summary>
    public enum AlmediaScreen
    {
        /// <summary>The account-linking flow shown in an in-app webview. System-browser linking is not reported.</summary>
        Linking,

        /// <summary>The reward progression screen opened by <see cref="AlmediaLinkSDK.ShowRewardHub"/> (or Engage routing).</summary>
        RewardHub,

        /// <summary>The offer screen opened by <see cref="AlmediaLinkSDK.ShowOffer"/>.</summary>
        Offer
    }

    internal static class AlmediaScreenExtensions
    {
        // Wire strings shared by the iOS and Android plugins. An unknown value must be
        // dropped by the caller (warning + no event), never surfaced as a garbage enum.
        internal static bool TryFromString(string value, out AlmediaScreen screen)
        {
            switch (value)
            {
                case "linking": screen = AlmediaScreen.Linking; return true;
                case "reward_hub": screen = AlmediaScreen.RewardHub; return true;
                case "offer": screen = AlmediaScreen.Offer; return true;
                default: screen = AlmediaScreen.Linking; return false;
            }
        }

        internal static string ToNativeString(this AlmediaScreen screen)
        {
            switch (screen)
            {
                case AlmediaScreen.Linking: return "linking";
                case AlmediaScreen.RewardHub: return "reward_hub";
                case AlmediaScreen.Offer: return "offer";
                default: throw new ArgumentOutOfRangeException(nameof(screen), screen, "Unknown AlmediaScreen value; add a mapping in AlmediaScreenExtensions.");
            }
        }
    }
}
