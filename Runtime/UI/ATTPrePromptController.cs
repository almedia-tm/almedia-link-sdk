using UnityEngine;

namespace AlmediaLink.UI
{
    /// <summary>
    /// Compatibility stub. The ATT consent pre-prompt was removed in 1.2.0; this type remains so
    /// that host code and Prefab Variants referencing it keep compiling and loading. It renders
    /// nothing and is never instantiated by the SDK.
    /// </summary>
    [System.Obsolete("The ATT consent pre-prompt was removed in 1.2.0.")]
    public class ATTPrePromptController : MonoBehaviour
    {
        internal bool ApplyHostSettings = true;

        /// <summary>No-op. The SDK no longer shows an ATT pre-prompt.</summary>
        public void Show()
        {
            AlmediaLog.Warning("ATTPrePromptController.Show() is a no-op: the ATT pre-prompt was removed in 1.2.0.");
        }
    }
}