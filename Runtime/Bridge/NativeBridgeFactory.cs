using UnityEngine;

namespace AlmediaLink.Bridge
{
    internal static class NativeBridgeFactory
    {
        internal const string GameObjectName = "AlmediaLink";

        private static INativeBridge _instance;
        private static AlmediaLinkBridge _bridge;

        internal static AlmediaLinkBridge Bridge => _bridge;

#if UNITY_EDITOR
        internal static EditorMockBridge ActiveMock => _instance as EditorMockBridge;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnDomainReload()
        {
            _instance = null;
            _bridge = null;
        }

        internal static INativeBridge Create()
        {
            if (_instance != null && _bridge != null && _bridge.gameObject != null)
                return _instance;

            var go = new GameObject(GameObjectName);
            Object.DontDestroyOnLoad(go);
            _bridge = go.AddComponent<AlmediaLinkBridge>();

#if UNITY_EDITOR
            _instance = new EditorMockBridge(_bridge);
#elif UNITY_IOS
            var systemVersion = UnityEngine.iOS.Device.systemVersion;
            if (SupportFloor.IsBelowIosFloor(systemVersion))
                _instance = CreateDisabledBridge(
                    "iOS " + systemVersion, "iOS " + SupportFloor.MinIosMajorVersion);
            else
                _instance = new iOSNativeBridge();
#elif UNITY_ANDROID
            var sdkInt = ReadAndroidSdkInt();
            if (SupportFloor.IsBelowAndroidFloor(sdkInt))
                _instance = CreateDisabledBridge(
                    "Android API " + sdkInt, "Android API " + SupportFloor.MinAndroidSdkInt);
            else
                _instance = new AndroidNativeBridge();
#else
            Object.Destroy(go);
            _bridge = null;
            throw new System.PlatformNotSupportedException(
                "AlmediaLink only supports iOS and Android player builds.");
#endif
            return _instance;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static int ReadAndroidSdkInt()
        {
            try
            {
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                return version.GetStatic<int>("SDK_INT");
            }
            catch (System.Exception e)
            {
                AlmediaLog.Warning(
                    $"Could not read Android SDK_INT ({e.GetType().Name}); assuming the OS is supported.");
                return int.MaxValue;
            }
        }
#endif

#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
        private static INativeBridge CreateDisabledBridge(string deviceOs, string requiredOs)
        {
            AlmediaLog.Warning(
                $"AlmediaLink SDK disabled: this device runs {deviceOs}, below the supported minimum of {requiredOs}. " +
                "Initialize() will report NotAvailable and every SDK call is inert; the app itself is unaffected.");
            return new NoOpNativeBridge(_bridge);
        }
#endif
    }
}
