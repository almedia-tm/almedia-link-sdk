using UnityEditor;
using UnityEngine;

namespace AlmediaLink.Editor
{
    [CustomEditor(typeof(AlmediaLinkSettings))]
    public class AlmediaLinkSettingsInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 220;

            EditorGUILayout.LabelField("SDK Configuration", EditorStyles.boldLabel);
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_iosIntegrationKey"), "iOS Integration Key");
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_androidIntegrationKey"), "Android Integration Key");
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_notificationPollIntervalSeconds"), "Polling Interval (sec)");
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_enableDefaultNotificationUI"), "Enable Default Notification UI");
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_canRunConsentFlow"), "Enable Consent Flow (iOS ATT)");

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Link Popup Text", EditorStyles.boldLabel);
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_popupTitle"), "Popup Title");
            GUILayout.Space(2);
            EditorGUILayout.LabelField("Benefit 1", EditorStyles.miniBoldLabel);
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_benefit1Title"), "Title");
            AlmediaLinkSettingsEditor.DrawTextArea(serializedObject.FindProperty("_benefit1Description"), "Description");
            GUILayout.Space(2);
            EditorGUILayout.LabelField("Benefit 2", EditorStyles.miniBoldLabel);
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_benefit2Title"), "Title");
            AlmediaLinkSettingsEditor.DrawTextArea(serializedObject.FindProperty("_benefit2Description"), "Description");
            GUILayout.Space(2);
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_ctaButtonText"), "CTA Button Text");
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_overlayTitle"), "Activity Overlay Title");
            GUILayout.Space(4);
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_popupBackgroundColor"), "Background Color");
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_ctaButtonColor"), "CTA Button Color");
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_ctaButtonTextColor"), "CTA Text Color");

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Notifications", EditorStyles.boldLabel);
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_notificationBackgroundColor"), "Background Color");

            GUILayout.Space(8);
            EditorGUILayout.LabelField("ATT Pre-Prompt Text (iOS)", EditorStyles.boldLabel);
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_attPromptTitle"), "Title");
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_attRewardAmount"), "Reward Amount");
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_attWhyTitle"), "Why Title");
            AlmediaLinkSettingsEditor.DrawTextArea(serializedObject.FindProperty("_attWhyBody"), "Why Body");
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_attControlTitle"), "Control Title");
            AlmediaLinkSettingsEditor.DrawTextArea(serializedObject.FindProperty("_attControlBody"), "Control Body");
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_attContinueButtonText"), "Continue Button Text");
            GUILayout.Space(4);
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_attBackgroundColor"), "Background Color");
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_attPrimaryButtonColor"), "Primary Button Color");
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_attButtonTextColor"), "Button Text Color");

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Prefab Overrides (optional)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Assign Prefab Variants of the SDK base prefabs to customize the UI. " +
                "Leave empty to use the built-in defaults. Variants automatically receive SDK updates.",
                MessageType.Info);
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_linkPopupOverride"), "Link Popup");
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_notificationCardOverride"), "Notification Card");
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_activityOverlayOverride"), "Activity Overlay");
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_attPrePromptOverride"), "ATT Pre-Prompt");

            EditorGUIUtility.labelWidth = prevLabelWidth;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
