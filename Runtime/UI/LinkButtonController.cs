using AlmediaLink.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AlmediaLink.UI
{
    /// <summary>
    /// Drives the Link button's two states. Each state is a self-contained subtree carrying its own
    /// background, button and labels, so the controller only decides which one is showing - the art,
    /// layout and press colors of each state are authored entirely in the prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public class LinkButtonController : MonoBehaviour
    {
        /// <summary>
        /// One of the button's visual states. Every member tolerates unassigned references so a
        /// partially-wired button degrades instead of throwing - see <see cref="ApplyVisual"/>.
        /// </summary>
        [System.Serializable]
        private class VisualState
        {
            [Tooltip("The subtree shown while this state is active.")]
            public GameObject Content;

            [Tooltip("The button inside this state's subtree.")]
            public Button Button;

            public bool Exists => Content != null;

            public void SetActive(bool visible)
            {
                if (Content != null) Content.SetActive(visible);
            }

            public void Subscribe(UnityAction onClick)
            {
                if (Button != null) Button.onClick.AddListener(onClick);
            }

            public void Unsubscribe(UnityAction onClick)
            {
                if (Button != null) Button.onClick.RemoveListener(onClick);
            }
        }

        [Header("Eligible state")]
        [Tooltip("Shown while the player can still link.")]
        [SerializeField] private VisualState _eligible = new VisualState();

        [Header("Linked state")]
        [Tooltip("Shown once the player has linked.")]
        [SerializeField] private VisualState _linked = new VisualState();

        [Header("Link popup")]
        [Tooltip("The popup an eligible player's tap opens. Assign a Prefab Variant to customize it; leave empty to start linking directly on tap.")]
        [SerializeField] private LinkPopupController _linkPopup;

        private PromoState? _lastPromoState;
        private bool _missingLinkedVisualWarned;
        private bool _missingPopupWarned;

        private void Awake()
        {
            AlmediaLinkSDK.InitializeIfNeeded();

            _eligible.Subscribe(OnButtonClicked);
            _linked.Subscribe(OnButtonClicked);
            if (_eligible.Button == null && _linked.Button == null)
                AlmediaLog.Error("LinkButton: no state button assigned on the prefab. Button will be inert.");

            AlmediaLinkSDK.OnStatusChanged += HandleStatusChanged;
            AlmediaLinkSDK.OnScreenAvailabilityChanged += HandleAvailabilityChanged;
            gameObject.SetActive(false);
            if (AlmediaLinkSDK.CurrentStatus != AlmediaStatus.NotInitialized)
                Refresh();
        }

        private void OnDestroy()
        {
            _eligible.Unsubscribe(OnButtonClicked);
            _linked.Unsubscribe(OnButtonClicked);
            AlmediaLinkSDK.OnStatusChanged -= HandleStatusChanged;
            AlmediaLinkSDK.OnScreenAvailabilityChanged -= HandleAvailabilityChanged;
        }

        private void HandleStatusChanged(AlmediaStatus status) => Refresh();

        private void HandleAvailabilityChanged(AlmediaScreenAvailability availability) => Refresh();

        private void Refresh()
        {
            PromoState promo = ShownStateFor(AlmediaLinkSDK.CurrentStatus, AlmediaLinkSDK.ScreenAvailability);

            // Applied before activating so a transition never shows the wrong state for a frame.
            if (promo != PromoState.Hidden)
                ApplyVisual(promo);

            SetVisible(promo != PromoState.Hidden);
            FirePromoLoad(promo);
        }

        private static PromoState ShownStateFor(AlmediaStatus status, AlmediaScreenAvailability availability)
        {
            switch (status)
            {
                case AlmediaStatus.Eligible: return PromoState.Eligible;
                case AlmediaStatus.Linked:
                    return availability.CanShowRewardHub ? PromoState.Linked : PromoState.Hidden;
                default: return PromoState.Hidden;
            }
        }

        /// <remarks>
        /// Both subtrees are optional. A button assembled by hand may only have the eligible one, and
        /// must still work: it keeps that subtree showing to a linked player rather than swapping to
        /// nothing and leaving an empty, invisible-but-clickable rect. Visibility never depends on
        /// either reference being assigned.
        /// </remarks>
        private void ApplyVisual(PromoState promo)
        {
            bool linked = promo == PromoState.Linked;

            _eligible.SetActive(!linked || !_linked.Exists);
            _linked.SetActive(linked);

            if (linked && !_linked.Exists && !_missingLinkedVisualWarned)
            {
                _missingLinkedVisualWarned = true;
                AlmediaLog.Warning(
                    "LinkButton: no linked-state subtree assigned; showing the eligible visual to a linked " +
                    "player. Re-create your Prefab Variant from the updated LinkButton prefab.");
            }
        }

        private void FirePromoLoad(PromoState state)
        {
            if (state == _lastPromoState) return;
            _lastPromoState = state;
            AlmediaLinkSDK.TrackPromoLoad(state);
        }

        private void OnButtonClicked()
        {
            // Reported as the state the player actually saw and tapped - the same value promo_load
            // sent for this button - so click and load stay comparable.
            AlmediaLinkSDK.TrackPromoClick(_lastPromoState
                ?? ShownStateFor(AlmediaLinkSDK.CurrentStatus, AlmediaLinkSDK.ScreenAvailability));

            // Routed on the live status rather than the visible subtree: if the two ever disagree, the
            // action the SDK can actually honor is the right one. Deliberately not Engage() - Engage
            // links directly, while the bundled button keeps the popup on the eligible path.
            // The legacy 1.x settings override must keep winning over the button's own reference.
            var settings = AlmediaLinkSettings.Load();
            var popup = settings != null && settings.LegacyLinkPopupOverride != null
                ? settings.LegacyLinkPopupOverride
                : _linkPopup;

            if (AlmediaLinkSDK.CurrentStatus == AlmediaStatus.Linked)
            {
                AlmediaLinkSDK.ShowRewardHub();
            }
            else if (popup != null)
            {
                AlmediaLinkUIManager.ShowLinkPopup(popup);
            }
            else
            {
                // No popup assigned: keep the button functional by linking directly
                if (!_missingPopupWarned)
                {
                    _missingPopupWarned = true;
                    AlmediaLog.Warning(
                        "LinkButton: no Link Popup assigned; the tap starts linking directly. " +
                        "Assign the popup on the LinkButton prefab to restore the intro popup.");
                }
                AlmediaLinkSDK.StartLinking(PlacementType.Popup);
            }
        }

        private void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
