using System.IO;
using UnityEditor;
using UnityEngine;

namespace AlmediaLink.Editor
{
    /// <summary>
    /// Seeds a default copy of <c>AlmediaLinkSettings</c> into the host project's
    /// <c>Assets/Almedia/Resources/</c> on first editor reload after the package is installed,
    /// and moves a 1.x settings asset from <c>Assets/AlmediaLink/Resources/</c> to that folder
    /// with its GUID unchanged. Never overwrites an existing host copy. A 1.x asset that cannot be
    /// moved stays in use, and no default is seeded beside it.
    /// </summary>
    [InitializeOnLoad]
    internal static class AlmediaLinkBootstrap
    {
        private const string TargetDir   = "Assets/Almedia/Resources";
        private const string AssetName   = "AlmediaSettings";
        private const string TargetAsset = TargetDir + "/" + AssetName + ".asset";
        private const string PkgDefaults = AlmediaSDK.AlmediaPackage.Root + "/Editor/Defaults";
        private const string LegacyDir   = "Assets/AlmediaLink";
        private const string LegacyAsset = LegacyDir + "/Resources/AlmediaLinkSettings.asset";

        private static string _lastWarning;

        static AlmediaLinkBootstrap()
        {
            EditorApplication.delayCall += EnsureSettings;
        }

        /// <summary>The settings asset the runtime loads: the new path when it exists, else a 1.x asset.</summary>
        internal static string SettingsAssetPath()
            => !File.Exists(TargetAsset) && File.Exists(LegacyAsset) ? LegacyAsset : TargetAsset;

        /// <summary>Idempotent.</summary>
        internal static void EnsureSettings()
        {
            bool moved = MoveLegacySettings();
            bool created = !File.Exists(LegacyAsset) && EnsureOne("AlmediaLinkSettings", AssetName);

            if (moved || created)
                AssetDatabase.Refresh();

            if (created)
                Debug.Log($"[Almedia] Default settings created at {TargetDir}. " +
                          $"Edit {AssetName}.asset to configure your integration.");
        }

        private static bool MoveLegacySettings()
        {
            string dst = TargetAsset;
            if (!File.Exists(LegacyAsset))
                return false;
            if (File.Exists(dst))
            {
                WarnOnce($"[Almedia] Both {LegacyAsset} and {dst} exist. The SDK uses {dst}; delete {LegacyAsset}.");
                return false;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Almedia"))
                AssetDatabase.CreateFolder("Assets", "Almedia");
            if (!AssetDatabase.IsValidFolder(TargetDir))
                AssetDatabase.CreateFolder("Assets/Almedia", "Resources");

            string error = AssetDatabase.MoveAsset(LegacyAsset, dst);
            if (!string.IsNullOrEmpty(error))
            {
                WarnOnce($"[Almedia] Could not move {LegacyAsset} to {dst}: {error}. " +
                         $"The SDK keeps using {LegacyAsset}; the move is retried on the next editor load.");
                return false;
            }

            var moved = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dst);
            if (moved != null)
            {
                moved.name = AssetName;
                EditorUtility.SetDirty(moved);
                AssetDatabase.SaveAssetIfDirty(moved);
            }

            DeleteIfEmpty($"{LegacyDir}/Resources");
            bool legacyRemoved = DeleteIfEmpty(LegacyDir);

            Debug.Log($"[Almedia] Settings moved from {LegacyAsset} to {dst}." +
                      (legacyRemoved ? "" : $" {LegacyDir} still contains other files and was left in place."));
            return true;
        }

        private static void WarnOnce(string message)
        {
            if (message == _lastWarning) return;
            _lastWarning = message;
            Debug.LogWarning(message);
        }

        private static bool DeleteIfEmpty(string folder)
        {
            if (!Directory.Exists(folder) || Directory.GetFileSystemEntries(folder).Length > 0)
                return false;
            return AssetDatabase.DeleteAsset(folder);
        }

        private static bool EnsureOne(string srcName, string assetName)
        {
            string src = $"{PkgDefaults}/{srcName}.default.asset";
            string dst = $"{TargetDir}/{assetName}.asset";

            if (File.Exists(dst) || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dst) != null)
            {
                // Host already has its own copy; never overwrite.
                return false;
            }

            if (!File.Exists(src))
            {
                Debug.LogError($"[Almedia] Default asset missing in package: {src}. " +
                               "This is a packaging defect - please reinstall the package.");
                return false;
            }

            Directory.CreateDirectory(TargetDir);
            bool result = AssetDatabase.CopyAsset(src, dst);
            if (!result)
            {
                Debug.LogError($"[Almedia] Failed to copy default asset from {src} to {dst}");
                return false;
            }

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
