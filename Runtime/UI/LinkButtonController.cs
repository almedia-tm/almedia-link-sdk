using AlmediaLink.Models;
using UnityEngine;
using UnityEngine.UI;

namespace AlmediaLink.UI
{
    [DisallowMultipleComponent]
    public class LinkButtonController : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private PromoState? _lastPromoState;

        private void Awake()
        {
            if (_button != null)
                _button.onClick.AddListener(OnButtonClicked);
            else
                AlmediaLog.Error("LinkButton: _button reference missing on prefab. Button will be inert.");
            AlmediaLinkSDK.OnStatusChanged += HandleStatusChanged;
            gameObject.SetActive(false);
            if (AlmediaLinkSDK.CurrentStatus != AlmediaStatus.NotInitialized)
                HandleStatusChanged(AlmediaLinkSDK.CurrentStatus);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnButtonClicked);
            AlmediaLinkSDK.OnStatusChanged -= HandleStatusChanged;
        }

        private void HandleStatusChanged(AlmediaStatus status)
        {
            bool show = status == AlmediaStatus.Eligible;
            SetVisible(show);
            FirePromoLoad(show, status == AlmediaStatus.Linked);
        }

        private void FirePromoLoad(bool shown, bool isLinked)
        {
            PromoState state = shown ? PromoState.Eligible : (isLinked ? PromoState.Linked : PromoState.Hidden);
            if (state == _lastPromoState) return;
            _lastPromoState = state;
            AlmediaLinkSDK.TrackPromoLoad(state);
        }

        private void OnButtonClicked()
        {
            AlmediaLinkSDK.TrackPromoClick();
            AlmediaLinkUIManager.ShowLinkPopup();
        }

        private void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
