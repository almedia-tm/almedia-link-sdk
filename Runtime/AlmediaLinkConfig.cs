namespace AlmediaLink
{
    /// <summary>
    /// Runtime configuration for the Almedia Link SDK.
    /// Set properties before calling AlmediaLinkSDK.Initialize().
    /// Values set here override the corresponding AlmediaLinkSettings (ScriptableObject) defaults.
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
        /// Notification polling interval in seconds.
        /// If null, falls back to AlmediaLinkSettings.NotificationPollIntervalSeconds (default 30).
        /// </summary>
        public int? NotificationsPollingIntervalSec { get; set; }

        /// <summary>
        /// Resolves this config against the ScriptableObject defaults loaded from Resources.
        /// </summary>
        internal ResolvedAlmediaLinkConfig Resolve()
        {
            return Resolve(AlmediaLinkSettings.Load());
        }

        /// <summary>
        /// Resolves this config against the provided settings.
        /// Code-supplied values take precedence over ScriptableObject defaults.
        /// Platform selection picks the correct key for the current build target.
        /// </summary>
        internal ResolvedAlmediaLinkConfig Resolve(AlmediaLinkSettings settings)
        {
            var result = new ResolvedAlmediaLinkConfig();

            string configKey;
            string settingsKey;

#if UNITY_IOS
            configKey = IosIntegrationKey;
            settingsKey = settings?.IosIntegrationKey;
#elif UNITY_ANDROID
            configKey = AndroidIntegrationKey;
            settingsKey = settings?.AndroidIntegrationKey;
#else
            configKey = !string.IsNullOrEmpty(IosIntegrationKey) ? IosIntegrationKey : AndroidIntegrationKey;
            settingsKey = !string.IsNullOrEmpty(settings?.IosIntegrationKey)
                ? settings.IosIntegrationKey
                : settings?.AndroidIntegrationKey;
#endif

            result.IntegrationKey = !string.IsNullOrEmpty(configKey) ? configKey : settingsKey;

            result.NotificationsPollingIntervalSec = NotificationsPollingIntervalSec
                ?? settings?.NotificationPollIntervalSeconds
                ?? AlmediaLinkSettings.DefaultPollInterval;
            
            result.Gaid = Gaid;
            result.Asid = Asid;
            result.Oaid = Oaid;
            result.Idfa = Idfa;
            result.Idfv = Idfv;
            result.AdjustDeviceId = AdjustDeviceId;
            result.AppsFlyerId = AppsFlyerId;
            result.AccountId = AccountId;

            result.IsValid = !string.IsNullOrEmpty(result.IntegrationKey);

            return result;
        }
    }
}
