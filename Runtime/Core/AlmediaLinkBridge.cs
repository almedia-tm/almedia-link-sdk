using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;
using AlmediaSDK.Bridge;

namespace AlmediaSDK
{
    [Preserve]
    internal class AlmediaLinkBridge : MonoBehaviour
    {
        internal static event Action<StatusChangedResponse> StatusChanged;
        internal static event Action<LinkCompletedResponse> LinkCompleted;
        internal static event Action<NotificationsReceivedResponse> NotificationsReceived;
        internal static event Action<InGameRewardGrantResponse> InGameRewardGrantRequested;
        internal static event Action<ProgressUpdatedResponse> ProgressUpdated;
        internal static event Action<TaskCompletedResponse> TaskCompleted;
        internal static event Action<BalanceChangedResponse> BalanceChanged;
        internal static event Action<ErrorCallbackResponse> ErrorOccurred;
        internal static event Action<AlmediaScreen> ScreenPresented;
        internal static event Action<AlmediaScreen, ScreenDismissedResponse> ScreenDismissed;
        internal static event Action<NativeLogResponse> NativeLogReceived;

        // These strings are the external native contract - the iOS .xcframework and
        // Android .aar dispatch to these names via UnitySendMessage. Hardcoded literals
        // (not nameof) so an IDE rename of a method surfaces as a validation failure
        // at startup rather than silently keeping the list in sync with a broken contract.
        private static readonly string[] NativeCallbackContract =
        {
            "OnStatusChanged",
            "OnLinkCompleted",
            "OnNotifications",
            "OnInGameRewardGrantRequested",
            "OnProgressUpdated",
            "OnTaskCompleted",
            "OnBalanceChanged",
            "OnError",
            "OnScreenPresented",
            "OnScreenDismissed",
            "OnNativeLog",
        };

        public void OnStatusChanged(string json)
        {
            if (!TryParse<StatusChangedResponse>(json, nameof(OnStatusChanged), out var response)) return;
            EventDispatch.Raise(nameof(StatusChanged), StatusChanged, response);
        }

        public void OnLinkCompleted(string json)
        {
            if (!TryParse<LinkCompletedResponse>(json, nameof(OnLinkCompleted), out var response)) return;
            EventDispatch.Raise(nameof(LinkCompleted), LinkCompleted, response);
        }

        public void OnNotifications(string json)
        {
            if (!TryParse<NotificationsReceivedResponse>(json, nameof(OnNotifications), out var response)) return;
            EventDispatch.Raise(nameof(NotificationsReceived), NotificationsReceived, response);
        }

        public void OnInGameRewardGrantRequested(string json)
        {
            if (!TryParse<InGameRewardGrantResponse>(json, nameof(OnInGameRewardGrantRequested), out var response)) return;
            EventDispatch.Raise(nameof(InGameRewardGrantRequested), InGameRewardGrantRequested, response);
        }

        public void OnProgressUpdated(string json)
        {
            if (!TryParse<ProgressUpdatedResponse>(json, nameof(OnProgressUpdated), out var response)) return;
            EventDispatch.Raise(nameof(ProgressUpdated), ProgressUpdated, response);
        }

        public void OnTaskCompleted(string json)
        {
            if (!TryParse<TaskCompletedResponse>(json, nameof(OnTaskCompleted), out var response)) return;
            EventDispatch.Raise(nameof(TaskCompleted), TaskCompleted, response);
        }

        public void OnBalanceChanged(string json)
        {
            if (!TryParse<BalanceChangedResponse>(json, nameof(OnBalanceChanged), out var response)) return;
            EventDispatch.Raise(nameof(BalanceChanged), BalanceChanged, response);
        }

        public void OnError(string json)
        {
            if (!TryParse<ErrorCallbackResponse>(json, nameof(OnError), out var response)) return;
            EventDispatch.Raise(nameof(ErrorOccurred), ErrorOccurred, response);
        }

        public void OnScreenPresented(string json)
        {
            if (!TryParse<ScreenPresentedResponse>(json, nameof(OnScreenPresented), out var response)) return;
            if (!TryParseScreen(response.screen, nameof(OnScreenPresented), out var screen)) return;
            EventDispatch.Raise(nameof(ScreenPresented), ScreenPresented, screen);
        }

        public void OnScreenDismissed(string json)
        {
            if (!TryParse<ScreenDismissedResponse>(json, nameof(OnScreenDismissed), out var response)) return;
            if (!TryParseScreen(response.screen, nameof(OnScreenDismissed), out var screen)) return;
            EventDispatch.Raise(nameof(ScreenDismissed), ScreenDismissed, screen, response);
        }

        public void OnNativeLog(string json)
        {
            if (!TryParse<NativeLogResponse>(json, nameof(OnNativeLog), out var log)) return;
            AlmediaLog.LogNative(log);
            EventDispatch.Raise(nameof(NativeLogReceived), NativeLogReceived, log);
        }

        private static bool TryParse<T>(string json, string methodName, out T response) where T : class
        {
            try
            {
                response = JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                AlmediaLog.Error($"Malformed JSON in {methodName}: {e.Message} | payload: {json}");
                response = null;
                return false;
            }
            if (response == null)
            {
                AlmediaLog.Error($"Empty payload in {methodName} | payload: {json}");
                return false;
            }
            return true;
        }

        // An unknown or missing screen string is dropped with a warning - a lifecycle event
        // must never reach subscribers with a garbage AlmediaScreen value.
        private static bool TryParseScreen(string value, string methodName, out AlmediaScreen screen)
        {
            if (AlmediaScreen.TryFromWire(value, out screen)) return true;
            AlmediaLog.Warning($"Unrecognized screen '{value}' in {methodName}; dropping the callback.");
            return false;
        }

        // Runs before native teardown reaches the job system, so this guarantees every
        // subsequent UnitySendMessage drops instead of hitting torn-down engine state.
        private void OnApplicationQuit()
        {
            NativeBridgeFactory.NotifyPlayerQuitting();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetAllEvents()
        {
            StatusChanged = null;
            LinkCompleted = null;
            NotificationsReceived = null;
            InGameRewardGrantRequested = null;
            ProgressUpdated = null;
            TaskCompleted = null;
            BalanceChanged = null;
            ErrorOccurred = null;
            ScreenPresented = null;
            ScreenDismissed = null;
            NativeLogReceived = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ValidateNativeContract()
        {
            var type = typeof(AlmediaLinkBridge);
            foreach (var name in NativeCallbackContract)
            {
                if (type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance) != null) continue;
                var msg = $"[AlmediaLink] Native callback '{name}' missing on AlmediaLinkBridge. " +
                          "The iOS/Android native plugins will fail silently for this event. " +
                          "A method was likely renamed";
                AlmediaLog.Error(msg);
            }
        }
    }
}
