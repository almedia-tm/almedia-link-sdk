using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AlmediaLink.UI
{
    [DisallowMultipleComponent]
    public class ATTPrePromptController : MonoBehaviour
    {
        [Header("Animator")]
        [SerializeField] private PopupAnimator _animator;

        [Header("Buttons")]
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _closeButton;

        [Header("Text Fields")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _rewardAmountText;
        [SerializeField] private TMP_Text _whyTitleText;
        [SerializeField] private TMP_Text _whyBodyText;
        [SerializeField] private TMP_Text _controlTitleText;
        [SerializeField] private TMP_Text _controlBodyText;
        [SerializeField] private TMP_Text _continueButtonLabel;

        [Header("Colors")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _continueButtonImage;

        /// <summary>
        /// When false, this controller belongs to a host-supplied override Prefab Variant,
        /// so the SDK must not overlay settings onto it. Set by AlmediaLinkUIManager before
        /// the UI activates; true for the SDK's built-in prefabs.
        /// </summary>
        internal bool ApplyHostSettings = true;

        private void Awake()
        {
            if (!Application.isPlaying) return;

            if (_continueButton != null)
                _continueButton.onClick.AddListener(HandleContinue);
            if (_closeButton != null)
                _closeButton.onClick.AddListener(HandleClose);

            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            ApplySettings();
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying) return;

            if (_continueButton != null)
                _continueButton.onClick.RemoveListener(HandleContinue);
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(HandleClose);
        }

        public void Show()
        {
            ApplySettings();
            AlmediaLinkSDK.TrackATTPreliminaryShow();
            _animator.Show();
        }

        private void HandleContinue()
        {
            AlmediaLog.Info("ATT pre-prompt: user tapped Continue.");
            _animator.Hide(() =>
            {
                AlmediaLinkSDK.ContinueWithATT();
                Destroy(gameObject);
            });
        }

        private void HandleClose()
        {
            AlmediaLog.Info("ATT pre-prompt: user tapped Close. Skipping ATT.");
            _animator.Hide(() =>
            {
                AlmediaLinkSDK.SkipATT();
                Destroy(gameObject);
            });
        }

        private void ApplySettings()
        {
            var settings = AlmediaLinkSettings.Load();
            if (settings == null) return;

            if (!Application.isPlaying || !ApplyHostSettings) return;

            // Text
            if (_titleText != null)
                _titleText.text = settings.AttPromptTitle;
            if (_rewardAmountText != null)
                _rewardAmountText.text = settings.AttRewardAmount;
            if (_whyTitleText != null)
                _whyTitleText.text = settings.AttWhyTitle;
            if (_whyBodyText != null)
                _whyBodyText.text = settings.AttWhyBody;
            if (_controlTitleText != null)
                _controlTitleText.text = settings.AttControlTitle;
            if (_controlBodyText != null)
                _controlBodyText.text = settings.AttControlBody;
            if (_continueButtonLabel != null)
                _continueButtonLabel.text = settings.AttContinueButtonText;

            // Colors
            if (_continueButtonLabel != null)
                _continueButtonLabel.color = settings.AttButtonTextColor;
            if (_backgroundImage != null)
                _backgroundImage.color = settings.AttBackgroundColor;
            if (_continueButtonImage != null)
                _continueButtonImage.color = settings.AttPrimaryButtonColor;
        }
    }
}
