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
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_autoInitializeFromPrefab"), "Auto-Initialize From Prefabs");

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
            EditorGUILayout.LabelField("Default UI Prefabs", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The notification UI the SDK spawns when the default UI is enabled. Assign Prefab " +
                "Variants to customize; disabling the toggle clears these so the prefabs stay out of " +
                "your build. The Link Popup is configured on the LinkButton prefab itself.",
                MessageType.Info);
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_notificationCardPrefab"), "Notification Card");
            AlmediaLinkSettingsEditor.DrawField(serializedObject.FindProperty("_activityOverlayPrefab"), "Activity Overlay");

            EditorGUIUtility.labelWidth = prevLabelWidth;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
