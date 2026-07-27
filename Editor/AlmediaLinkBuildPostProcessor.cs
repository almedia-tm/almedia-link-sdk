#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace AlmediaLink.Editor
{
    /// <summary>
    /// Post-build hook for iOS. The SDK reads IDFA gated behind App Tracking Transparency,
    /// which iOS only permits when <c>NSUserTrackingUsageDescription</c> is present in the
    /// app's Info.plist. This hook ensures that key exists in the generated Xcode project.
    /// <para>
    /// If the host app (or another SDK) has already supplied its own
    /// <c>NSUserTrackingUsageDescription</c> via Player Settings → iOS → Custom Info.plist
    /// entries, or another plugin's post-processor, this hook leaves the existing entry
    /// untouched. It runs after all other post-processors (see <see cref="CallbackOrder"/>)
    /// so it observes the final merged Info.plist.
    /// </para>
    /// </summary>
    public static class AlmediaLinkBuildPostProcessor
    {
        private const string UsageDescriptionKey = "NSUserTrackingUsageDescription";

        private const string DefaultUsageDescription =
            "Tracking lets us credit your rewards correctly and faster.";

        // Run last so we observe the final merged Info.plist after Unity and every other
        // SDK's post-processor have contributed their entries: we only add a default when
        // nobody else set the key, and we never overwrite an existing one.
        private const int CallbackOrder = int.MaxValue;

        [PostProcessBuild(CallbackOrder)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

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
                    "leaving the existing entry untouched.");
                return;
            }

            plist.root.SetString(UsageDescriptionKey, DefaultUsageDescription);
            plist.WriteToFile(plistPath);

            Debug.Log(
                $"[AlmediaLink] Added a default {UsageDescriptionKey} to Info.plist " +
                "(required for ATT-gated IDFA reading). To customize the copy, add your own " +
                "NSUserTrackingUsageDescription entry via Player Settings → iOS.");
        }
    }
}
#endif
