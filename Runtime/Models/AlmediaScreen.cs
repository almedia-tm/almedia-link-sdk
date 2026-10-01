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
}
