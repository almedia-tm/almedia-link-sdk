using System.Collections.Generic;
using AlmediaLink.Models;

namespace AlmediaLink
{
    /// <summary>
    /// Runtime configuration for <see cref="AlmediaLinkSDK.Initialize"/>.
    /// Values set here override the corresponding AlmediaLinkSettings (ScriptableObject) defaults.
    /// New integrations use <see cref="AlmediaSDK.AlmediaConfig"/>.
    /// </summary>
    public class AlmediaLinkConfig
    {
        /// <summary>
        /// Almedia-issued integration key for iOS.
        /// If null or empty, falls back to AlmediaLinkSettings.IosIntegrationKey.
        /// </summary>
        public string IosIntegrationKey { get; set; }

        /// <summary>
        /// Almedia-issued integration key for Android.
        /// If null or empty, falls back to AlmediaLinkSettings.AndroidIntegrationKey.
        /// </summary>
        public string AndroidIntegrationKey { get; set; }

        /// <summary>
        /// Google Advertising ID (Android). Runtime-only, no ScriptableObject fallback.
        /// </summary>
        public string Gaid { get; set; }

        /// <summary>
        /// App Set ID (Android). Runtime-only, no ScriptableObject fallback.
        /// </summary>
        public string Asid { get; set; }

        /// <summary>
        /// Open Advertising ID (Huawei etc.). Runtime-only, no ScriptableObject fallback.
        /// </summary>
        public string Oaid { get; set; }

        /// <summary>
        /// Identifier for Advertisers (iOS). Runtime-only, no ScriptableObject fallback.
        /// </summary>
        public string Idfa { get; set; }

        /// <summary>
        /// Identifier for Vendor (iOS). Runtime-only, no ScriptableObject fallback.
        /// If null or empty, the native SDK collects it automatically; a provided value wins.
        /// </summary>
        public string Idfv { get; set; }

        /// <summary>
        /// Adjust device identifier. Runtime-only, no ScriptableObject fallback.
        /// </summary>
        public string AdjustDeviceId { get; set; }

        /// <summary>
        /// AppsFlyer identifier. Runtime-only, no ScriptableObject fallback.
        /// </summary>
        public string AppsFlyerId { get; set; }

        /// <summary>
        /// Host app's internal user/player ID. Runtime-only, optional.
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// Where the user was acquired, sent as <c>sub3</c> on the linking magic link only.
        /// Pass your MMP's media source verbatim, e.g. <c>applovin_int</c> or
        /// <c>googleadwords_int</c>. Runtime-only, no ScriptableObject fallback.
        /// Max 250 UTF-8 bytes; a longer value is left off the link and named in a warning.
        /// </summary>
        public string TrafficSource { get; set; }

        /// <summary>
        /// Opaque publisher payload appended to the linking magic link only, for your own
        /// S2S reporting. Runtime-only, no ScriptableObject fallback. The SDK never reads it.
        /// Max 250 UTF-8 bytes; a longer value is left off the link and named in a warning.
        /// </summary>
        public string Meta1 { get; set; }

        /// <summary>See <see cref="Meta1"/>.</summary>
        public string Meta2 { get; set; }

        /// <summary>See <see cref="Meta1"/>.</summary>
        public string Meta3 { get; set; }

        /// <summary>See <see cref="Meta1"/>.</summary>
        public string Meta4 { get; set; }

        /// <summary>
        /// Notification polling interval in seconds.
        /// If null, falls back to AlmediaLinkSettings.NotificationPollIntervalSeconds (default 30).
        /// </summary>
        public int? NotificationsPollingIntervalSec { get; set; }

        /// <summary>
        /// Parts of the Link experience this game hides from the current player. Never null;
        /// declare it with a collection initializer: <c>DisabledFeatures = { AlmediaFeature.Offer }</c>.
        /// Joined with the settings asset's set: the asset is the floor for every player, code
        /// adds for this one and cannot remove. Declared at initialization only; changing it
        /// re-initializes. The backend enforces the set, the SDK reacts to the status it returns.
        /// </summary>
        public HashSet<AlmediaFeature> DisabledFeatures { get; } = new HashSet<AlmediaFeature>();

        internal AlmediaSDK.AlmediaConfig ToNewApi()
        {
            var config = new AlmediaSDK.AlmediaConfig
            {
                IosIntegrationKey = IosIntegrationKey,
                AndroidIntegrationKey = AndroidIntegrationKey,
                Gaid = Gaid,
                Asid = Asid,
                Oaid = Oaid,
                Idfa = Idfa,
                Idfv = Idfv,
                AdjustDeviceId = AdjustDeviceId,
                AppsFlyerId = AppsFlyerId,
                AccountId = AccountId,
                TrafficSource = TrafficSource,
                Meta1 = Meta1,
                Meta2 = Meta2,
                Meta3 = Meta3,
                Meta4 = Meta4,
                NotificationsPollingIntervalSec = NotificationsPollingIntervalSec
            };
            foreach (var feature in DisabledFeatures)
                config.DisabledFeatures.Add(Compat.ToNewApi(feature));
            return config;
        }
    }
}
