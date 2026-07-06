#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace AlmediaLink.Editor
{
    /// <summary>
    /// Post-build hook for iOS. When <see cref="AlmediaLinkSettings.CanRunConsentFlow"/> is
    /// enabled, ensures <c>NSUserTrackingUsageDescription</c> is present in the generated
    /// Xcode Info.plist so the SDK's ATT flow can complete.
    /// <para>
    /// If the host app has already supplied its own <c>NSUserTrackingUsageDescription</c>
    /// (for example, via Player Settings → iOS → Custom Info.plist entries or another
    /// plugin's post-processor), this hook leaves the existing entry untouched.
    /// </para>
    /// </summary>
    public static class AlmediaLinkBuildPostProcessor
    {
        private const string UsageDescriptionKey = "NSUserTrackingUsageDescription";

        private const string DefaultUsageDescription =
            "Tracking lets us credit your rewards correctly and faster.";

        // Run late so we observe the final merged Info.plist after Unity and other
        // plugins have contributed their entries.
        private const int CallbackOrder = 100;

        [PostProcessBuild(CallbackOrder)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            var settings = AlmediaLinkSettings.Load();
            if (settings == null)
            {
                Debug.LogWarning(
                    "[AlmediaLink] Settings asset not found; skipping iOS post-process. " +
                    "Open Almedia → Settings to create it.");
                return;
            }

            if (!settings.CanRunConsentFlow)
            {
                // The SDK is not running the consent flow; do not touch the host app's Info.plist.
                return;
            }

            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            if (!File.Exists(plistPath))
            {
                Debug.LogWarning($"[AlmediaLink] Info.plist not found at {plistPath}; skipping post-process.");
                return;
            }

            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            if (plist.root.values.ContainsKey(UsageDescriptionKey))
            {
                Debug.Log(
                    $"[AlmediaLink] {UsageDescriptionKey} already present in Info.plist; " +
                    "leaving host-app entry untouched.");
                return;
            }

            plist.root.SetString(UsageDescriptionKey, DefaultUsageDescription);
            plist.WriteToFile(plistPath);

            Debug.Log(
                $"[AlmediaLink] Added {UsageDescriptionKey} to Info.plist " +
                "(CanRunConsentFlow=true). To customize the copy, add your own " +
                "NSUserTrackingUsageDescription entry via Player Settings.");
        }
    }
}
#endif
