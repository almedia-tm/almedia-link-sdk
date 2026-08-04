using System.Text.RegularExpressions;

namespace AlmediaLink.Editor
{
    /// <summary>
    /// Decides whether, and how, to pin a modern D8/R8 into an exported Android
    /// Gradle project.
    /// <para>
    /// Unity 2022.3 ships AGP 7.4.2, whose embedded dexer crashes on the metadata of
    /// any library built with Kotlin 1.9 or newer (<c>ERROR:D8:
    /// com.android.tools.r8.kotlin.H</c>) - but only when minSdk &lt; 24, because that
    /// is when interface-method desugaring parses library metadata. The Link SDK's
    /// AARs and their kotlinx dependencies are Kotlin 2.x, so such builds fail without
    /// a pinned dexer and work at minSdk 24+ without one.
    /// </para>
    /// <para>
    /// The injected block is fenced by ALMEDIA_LINK_R8_PIN markers so it can be
    /// recognized again later: a stale block from an older SDK version is replaced in
    /// place, and a block that is no longer needed (the project moved to AGP 8+, or a
    /// publisher pin appeared) is removed. Any <c>com.android.tools:r8</c> pin
    /// outside the markers is the publisher's and always wins.
    /// </para>
    /// <para>
    /// Pure string-in/string-out so the whole decision is unit-testable without
    /// running an Android build.
    /// </para>
    /// </summary>
    public static class AlmediaLinkGradleDexerPin
    {
        public const string R8Coordinate = "com.android.tools:r8:8.3.37";

        public const string MarkerBegin = "// >>> ALMEDIA_LINK_R8_PIN";
        public const string MarkerEnd = "// <<< ALMEDIA_LINK_R8_PIN";

        /// <summary>
        /// The bare pin, exactly as a publisher would write it by hand. Shown in the
        /// Console warning when the pin cannot be applied automatically, and the body
        /// of the injected block. A hand-pasted copy carries no markers, so the SDK
        /// treats it as the publisher's own pin and never touches it.
        /// </summary>
        public const string ManualPinSnippet =
            "buildscript {\n" +
            "    repositories {\n" +
            "        google()\n" +
            "        mavenCentral()\n" +
            "    }\n" +
            "    dependencies {\n" +
            "        classpath '" + R8Coordinate + "'\n" +
            "    }\n" +
            "}";

        private const string PinBlock =
            MarkerBegin + " (added by the Almedia Link SDK - do not edit between the markers)\n" +
            "// AGP 7.x's embedded D8 crashes on Kotlin >= 1.9 metadata when desugaring for\n" +
            "// minSdk < 24 (ERROR:D8: com.android.tools.r8.kotlin.H). Pin a modern dexer\n" +
            "// ahead of it on the classpath.\n" +
            "// To take over: replace this whole block with your own com.android.tools:r8\n" +
            "// classpath pin (with any repositories you need) - the SDK never overrides an\n" +
            "// existing pin and stops injecting this block once it sees yours.\n" +
            ManualPinSnippet + "\n" +
            MarkerEnd + "\n\n";

        /// <summary>What <see cref="Apply"/> will do with a given root build.gradle, for logging.</summary>
        public enum Decision
        {
            /// <summary>AGP 7.x or older without a publisher pin - inject (or refresh) the fenced block.</summary>
            ApplyPin,

            /// <summary>A com.android.tools:r8 pin outside our markers - the publisher's; leave it alone.</summary>
            PublisherPinned,

            /// <summary>AGP 8+ embeds a dexer newer than our pin - injecting could downgrade it.</summary>
            ModernAgp,

            /// <summary>The AGP version could not be determined - nothing is injected rather than
            /// guessed at; the caller should tell the publisher how to pin by hand.</summary>
            UnknownAgp,
        }

        // Unity 2022+ declares AGP via the plugins DSL; older templates via the
        // legacy buildscript classpath. Cover both.
        private static readonly Regex PluginsDslAgp =
            new Regex(@"id\s+['""]com\.android\.application['""]\s+version\s+['""](\d+)\.");

        private static readonly Regex LegacyClasspathAgp =
            new Regex(@"com\.android\.tools\.build:gradle:(\d+)\.");

        // Our fenced block, including the blank line that separates it from the
        // original content, tolerating CRLF conversion by publisher tooling.
        private static readonly Regex OwnBlock =
            new Regex(
                Regex.Escape(MarkerBegin) + ".*?" + Regex.Escape(MarkerEnd) + @"\s*",
                RegexOptions.Singleline);

        /// <summary>
        /// The AGP major version declared in the root build.gradle, or null when it
        /// cannot be determined.
        /// </summary>
        public static int? AgpMajorVersion(string rootGradleContent)
        {
            if (rootGradleContent == null) return null;

            var match = PluginsDslAgp.Match(rootGradleContent);
            if (!match.Success) match = LegacyClasspathAgp.Match(rootGradleContent);

            return match.Success ? int.Parse(match.Groups[1].Value) : (int?)null;
        }

        /// <summary>
        /// Classifies the root build.gradle. The decision ignores our own fenced block,
        /// so a previously patched export is re-decided from scratch each time.
        /// </summary>
        public static Decision Decide(string rootGradleContent)
        {
            if (string.IsNullOrEmpty(rootGradleContent)) return Decision.UnknownAgp;

            var withoutOurs = RemoveOwnBlock(rootGradleContent);
            if (withoutOurs.Contains("com.android.tools:r8")) return Decision.PublisherPinned;

            var major = AgpMajorVersion(withoutOurs);
            if (!major.HasValue) return Decision.UnknownAgp;

            return major.Value < 8 ? Decision.ApplyPin : Decision.ModernAgp;
        }

        /// <summary>
        /// True when the pin belongs in this root build.gradle: an AGP 7.x or older
        /// project without a publisher pin.
        /// </summary>
        public static bool ShouldApply(string rootGradleContent) =>
            Decide(rootGradleContent) == Decision.ApplyPin;

        /// <summary>
        /// The content with the fenced pin block prepended, refreshed, or removed as
        /// <see cref="Decide"/> dictates, or the original reference when there is
        /// nothing to change. Idempotent.
        /// </summary>
        public static string Apply(string rootGradleContent)
        {
            if (string.IsNullOrEmpty(rootGradleContent)) return rootGradleContent;

            var withoutOurs = RemoveOwnBlock(rootGradleContent);
            var patched = Decide(rootGradleContent) == Decision.ApplyPin
                ? PinBlock + withoutOurs
                : withoutOurs;

            return patched == rootGradleContent ? rootGradleContent : patched;
        }

        private static string RemoveOwnBlock(string content) =>
            content.Contains(MarkerBegin) ? OwnBlock.Replace(content, "") : content;
    }
}
