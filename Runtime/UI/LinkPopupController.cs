using AlmediaLink.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace AlmediaLink.UI
{
    [DisallowMultipleComponent]
    public class LinkPopupController : MonoBehaviour
    {
        [Header("Animator")]
        [SerializeField] private PopupAnimator _animator;

        [Header("Buttons")]
        [SerializeField] private Button _ctaButton;
        [SerializeField] private Button _closeButton;

        [Header("Text Fields")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _ctaButtonLabel;

        [Header("Benefit Box")]
        [SerializeField] private TMP_Text _benefit1TitleText;
        [FormerlySerializedAs("_bodyText")]
        [SerializeField] private TMP_Text _benefit1DescriptionText;
        [SerializeField] private TMP_Text _benefit2TitleText;
        [FormerlySerializedAs("_bonusText")]
        [SerializeField] private TMP_Text _benefit2DescriptionText;

        [Header("Colors")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _ctaButtonImage;

        /// <summary>
        /// When false, this controller belongs to a host-supplied override Prefab Variant,
        /// so the SDK must not overlay settings onto it. Set by AlmediaLinkUIManager before
        /// the UI activates; true for the SDK's built-in prefabs.
        /// </summary>
        internal bool ApplyHostSettings = true;

        private bool _isClosing;

        private void Awake()
        {
            if (!Application.isPlaying) return;

            if (_ctaButton != null)
                _ctaButton.onClick.AddListener(HandleCTATap);
            if (_closeButton != null)
                _closeButton.onClick.AddListener(HandleCloseTap);

            AlmediaLinkSDK.OnLinkCompleted += HandleLinkCompleted;
            AlmediaLinkSDK.OnErrorOccurred += HandleErrorOccurred;

            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            ApplySettings();
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying) return;

            if (_ctaButton != null)
                _ctaButton.onClick.RemoveListener(HandleCTATap);
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(HandleCloseTap);

            AlmediaLinkSDK.OnLinkCompleted -= HandleLinkCompleted;
            AlmediaLinkSDK.OnErrorOccurred -= HandleErrorOccurred;
        }

        public void Show()
        {
            ApplySettings();
            AlmediaLinkSDK.TrackPopupShow();
            _animator.Show();
        }

        private void HandleCTATap()
        {
            AlmediaLinkSDK.TrackPopupCtaClick();
            AlmediaLinkSDK.StartLinking();
        }

        private void HandleCloseTap()
        {
            AlmediaLinkSDK.TrackPopupDismiss();
            Close();
        }

        private void HandleLinkCompleted(string linkedAt)
        {
            Close();
        }

        private void HandleErrorOccurred(AlmediaError error)
        {
            if (error == null || error.Code != AlmediaErrorCode.LinkingFailed) return;
            Close();
        }

        private void Close()
        {
            if (_isClosing) return;
            _isClosing = true;
            _animator.Hide(DestroySelf);
        }

        private void DestroySelf()
        {
            Destroy(gameObject);
        }

        private void ApplySettings()
        {
            var settings = AlmediaLinkSettings.Load();
            if (settings == null) return;

            if (!Application.isPlaying || !ApplyHostSettings) return;

            // Text
            if (_titleText != null)
                _titleText.text = settings.PopupTitle;
            if (_ctaButtonLabel != null)
                _ctaButtonLabel.text = settings.CtaButtonText;

            if (_benefit1TitleText != null)
                _benefit1TitleText.text = settings.Benefit1Title;
            if (_benefit1DescriptionText != null)
                _benefit1DescriptionText.text = settings.Benefit1Description;
            if (_benefit2TitleText != null)
                _benefit2TitleText.text = settings.Benefit2Title;
            if (_benefit2DescriptionText != null)
                _benefit2DescriptionText.text = settings.Benefit2Description;

            // Colors
            if (_ctaButtonLabel != null)
                _ctaButtonLabel.color = settings.CtaButtonTextColor;
            if (_backgroundImage != null)
                _backgroundImage.color = settings.PopupBackgroundColor;
            if (_ctaButtonImage != null)
                _ctaButtonImage.color = settings.CtaButtonColor;
        }
    }
}
