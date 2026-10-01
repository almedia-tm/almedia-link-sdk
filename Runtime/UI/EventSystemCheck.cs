using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace AlmediaLink.UI
{
    internal static class EventSystemCheck
    {
        private static readonly HashSet<int> WarnedThisFrame = new HashSet<int>();
        private static int _warnedFrame = -1;

        /// <summary>
        /// Logs a warning when <paramref name="ui"/> is still enabled one frame after this call and no
        /// EventSystem is active, at most once per UI per frame. Call from <c>OnEnable</c>.
        /// </summary>
        internal static void WarnIfMissing(MonoBehaviour ui, string uiName)
        {
            ui.StartCoroutine(CheckNextFrame(ui.GetInstanceID(), uiName));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnDomainReload()
        {
            WarnedThisFrame.Clear();
            _warnedFrame = -1;
        }

        private static IEnumerator CheckNextFrame(int uiId, string uiName)
        {
            yield return null;
            if (EventSystem.current != null) yield break;

            if (_warnedFrame != Time.frameCount)
            {
                WarnedThisFrame.Clear();
                _warnedFrame = Time.frameCount;
            }
            if (!WarnedThisFrame.Add(uiId)) yield break;

            AlmediaLog.Warning(
                $"{uiName}: scene '{SceneManager.GetActiveScene().name}' has no active EventSystem, " +
                "so this UI cannot receive taps. Add an EventSystem to the scene.");
        }
    }
}
