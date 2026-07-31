namespace AlmediaLink.Bridge
{
    /// <summary>
    /// The OS support floor: below these versions the SDK disables itself cleanly - the bridge
    /// factory selects <see cref="NoOpNativeBridge"/> and no C# code path ever calls into the
    /// native libraries. These constants are the single source of truth for the floor
    /// </summary>
    internal static class SupportFloor
    {
        internal const int MinIosMajorVersion = 16;
        internal const int MinAndroidSdkInt = 25;

        /// <summary>
        /// True when <paramref name="systemVersion"/> (as reported by
        /// <c>UnityEngine.iOS.Device.systemVersion</c>, e.g. "14.8.1") is below the support floor.
        /// Fails open: an unparseable version is treated as supported, with a warning - a modern
        /// device with an odd version string must not get a silently bricked SDK.
        /// </summary>
        internal static bool IsBelowIosFloor(string systemVersion)
        {
            if (!TryParseMajorVersion(systemVersion, out var major))
            {
                AlmediaLog.Warning(
                    $"Could not parse iOS version '{systemVersion}'; assuming the OS is supported.");
                return false;
            }
            return major < MinIosMajorVersion;
        }

        internal static bool IsBelowAndroidFloor(int sdkInt)
        {
            return sdkInt < MinAndroidSdkInt;
        }

        internal static bool TryParseMajorVersion(string systemVersion, out int major)
        {
            major = 0;
            if (string.IsNullOrEmpty(systemVersion)) return false;

            var version = systemVersion.Trim();
            var dot = version.IndexOf('.');
            var majorPart = dot < 0 ? version : version.Substring(0, dot);
            return int.TryParse(majorPart, out major) && major > 0;
        }
    }
}
