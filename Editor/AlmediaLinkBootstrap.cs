using System.IO;
using UnityEditor;
using UnityEngine;

namespace AlmediaLink.Editor
{
    /// <summary>
    /// Seeds a default copy of <c>AlmediaLinkSettings</c>
    /// into the host project's <c>Assets/AlmediaLink/Resources/</c> on first editor
    /// reload after the package is installed. Never overwrites existing host copies.
    /// </summary>
    [InitializeOnLoad]
    internal static class AlmediaLinkBootstrap
    {
        private const string TargetDir   = "Assets/AlmediaLink/Resources";
        private const string PkgDefaults = "Packages/com.almedia.link/Editor/Defaults";
        
        static AlmediaLinkBootstrap()
        {
            // Defer until after AssetDatabase has finished its post-import work.
            EditorApplication.delayCall += EnsureSettings;
        }

        /// <summary>
        /// Idempotent: copies missing default assets from the package into the host's
        /// <c>Assets/AlmediaLink/Resources/</c>. Internal so other editor tooling
        /// (e.g. <c>AlmediaLinkSettingsEditor</c>) can invoke it on demand.
        /// </summary>
        internal static void EnsureSettings()
        {
            bool changed = false;
            changed |= EnsureOne("AlmediaLinkSettings");

            if (changed)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[AlmediaLink] Default settings created at {TargetDir}. " +
                          "Edit AlmediaLinkSettings.asset to configure your integration.");
            }
        }

        private static bool EnsureOne(string assetName)
        {
            string src = $"{PkgDefaults}/{assetName}.default.asset";
            string dst = $"{TargetDir}/{assetName}.asset";

            if (File.Exists(dst) || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dst) != null)
            {
                // Host already has its own copy; never overwrite.
                return false;
            }

            if (!File.Exists(src))
            {
                Debug.LogError($"[AlmediaLink] Default asset missing in package: {src}. " +
                               "This is a packaging defect - please reinstall the package.");
                return false;
            }

            Directory.CreateDirectory(TargetDir);
            bool result = AssetDatabase.CopyAsset(src, dst);
            if (!result)
            {
                Debug.LogError($"[AlmediaLink] Failed to copy default asset from {src} to {dst}");
                return false;
            }

            // Rename the copy so its m_Name matches the destination filename - otherwise Unity warns on every import.
            var copy = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dst);
            if (copy != null)
            {
                copy.name = assetName;
                EditorUtility.SetDirty(copy);
                AssetDatabase.SaveAssetIfDirty(copy);
            }

            return true;
        }
    }
}
