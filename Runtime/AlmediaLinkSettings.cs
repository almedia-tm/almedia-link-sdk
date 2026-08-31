using AlmediaLink.UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace AlmediaLink
{
    [CreateAssetMenu(fileName = "AlmediaLinkSettings", menuName = "AlmediaLink/Settings", order = 0)]
    public sealed class AlmediaLinkSettings : ScriptableObject
    {
        private const string ResourcePath = "AlmediaLinkSettings";
        internal const int DefaultPollInterval = 30;

        [Header("SDK Configuration")]
        [Tooltip("Almedia issued key identifying the host app on iOS")]
        [SerializeField] private string _iosIntegrationKey;

        [Tooltip("Almedia issued key identifying the host app on Android")]
        [SerializeField] private string _androidIntegrationKey;

        [Tooltip("Polling interval in seconds for fetching notifications")]
        [Min(5)]
        [SerializeField] private int _notificationPollIntervalSeconds = DefaultPollInterval;

        [Tooltip("When enabled, the SDK renders the built-in NotificationCard and ActivityOverlay. Disable to use your own UI.")]
        [SerializeField] private bool _enableDefaultNotificationUI = true;

        [Tooltip("When enabled, a LinkButton prefab initializes the SDK by itself (using the keys above) unless the host has already called Initialize. Disable when initialization timing must stay under host control, e.g. consent-gated flows.")]
        [SerializeField] private bool _autoInitializeFromPrefab = false;

        [Header("UI Text")]
        [Tooltip("LinkPopup headline.")]
        [SerializeField] private string _popupTitle = "Get Rewarded While Playing";

        [Tooltip("Benefit 1 - bold title line.")]
        [SerializeField] private string _benefit1Title = "Earn real money & gift cards";

        [Tooltip("Benefit 1 - description below the title.")]
        [TextArea(1, 3)]
        [SerializeField] private string _benefit1Description = "Convert your gameplay into rewards.";

        [Tooltip("Benefit 2 - bold title line.")]
        [SerializeField] private string _benefit2Title = "Earn up to $20";

        [Tooltip("Benefit 2 - description below the title.")]
        [TextArea(1, 3)]
        [SerializeField] private string _benefit2Description = "On Freecash.";

        [Tooltip("Call-to-action button label on the LinkPopup.")]
        [SerializeField] private string _ctaButtonText = "Link Your Freecash Account";

        [Tooltip("Title shown at the top of the Activity Overlay (notification list modal).")]
        [SerializeField] private string _overlayTitle = "Notifications";

        [Tooltip("Link Popup background color.")]
        [SerializeField] private Color _popupBackgroundColor = new Color32(0x12, 0x12, 0x12, 0xFF);

        [Tooltip("Link Popup CTA button background color.")]
        [SerializeField] private Color _ctaButtonColor = new Color32(0x00, 0xC8, 0x53, 0xFF);

        [Tooltip("Link Popup CTA button text color.")]
        [SerializeField] private Color _ctaButtonTextColor = Color.white;

        [Header("Notifications")]
        [Tooltip("Notification card background color.")]
        [SerializeField] private Color _notificationBackgroundColor = new Color32(0x12, 0x12, 0x12, 0xFF);

        [Header("Default UI Prefabs (the notification UI the SDK spawns when enabled)")]
        [Tooltip("The notification card the SDK instantiates when notifications arrive. Assign a Prefab Variant to customize it. Cleared automatically while the default notification UI is disabled so the prefab stays out of your build.")]
        [FormerlySerializedAs("_notificationCardOverride")]
        [SerializeField] private NotificationCardController _notificationCardPrefab;

        [Tooltip("The notification list overlay opened from a stacked card. Assign a Prefab Variant to customize it. Cleared automatically while the default notification UI is disabled.")]
        [FormerlySerializedAs("_activityOverlayOverride")]
        [SerializeField] private ActivityOverlayController _activityOverlayPrefab;

        // 1.x compatibility: intentionally undrawn but still serialized and honored (wins over the
        // button's own popup reference). Not dead code.
        [SerializeField] private LinkPopupController _linkPopupOverride;

        // Bumped by AlmediaLinkMigrations after each one-shot upgrade pass over host assets.
        [SerializeField, HideInInspector] internal int _migrationVersion;

        internal bool AutoInitializeFromPrefab => _autoInitializeFromPrefab;

        public string IosIntegrationKey => _iosIntegrationKey;
        public string AndroidIntegrationKey => _androidIntegrationKey;
        public int NotificationPollIntervalSeconds => _notificationPollIntervalSeconds;
        public bool EnableDefaultNotificationUI => _enableDefaultNotificationUI;

        public string PopupTitle => _popupTitle;
        public string Benefit1Title => _benefit1Title;
        public string Benefit1Description => _benefit1Description;
        public string Benefit2Title => _benefit2Title;
        public string Benefit2Description => _benefit2Description;
        public string CtaButtonText => _ctaButtonText;
        public string OverlayTitle => _overlayTitle;
        public Color PopupBackgroundColor => _popupBackgroundColor;
        public Color CtaButtonColor => _ctaButtonColor;
        public Color CtaButtonTextColor => _ctaButtonTextColor;

        public Color NotificationBackgroundColor => _notificationBackgroundColor;

        public NotificationCardController NotificationCardPrefab => _notificationCardPrefab;
        public ActivityOverlayController ActivityOverlayPrefab => _activityOverlayPrefab;

        internal LinkPopupController LegacyLinkPopupOverride => _linkPopupOverride;

        #region Obsolete 1.x surface - compatibility shims

        [System.Obsolete("The popup is configured on the LinkButton prefab since 1.2.0. A value assigned here is still honored (it wins over the button's reference) but new integrations should assign the button's Link Popup field")]
        public LinkPopupController LinkPopupOverride => _linkPopupOverride;

        [System.Obsolete("Renamed to NotificationCardPrefab.")]
        public NotificationCardController NotificationCardOverride => _notificationCardPrefab;

        [System.Obsolete("Renamed to ActivityOverlayPrefab.")]
        public ActivityOverlayController ActivityOverlayOverride => _activityOverlayPrefab;

        // The ATT consent pre-prompt was removed in 1.2.0. These return the former default values
        // so host code compiled against 1.x keeps building; the values drive no UI.

        [System.Obsolete("The ATT consent pre-prompt was removed in 1.2.0; this constant drives no UI.")]
        public string AttPromptTitle => "Don't miss your rewards by enabling app tracking";
        [System.Obsolete("The ATT consent pre-prompt was removed in 1.2.0; this constant drives no UI.")]
        public string AttRewardAmount => "$9.38";
        [System.Obsolete("The ATT consent pre-prompt was removed in 1.2.0; this constant drives no UI.")]
        public string AttWhyTitle => "Why do we need this?";
        [System.Obsolete("The ATT consent pre-prompt was removed in 1.2.0; this constant drives no UI.")]
        public string AttWhyBody => "Tracking lets us make sure your rewards can be credited correctly and faster.";
        [System.Obsolete("The ATT consent pre-prompt was removed in 1.2.0; this constant drives no UI.")]
        public string AttControlTitle => "You're in control";
        [System.Obsolete("The ATT consent pre-prompt was removed in 1.2.0; this constant drives no UI.")]
        public string AttControlBody => "Change permissions at any time in Settings.";
        [System.Obsolete("The ATT consent pre-prompt was removed in 1.2.0; this constant drives no UI.")]
        public string AttContinueButtonText => "Continue";
        [System.Obsolete("The ATT consent pre-prompt was removed in 1.2.0; this constant drives no UI.")]
        public Color AttBackgroundColor => new Color32(0x12, 0x12, 0x12, 0xFF);
        [System.Obsolete("The ATT consent pre-prompt was removed in 1.2.0; this constant drives no UI.")]
        public Color AttPrimaryButtonColor => new Color32(0x00, 0xC8, 0x53, 0xFF);
        [System.Obsolete("The ATT consent pre-prompt was removed in 1.2.0; this constant drives no UI.")]
        public Color AttButtonTextColor => Color.white;

#pragma warning disable CS0618 // returning the obsolete stub type from an obsolete member
        [System.Obsolete("The ATT consent pre-prompt was removed in 1.2.0; always null.")]
        public ATTPrePromptController AttPrePromptOverride => null;
#pragma warning restore CS0618

        #endregion

        private static AlmediaLinkSettings _cachedInstance;

        /// <summary>
        /// Loads the settings asset from Resources/AlmediaLinkSettings.
        /// Returns the cached instance on subsequent calls. Unity fake-null semantics
        /// auto-invalidate the cache if the underlying asset is destroyed; call
        /// <see cref="InvalidateCache"/> to force a fresh load in other cases
        /// (editor tooling, tests, domain-reload-disabled play mode).
        /// </summary>
        public static AlmediaLinkSettings Load()
        {
            if (_cachedInstance != null)
                return _cachedInstance;

            _cachedInstance = Resources.Load<AlmediaLinkSettings>(ResourcePath);

            if (_cachedInstance == null)
            {
                AlmediaLog.Error(
                    $"[AlmediaLink] AlmediaLinkSettings asset not found at Resources/{ResourcePath}. " +
                               "Create one via: Right-click in Assets/AlmediaLink/Resources → Create → Almedia → Link SDK Settings.");
            }

            return _cachedInstance;
        }

        /// <summary>
        /// Clears the cached instance so the next <see cref="Load"/> call re-fetches
        /// from Resources. Useful for editor tooling and tests that swap the asset.
        /// </summary>
        public static void InvalidateCache()
        {
            _cachedInstance = null;
        }

#if UNITY_EDITOR
        private const string PkgCardPath = "Packages/com.almedia.link/Runtime/Prefabs/NotificationCard.prefab";
        private const string PkgOverlayPath = "Packages/com.almedia.link/Runtime/Prefabs/ActivityOverlay.prefab";

        private void OnValidate()
        {
            if (_notificationPollIntervalSeconds < 5)
                _notificationPollIntervalSeconds = DefaultPollInterval;

            SyncNotificationPrefabsWithToggle();
            InvalidateCache();
        }

        // This asset ships via Resources, so a serialized prefab reference here force-includes that
        // prefab in every build regardless of the toggle - the references must track the toggle.
        private void SyncNotificationPrefabsWithToggle()
        {
            if (!_enableDefaultNotificationUI)
            {
                // Host-assigned prefabs survive the toggle. Bundled refs are cleared; so are refs
                // that read null (type mismatch) - their raw serialized reference still
                // force-includes the prefab in a build.
                if (_notificationCardPrefab == null || IsBundled(_notificationCardPrefab))
                    _notificationCardPrefab = null;
                if (_activityOverlayPrefab == null || IsBundled(_activityOverlayPrefab))
                    _activityOverlayPrefab = null;
                return;
            }

            // Re-enabling restores the bundled defaults; a host-assigned variant is left alone.
            if (_notificationCardPrefab == null)
                _notificationCardPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<NotificationCardController>(PkgCardPath);
            if (_activityOverlayPrefab == null)
                _activityOverlayPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<ActivityOverlayController>(PkgOverlayPath);
        }

        private static bool IsBundled(Object asset)
        {
            if (asset == null) return false;
            return UnityEditor.AssetDatabase.GetAssetPath(asset).StartsWith("Packages/com.almedia.link");
        }
#endif
    }
}