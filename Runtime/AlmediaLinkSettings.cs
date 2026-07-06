using AlmediaLink.UI;
using UnityEngine;

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

        [Tooltip("Whether the SDK should run the consent flow (e.g. iOS ATT pre-prompt + system dialog).")]
        [SerializeField] private bool _canRunConsentFlow;

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

        [Header("ATT Pre-Prompt Text (iOS Only)")]
        [SerializeField] private string _attPromptTitle = "Don't miss your rewards by enabling app tracking";
        [SerializeField] private string _attRewardAmount = "$9.38";
        [SerializeField] private string _attWhyTitle = "Why do we need this?";
        [TextArea(2, 4)]
        [SerializeField] private string _attWhyBody = "Tracking lets us make sure your rewards can be credited correctly and faster.";
        [SerializeField] private string _attControlTitle = "You're in control";
        [TextArea(2, 4)]
        [SerializeField] private string _attControlBody = "Change permissions at any time in Settings.";
        [SerializeField] private string _attContinueButtonText = "Continue";

        [Tooltip("ATT Pre-Prompt background color.")]
        [SerializeField] private Color _attBackgroundColor = new Color32(0x12, 0x12, 0x12, 0xFF);

        [Tooltip("ATT Pre-Prompt primary (Continue) button background color.")]
        [SerializeField] private Color _attPrimaryButtonColor = new Color32(0x00, 0xC8, 0x53, 0xFF);

        [Tooltip("ATT Pre-Prompt primary button text color.")]
        [SerializeField] private Color _attButtonTextColor = Color.white;

        [Header("Prefab Overrides (optional - assign Prefab Variants to customize UI)")]
        [Tooltip("Optional Prefab Variant of LinkPopup to use instead of the SDK default. Leave empty to use the built-in prefab.")]
        [SerializeField] private LinkPopupController _linkPopupOverride;

        [Tooltip("Optional Prefab Variant of NotificationCard to use instead of the SDK default. Leave empty to use the built-in prefab.")]
        [SerializeField] private NotificationCardController _notificationCardOverride;

        [Tooltip("Optional Prefab Variant of ActivityOverlay to use instead of the SDK default. Leave empty to use the built-in prefab.")]
        [SerializeField] private ActivityOverlayController _activityOverlayOverride;

        [Tooltip("Optional Prefab Variant of ATTPrePrompt to use instead of the SDK default. Leave empty to use the built-in prefab.")]
        [SerializeField] private ATTPrePromptController _attPrePromptOverride;

        public string IosIntegrationKey => _iosIntegrationKey;
        public string AndroidIntegrationKey => _androidIntegrationKey;
        public int NotificationPollIntervalSeconds => _notificationPollIntervalSeconds;
        public bool EnableDefaultNotificationUI => _enableDefaultNotificationUI;
        public bool CanRunConsentFlow => _canRunConsentFlow;

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

        public string AttPromptTitle => _attPromptTitle;
        public string AttRewardAmount => _attRewardAmount;
        public string AttWhyTitle => _attWhyTitle;
        public string AttWhyBody => _attWhyBody;
        public string AttControlTitle => _attControlTitle;
        public string AttControlBody => _attControlBody;
        public string AttContinueButtonText => _attContinueButtonText;
        public Color AttBackgroundColor => _attBackgroundColor;
        public Color AttPrimaryButtonColor => _attPrimaryButtonColor;
        public Color AttButtonTextColor => _attButtonTextColor;

        public LinkPopupController LinkPopupOverride => _linkPopupOverride;
        public NotificationCardController NotificationCardOverride => _notificationCardOverride;
        public ActivityOverlayController ActivityOverlayOverride => _activityOverlayOverride;
        public ATTPrePromptController AttPrePromptOverride => _attPrePromptOverride;

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
        private void OnValidate()
        {
            if (_notificationPollIntervalSeconds < 5)
                _notificationPollIntervalSeconds = DefaultPollInterval;

            InvalidateCache();
        }
#endif
    }
}