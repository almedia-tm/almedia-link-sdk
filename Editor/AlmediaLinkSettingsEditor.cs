using System;
using UnityEditor;
using UnityEngine;

namespace AlmediaLink.Editor
{
    public class AlmediaLinkSettingsEditor : EditorWindow
    {
        private const string AssetPath = "Assets/AlmediaLink/Resources/AlmediaLinkSettings.asset";
        private const string PrefPrefix = "com.almedialink.";

        private SerializedObject _serializedObject;
        private Vector2 _scrollPosition;

        // Styles - GUIStyle does not survive domain reload reliably, so we
        // rebuild every OnGUI. No persistent flag.
        private GUIStyle _titleStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _envValueStyle;

        // SDK Configuration
        private SerializedProperty _iosIntegrationKey;
        private SerializedProperty _androidIntegrationKey;
        private SerializedProperty _notificationPollIntervalSeconds;
        private SerializedProperty _enableDefaultNotificationUI;
        private SerializedProperty _canRunConsentFlow;

        // UI Text
        private SerializedProperty _popupTitle;
        private SerializedProperty _benefit1Title;
        private SerializedProperty _benefit1Description;
        private SerializedProperty _benefit2Title;
        private SerializedProperty _benefit2Description;
        private SerializedProperty _ctaButtonText;
        private SerializedProperty _overlayTitle;
        private SerializedProperty _popupBackgroundColor;
        private SerializedProperty _ctaButtonColor;
        private SerializedProperty _ctaButtonTextColor;

        // Notifications
        private SerializedProperty _notificationBackgroundColor;

        // ATT Pre-Prompt Text
        private SerializedProperty _attPromptTitle;
        private SerializedProperty _attRewardAmount;
        private SerializedProperty _attWhyTitle;
        private SerializedProperty _attWhyBody;
        private SerializedProperty _attControlTitle;
        private SerializedProperty _attControlBody;
        private SerializedProperty _attContinueButtonText;
        private SerializedProperty _attBackgroundColor;
        private SerializedProperty _attPrimaryButtonColor;
        private SerializedProperty _attButtonTextColor;

        // Prefab Overrides
        private SerializedProperty _linkPopupOverride;
        private SerializedProperty _notificationCardOverride;
        private SerializedProperty _activityOverlayOverride;
        private SerializedProperty _attPrePromptOverride;

        [MenuItem("Almedia/Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<AlmediaLinkSettingsEditor>(true, "Almedia Link SDK");
            window.minSize = new Vector2(500, 450);
            window.Show();
        }

        private void OnEnable()
        {
            LoadSettings();
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        private void OnFocus()
        {
            LoadSettings();
        }

        private void OnProjectChanged()
        {
            LoadSettings();
            Repaint();
        }

        private void InitStyles()
        {
            _titleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 20
            };

            _headerStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 18
            };

            _envValueStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight
            };
        }

        private void LoadSettings()
        {
            var asset = GetOrCreateSettings();
            if (asset == null) return;

            _serializedObject = new SerializedObject(asset);

            _iosIntegrationKey = _serializedObject.FindProperty("_iosIntegrationKey");
            _androidIntegrationKey = _serializedObject.FindProperty("_androidIntegrationKey");
            _notificationPollIntervalSeconds = _serializedObject.FindProperty("_notificationPollIntervalSeconds");
            _enableDefaultNotificationUI = _serializedObject.FindProperty("_enableDefaultNotificationUI");
            _canRunConsentFlow = _serializedObject.FindProperty("_canRunConsentFlow");

            _popupTitle = _serializedObject.FindProperty("_popupTitle");
            _benefit1Title = _serializedObject.FindProperty("_benefit1Title");
            _benefit1Description = _serializedObject.FindProperty("_benefit1Description");
            _benefit2Title = _serializedObject.FindProperty("_benefit2Title");
            _benefit2Description = _serializedObject.FindProperty("_benefit2Description");
            _ctaButtonText = _serializedObject.FindProperty("_ctaButtonText");
            _overlayTitle = _serializedObject.FindProperty("_overlayTitle");
            _popupBackgroundColor = _serializedObject.FindProperty("_popupBackgroundColor");
            _ctaButtonColor = _serializedObject.FindProperty("_ctaButtonColor");
            _ctaButtonTextColor = _serializedObject.FindProperty("_ctaButtonTextColor");

            _notificationBackgroundColor = _serializedObject.FindProperty("_notificationBackgroundColor");

            _attPromptTitle = _serializedObject.FindProperty("_attPromptTitle");
            _attRewardAmount = _serializedObject.FindProperty("_attRewardAmount");
            _attWhyTitle = _serializedObject.FindProperty("_attWhyTitle");
            _attWhyBody = _serializedObject.FindProperty("_attWhyBody");
            _attControlTitle = _serializedObject.FindProperty("_attControlTitle");
            _attControlBody = _serializedObject.FindProperty("_attControlBody");
            _attContinueButtonText = _serializedObject.FindProperty("_attContinueButtonText");
            _attBackgroundColor = _serializedObject.FindProperty("_attBackgroundColor");
            _attPrimaryButtonColor = _serializedObject.FindProperty("_attPrimaryButtonColor");
            _attButtonTextColor = _serializedObject.FindProperty("_attButtonTextColor");

            _linkPopupOverride = _serializedObject.FindProperty("_linkPopupOverride");
            _notificationCardOverride = _serializedObject.FindProperty("_notificationCardOverride");
            _activityOverlayOverride = _serializedObject.FindProperty("_activityOverlayOverride");
            _attPrePromptOverride = _serializedObject.FindProperty("_attPrePromptOverride");
        }

