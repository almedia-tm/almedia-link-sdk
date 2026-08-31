using UnityEditor;
using UnityEngine;
using AlmediaLink.UI;

namespace AlmediaLink.Editor
{
    /// <summary>
    /// One-shot upgrade passes over host-project assets, keyed on
    /// <c>AlmediaLinkSettings._migrationVersion</c> so each pass runs exactly once per project.
    /// </summary>
    [InitializeOnLoad]
    internal static class AlmediaLinkMigrations
    {
        private const string SettingsPath = "Assets/AlmediaLink/Resources/AlmediaLinkSettings.asset";
        private const int CurrentVersion = 1;

        static AlmediaLinkMigrations()
        {
            // After Bootstrap's delayCall, so a fresh install has its settings asset first.
            EditorApplication.delayCall += () => EditorApplication.delayCall += Run;
        }

        private static void Run()
        {
            var settings = AssetDatabase.LoadAssetAtPath<AlmediaLinkSettings>(SettingsPath);
            if (settings == null) return; // not bootstrapped yet; next reload tries again
            if (settings._migrationVersion >= CurrentVersion) return;

            MigrateApplyHostSettingsExemption(settings);

            settings._migrationVersion = CurrentVersion;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
        }

        /// <summary>
        /// v1 (SDK 1.2.0): under 1.x, a Prefab Variant assigned in a settings override slot was
        /// implicitly exempt from settings theming (the SDK set ApplyHostSettings=false on the
        /// instance). Theming exemption is now an authored checkbox on the prefab itself, so this
        /// pass unticks it once on host-owned variants that were assigned as overrides - preserving
        /// their 1.x appearance. Bundled package prefabs stay themed.
        /// </summary>
        private static void MigrateApplyHostSettingsExemption(AlmediaLinkSettings settings)
        {
            ExemptIfHostVariant(settings.LegacyLinkPopupOverride, v => v.ApplyHostSettings = false);
            ExemptIfHostVariant(settings.NotificationCardPrefab, v => v.ApplyHostSettings = false);
            ExemptIfHostVariant(settings.ActivityOverlayPrefab, v => v.ApplyHostSettings = false);
        }

        private static void ExemptIfHostVariant<T>(T prefab, System.Action<T> untick) where T : MonoBehaviour
        {
            if (prefab == null) return;

            var path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path) || path.StartsWith("Packages/com.almedia.link"))
                return; // bundled default - keeps receiving settings theming, as in 1.x

            untick(prefab);
            EditorUtility.SetDirty(prefab);
            PrefabUtility.SavePrefabAsset(prefab.gameObject);
            Debug.Log($"[AlmediaLink] Migration: unticked 'Apply Host Settings' on '{path}' to preserve " +
                      "its 1.x appearance (override variants were exempt from settings theming).");
        }
    }
}
