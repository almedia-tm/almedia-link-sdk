#if UNITY_ANDROID
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace AlmediaLink.Editor
{
    /// <summary>
    /// Applies <see cref="AlmediaLinkGradleDexerPin"/> to the exported Gradle project
    /// when the build targets minSdk &lt; 24 on AGP 7.x, so publishers can lower their
    /// minimum API without hand-editing Gradle templates. See the pin class for the
    /// underlying AGP/Kotlin incompatibility.
    /// <para>
    /// Any outcome that leaves a minSdk &lt; 24 build unpinned - unrecognizable
    /// template, missing file, I/O failure - is reported to the Console together with
    /// the exact block to add by hand, so the later D8 crash is never the first sign.
    /// </para>
    /// </summary>
    public sealed class AlmediaLinkAndroidGradlePostProcessor : IPostGenerateGradleAndroidProject
    {
        // Run last so we observe the root build.gradle after Unity and every other
        // SDK's project modifier have contributed to it.
        public int callbackOrder => int.MaxValue;

        public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
        {
            if ((int)PlayerSettings.Android.minSdkVersion >= 24) return;

            var rootGradlePath = Path.GetFullPath(Path.Combine(unityLibraryPath, "..", "build.gradle"));
            try
            {
                if (!File.Exists(rootGradlePath))
                {
                    WarnCouldNotPin($"Root build.gradle not found at {rootGradlePath}");
                    return;
                }

                var original = File.ReadAllText(rootGradlePath);
                var decision = AlmediaLinkGradleDexerPin.Decide(original);

                var patched = AlmediaLinkGradleDexerPin.Apply(original);
                if (patched != original) File.WriteAllText(rootGradlePath, patched);

                switch (decision)
                {
                    case AlmediaLinkGradleDexerPin.Decision.ApplyPin:
                        Debug.Log(
                            $"[AlmediaLink] minSdk < 24 on AGP 7.x: pinned {AlmediaLinkGradleDexerPin.R8Coordinate} " +
                            "in the exported root build.gradle so Kotlin 2.x libraries dex correctly.");
                        break;

                    case AlmediaLinkGradleDexerPin.Decision.PublisherPinned:
                        Debug.Log(
                            "[AlmediaLink] The root build.gradle already carries a com.android.tools:r8 " +
                            "classpath pin; leaving it untouched.");
                        break;

                    case AlmediaLinkGradleDexerPin.Decision.ModernAgp:
                        // AGP 8+ dexes Kotlin 2.x metadata natively; nothing to do, nothing to say.
                        break;

                    case AlmediaLinkGradleDexerPin.Decision.UnknownAgp:
                        WarnCouldNotPin(
                            $"Could not determine the Android Gradle Plugin version in {rootGradlePath}");
                        break;
                }
            }
            catch (Exception e)
            {
                WarnCouldNotPin($"Failed to patch {rootGradlePath} ({e.GetType().Name}: {e.Message})");
            }
        }

        private static void WarnCouldNotPin(string reason)
        {
            Debug.LogWarning(
                $"[AlmediaLink] {reason}; the minSdk<24 dexer pin was NOT applied. If the Android build " +
                "fails with \"ERROR:D8: com.android.tools.r8.kotlin.H\", enable Publishing Settings > " +
                "Custom Base Gradle Template and add this at the top of " +
                "Assets/Plugins/Android/baseProjectTemplate.gradle (or, when using Export Project, at the " +
                "top of the exported root build.gradle):\n\n" +
                AlmediaLinkGradleDexerPin.ManualPinSnippet + "\n\n" +
                "See \"Building below minSdk 24\" in the integration guide.");
        }
    }
}
#endif
