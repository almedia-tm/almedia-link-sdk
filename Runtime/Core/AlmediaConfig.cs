using System;
using System.Collections.Generic;
using System.Linq;

namespace AlmediaSDK
{
    /// <summary>
    /// Defaults taken from the host project's settings asset. The settings layer supplies them
    /// through <see cref="AlmediaConfig.DefaultsProvider"/>.
    /// </summary>
    internal struct AlmediaConfigDefaults
    {
        public string IosIntegrationKey;
        public string AndroidIntegrationKey;
        public int? NotificationsPollingIntervalSec;
        public IEnumerable<AlmediaFeature> DisabledFeatures;
    }

    /// <summary>
    /// Runtime configuration for the Almedia SDK.
    /// Set properties before calling <see cref="Almedia.Initialize"/>.
    /// Values set here override the settings-asset defaults.
    /// </summary>
    public class AlmediaConfig
    {
        /// <summary>Almedia-issued integration key for iOS. Falls back to the settings asset when empty.</summary>
        public string IosIntegrationKey { get; set; }

        /// <summary>Almedia-issued integration key for Android. Falls back to the settings asset when empty.</summary>
        public string AndroidIntegrationKey { get; set; }

        /// <summary>Google Advertising ID (Android). Runtime-only.</summary>
        public string Gaid { get; set; }

        /// <summary>App Set ID (Android). Runtime-only.</summary>
        public string Asid { get; set; }

        /// <summary>Open Advertising ID (Huawei etc.). Runtime-only.</summary>
        public string Oaid { get; set; }

        /// <summary>Identifier for Advertisers (iOS). Runtime-only.</summary>
        public string Idfa { get; set; }

        /// <summary>Identifier for Vendor (iOS). Runtime-only. When empty the native SDK collects it.</summary>
        public string Idfv { get; set; }

        /// <summary>Adjust device identifier. Runtime-only.</summary>
        public string AdjustDeviceId { get; set; }

        /// <summary>AppsFlyer identifier. Runtime-only.</summary>
        public string AppsFlyerId { get; set; }

        /// <summary>Host app's internal user/player ID. Optional.</summary>
        public string AccountId { get; set; }

        /// <summary>
        /// Where the user was acquired, sent as <c>sub3</c> on the linking magic link only.
        /// Pass your MMP's media source verbatim, e.g. <c>applovin_int</c> or
        /// <c>googleadwords_int</c>. Runtime-only.
        /// Max 250 UTF-8 bytes; a longer value is left off the link and named in a warning.
        /// </summary>
        public string TrafficSource { get; set; }

        /// <summary>
        /// Opaque publisher payload appended to the linking magic link only, for your own
        /// S2S reporting. Runtime-only. The SDK never reads it.
        /// Max 250 UTF-8 bytes; a longer value is left off the link and named in a warning.
        /// </summary>
        public string Meta1 { get; set; }

        /// <summary>See <see cref="Meta1"/>.</summary>
        public string Meta2 { get; set; }

        /// <summary>See <see cref="Meta1"/>.</summary>
        public string Meta3 { get; set; }

        /// <summary>See <see cref="Meta1"/>.</summary>
        public string Meta4 { get; set; }

        /// <summary>Notification polling interval in seconds. Falls back to the settings asset, then 30.</summary>
        public int? NotificationsPollingIntervalSec { get; set; }

        /// <summary>
        /// Parts of the Link experience this game hides from the current player. Never null;
        /// declare it with a collection initializer: <c>DisabledFeatures = { AlmediaFeature.Offer }</c>.
        /// Joined with the settings asset's set: the asset is the floor for every player, code
        /// adds for this one and cannot remove. Declared at initialization only; changing it
        /// re-initializes. The backend enforces the set, the SDK reacts to the status it returns.
        /// </summary>
        public HashSet<AlmediaFeature> DisabledFeatures { get; } = new HashSet<AlmediaFeature>();

        internal const int DefaultPollInterval = 30;

        /// <summary>
        /// Supplies the settings-asset defaults to <see cref="Resolve()"/>. Set by the settings
        /// layer at load; survives <see cref="Almedia.ResetOnDomainReload"/>. Null until the
        /// AlmediaLink assembly sets it at SubsystemRegistration or in <c>AlmediaLinkSDK.Initialize</c>;
        /// while null, <see cref="Resolve()"/> ignores the settings asset.
        /// </summary>
        internal static Func<AlmediaConfigDefaults> DefaultsProvider;

        internal ResolvedAlmediaConfig Resolve()
        {
            var defaults = DefaultsProvider != null ? DefaultsProvider() : default;
            return Resolve(defaults);
        }

        internal ResolvedAlmediaConfig Resolve(AlmediaConfigDefaults defaults)
        {
            var result = new ResolvedAlmediaConfig();

            string configKey;
            string settingsKey;

#if UNITY_IOS
            configKey = IosIntegrationKey;
            settingsKey = defaults.IosIntegrationKey;
#elif UNITY_ANDROID
            configKey = AndroidIntegrationKey;
            settingsKey = defaults.AndroidIntegrationKey;
#else
            configKey = !string.IsNullOrEmpty(IosIntegrationKey) ? IosIntegrationKey : AndroidIntegrationKey;
            settingsKey = !string.IsNullOrEmpty(defaults.IosIntegrationKey)
                ? defaults.IosIntegrationKey
                : defaults.AndroidIntegrationKey;
#endif

            result.IntegrationKey = !string.IsNullOrEmpty(configKey) ? configKey : settingsKey;

            result.NotificationsPollingIntervalSec = NotificationsPollingIntervalSec
                ?? defaults.NotificationsPollingIntervalSec
                ?? DefaultPollInterval;

            // ToArray copies without the enumerator's version check, so a host mutating the set
            // from another thread cannot fault the resolve.
            result.DisabledFeatures = AlmediaFeature.ToCanonicalString(
                DisabledFeatures.ToArray().Concat(defaults.DisabledFeatures ?? Enumerable.Empty<AlmediaFeature>()));

            result.Gaid = Gaid;
            result.Asid = Asid;
            result.Oaid = Oaid;
            result.Idfa = Idfa;
            result.Idfv = Idfv;
            result.AdjustDeviceId = AdjustDeviceId;
            result.AppsFlyerId = AppsFlyerId;
            result.AccountId = AccountId;
            result.TrafficSource = TrafficSource;
            result.Meta1 = Meta1;
            result.Meta2 = Meta2;
            result.Meta3 = Meta3;
            result.Meta4 = Meta4;

            result.IsValid = !string.IsNullOrEmpty(result.IntegrationKey);

            return result;
        }
    }
}