        private void OnGUI()
        {
            InitStyles();

            if (_serializedObject == null || _serializedObject.targetObject == null)
            {
                EditorGUILayout.HelpBox("Settings asset not found.", MessageType.Warning);
                if (GUILayout.Button("Create Settings Asset"))
                    LoadSettings();
                return;
            }

            _serializedObject.Update();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Header
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Almedia Link SDK", _titleStyle);
            EditorGUILayout.LabelField($"v{AlmediaLinkSDK.Version}", EditorStyles.miniLabel);
            GUILayout.Space(8);

            // Sections
            DrawStaticSection("SDK Configuration", DrawSDKConfiguration);
            GUILayout.Space(4);
            DrawCollapsibleSection("show_ui_text", "Link Popup Text", DrawUIText);
            GUILayout.Space(4);
            DrawCollapsibleSection("show_notifications", "Notifications", DrawNotifications, false);
            GUILayout.Space(4);
            DrawCollapsibleSection("show_att_text", "ATT Pre-Prompt Text (iOS)", DrawATTPrePrompt, false);
            GUILayout.Space(4);
            DrawCollapsibleSection("show_prefab_overrides", "Prefab Overrides", DrawPrefabOverrides, false);
            GUILayout.Space(8);
            DrawEnvironment();

            EditorGUILayout.EndScrollView();

            if (GUI.changed)
            {
                _serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_serializedObject.targetObject);
            }
        }

        #region Sections

        private void DrawSDKConfiguration()
        {
            var prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 220;

            DrawFieldWithWarning(_iosIntegrationKey, "iOS Integration Key", "iOS Integration Key is required.");
            DrawFieldWithWarning(_androidIntegrationKey, "Android Integration Key", "Android Integration Key is required.");
            DrawField(_notificationPollIntervalSeconds, "Polling Interval (sec)");
            DrawField(_enableDefaultNotificationUI, "Enable Default Notification UI");
            DrawField(_canRunConsentFlow, "Enable Consent Flow (iOS ATT)");

            EditorGUIUtility.labelWidth = prevLabelWidth;
        }

        private void DrawUIText()
        {
            DrawField(_popupTitle, "Popup Title");
            GUILayout.Space(4);
            EditorGUILayout.LabelField("Benefit 1", EditorStyles.miniBoldLabel);
            DrawField(_benefit1Title, "Title");
            DrawTextArea(_benefit1Description, "Description");
            GUILayout.Space(2);
            EditorGUILayout.LabelField("Benefit 2", EditorStyles.miniBoldLabel);
            DrawField(_benefit2Title, "Title");
            DrawTextArea(_benefit2Description, "Description");
            GUILayout.Space(4);
            DrawField(_ctaButtonText, "CTA Button Text");
            GUILayout.Space(4);
            DrawField(_overlayTitle, "Activity Overlay Title");
            GUILayout.Space(6);
            DrawField(_popupBackgroundColor, "Background Color");
            DrawField(_ctaButtonColor, "CTA Button Color");
            DrawField(_ctaButtonTextColor, "CTA Text Color");
        }

        private void DrawNotifications()
        {
            DrawField(_notificationBackgroundColor, "Background Color");
        }

        private void DrawATTPrePrompt()
        {
            DrawField(_attPromptTitle, "Title");
            DrawField(_attRewardAmount, "Reward Amount");
            DrawField(_attWhyTitle, "Why Title");
            DrawTextArea(_attWhyBody, "Why Body");
            DrawField(_attControlTitle, "Control Title");
            DrawTextArea(_attControlBody, "Control Body");
            DrawField(_attContinueButtonText, "Continue Button Text");
            GUILayout.Space(6);
            DrawField(_attBackgroundColor, "Background Color");
            DrawField(_attPrimaryButtonColor, "Primary Button Color");
            DrawField(_attButtonTextColor, "Button Text Color");
        }

        private void DrawPrefabOverrides()
        {
            EditorGUILayout.HelpBox(
                "Optional. Assign Prefab Variants of the SDK base prefabs to customize the UI. " +
                "Leave empty to use the built-in defaults. Variants automatically receive SDK updates " +
                "for non-overridden properties.",
                MessageType.Info);
            DrawField(_linkPopupOverride, "Link Popup");
            DrawField(_notificationCardOverride, "Notification Card");
            DrawField(_activityOverlayOverride, "Activity Overlay");
            DrawField(_attPrePromptOverride, "ATT Pre-Prompt");
        }

        private void DrawEnvironment()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Environment", _headerStyle);
                GUILayout.Space(2);
                DrawEnvRow("Unity Version", Application.unityVersion);
                DrawEnvRow("Platform", EditorUserBuildSettings.activeBuildTarget.ToString());
                DrawEnvRow("Scripting Backend",
                    PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup).ToString());
                DrawEnvRow("SDK Version", AlmediaLinkSDK.Version);
            }
        }

        #endregion

        #region Helpers

        private void DrawStaticSection(string title, Action drawContent)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(title, _headerStyle);
                GUILayout.Space(4);
                drawContent();
            }
        }

        private void DrawCollapsibleSection(string key, string title, Action drawContent, bool defaultOpen = true)
        {
            string prefKey = PrefPrefix + key;
            bool expanded = EditorPrefs.GetBool(prefKey, defaultOpen);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.BeginHorizontal();
                bool newExpanded = EditorGUILayout.Foldout(expanded, "", true);
                EditorGUILayout.LabelField(title, _headerStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                if (newExpanded != expanded)
                    EditorPrefs.SetBool(prefKey, newExpanded);

                if (newExpanded)
                {
                    GUILayout.Space(4);
                    drawContent();
                }
            }
        }

        /// <summary>
        /// Draws a property field without rendering [Header] or other decorator attributes.
        /// Uses typed drawing methods instead of PropertyField to skip decorators.
        /// </summary>
        internal static void DrawField(SerializedProperty prop, string label)
        {
            var content = new GUIContent(label);
            switch (prop.propertyType)
            {
                case SerializedPropertyType.String:
                    prop.stringValue = EditorGUILayout.TextField(content, prop.stringValue);
                    break;
                case SerializedPropertyType.Integer:
                    prop.intValue = EditorGUILayout.IntField(content, prop.intValue);
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = EditorGUILayout.Toggle(content, prop.boolValue);
                    break;
                case SerializedPropertyType.Float:
                    prop.floatValue = EditorGUILayout.FloatField(content, prop.floatValue);
                    break;
                case SerializedPropertyType.Color:
                    prop.colorValue = EditorGUILayout.ColorField(content, prop.colorValue);
                    break;
                default:
                    EditorGUILayout.PropertyField(prop, content);
                    break;
            }
        }

        internal static void DrawTextArea(SerializedProperty prop, string label)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth));
            prop.stringValue = EditorGUILayout.TextArea(prop.stringValue, EditorStyles.textArea, GUILayout.MinHeight(40));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawFieldWithWarning(SerializedProperty prop, string label, string warning)
        {
            DrawField(prop, label);
            if (string.IsNullOrWhiteSpace(prop.stringValue))
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
        }

        private void DrawEnvRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label);
            EditorGUILayout.LabelField(value, _envValueStyle);
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Settings Asset

        private static AlmediaLinkSettings GetOrCreateSettings()
        {
            // Idempotent - copies from package defaults if the host-side asset is missing.
            AlmediaLinkBootstrap.EnsureSettings();

            var asset = AssetDatabase.LoadAssetAtPath<AlmediaLinkSettings>(AssetPath);
            if (asset != null) return asset;

            // Last-resort fallback: package defaults missing too. Create an empty asset
            // so the editor window still renders. Should not happen in normal installs.
            asset = ScriptableObject.CreateInstance<AlmediaLinkSettings>();

            var dir = System.IO.Path.GetDirectoryName(AssetPath);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.LogWarning($"[AlmediaLink] Package defaults missing - created empty settings asset at {AssetPath}");
            return asset;
        }

        #endregion
    }
}
