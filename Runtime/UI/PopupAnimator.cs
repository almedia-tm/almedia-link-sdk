using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AlmediaLink.UI
{
    /// <summary>
    /// Reusable component that handles popup layout, animation, and safe area.
    /// Attach alongside a popup controller (LinkPopupController, ATTPrePromptController, etc.).
    /// Controllers call Show()/Hide() and this component handles the rest.
    /// </summary>
    [DisallowMultipleComponent]
    public class PopupAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform _popupPanel;
        [SerializeField] private RectTransform _contentPanel;
        [SerializeField] private CanvasScaler _canvasScaler;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Image _backgroundImage;
        [Tooltip("CanvasGroup on the Overlay GameObject. Uses alpha fade (no mesh rebuild) instead of Image.color.")]
        [SerializeField] private CanvasGroup _overlayCanvasGroup;

        [Header("Portrait Layout")]
        [SerializeField] private float _portraitPopupHeight = 1166f;
        [SerializeField] private Vector2 _portraitPivot = new(0.5f, 0f);
        [Tooltip("CanvasScaler match value for portrait (1.0 = pure height match).")]
        [Range(0f, 1f)]
        [SerializeField] private float _portraitMatch = 1f;

        [Header("Landscape Layout")]
        [Tooltip("Virtual width of the popup in landscape.")]
        [SerializeField] private float _landscapePopupWidth = 843f;
        [SerializeField] private Vector2 _landscapePivot = new(0.5f, 0.5f);
        [Tooltip("CanvasScaler match value for landscape.")]
        [Range(0f, 1f)]
        [SerializeField] private float _landscapeMatch = 0.8f;

        [Header("Animation")]
        [SerializeField] private float _animationDuration = 0.3f;
        [SerializeField] private bool _animateOnShow = true;

        private bool _isLandscape;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private Rect _lastSafeArea;

        private Sprite _portraitSprite;
        private float _portraitPixelsPerUnit;
        private Coroutine _activeAnimation;
        private bool _animating;
        private LayoutGroup[] _layoutGroups;
        private ContentSizeFitter[] _contentSizeFitters;

        public bool IsAnimating => _animating;

        private void Awake()
        {
            if (_canvasScaler == null)
                _canvasScaler = GetComponent<CanvasScaler>();

            if (_backgroundImage != null)
            {
                _portraitSprite = _backgroundImage.sprite;
                _portraitPixelsPerUnit = _backgroundImage.pixelsPerUnitMultiplier;
            }

            if (_popupPanel != null)
            {
                _layoutGroups = _popupPanel.GetComponentsInChildren<LayoutGroup>(true);
                _contentSizeFitters = _popupPanel.GetComponentsInChildren<ContentSizeFitter>(true);

                if (!_popupPanel.TryGetComponent<Canvas>(out _))
                    _popupPanel.gameObject.AddComponent<Canvas>();
                if (!_popupPanel.TryGetComponent<GraphicRaycaster>(out _))
                    _popupPanel.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void OnEnable()
        {
            if (!_animating)
                ForceRefresh();
        }

        private void LateUpdate()
        {
            if (_activeAnimation != null)
                return;

            bool sizeChanged = Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight;
            bool safeAreaChanged = Screen.safeArea != _lastSafeArea;

            if (!sizeChanged && !safeAreaChanged)
                return;

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastSafeArea = Screen.safeArea;

            _isLandscape = Screen.width > Screen.height;
            ApplyLayout(_isLandscape);
        }

        public void Show(Action onComplete = null)
        {
            bool animate = _animateOnShow && _animationDuration > 0f;

            _animating = animate;
            gameObject.SetActive(true);

            Canvas.ForceUpdateCanvases();
            ForceRefresh();

            if (!animate)
            {
                SetOverlayAlpha(1f);
                onComplete?.Invoke();
                return;
            }

            Vector2 from = _isLandscape
                ? new Vector2(_popupPanel.rect.width, 0f)
                : new Vector2(0f, -_popupPanel.rect.height);

            _popupPanel.anchoredPosition = from;
            SetOverlayAlpha(0f);

            StartSlideAnimation(from, Vector2.zero, fadeIn: true, onComplete);
        }

        public void Hide(Action onComplete = null)
        {
            if (_animationDuration <= 0f)
            {
                SetOverlayAlpha(0f);
                _animating = false;
                gameObject.SetActive(false);
                onComplete?.Invoke();
                return;
            }

            _animating = true;

            Vector2 to = _isLandscape
                ? new Vector2(_popupPanel.rect.width, 0f)
                : new Vector2(0f, -_popupPanel.rect.height);

            StartSlideAnimation(Vector2.zero, to, fadeIn: false, () =>
            {
                _animating = false;
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }

        public void ForceRefresh()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastSafeArea = Screen.safeArea;
            _isLandscape = Screen.width > Screen.height;
            ApplyLayout(_isLandscape);
        }

        private void ApplyLayout(bool landscape)
        {
            if (_popupPanel == null || _canvasScaler == null)
                return;

            float match = landscape ? _landscapeMatch : _portraitMatch;
            _canvasScaler.matchWidthOrHeight = match;

            Vector2 refRes = _canvasScaler.referenceResolution;
            float widthScale = Screen.width / refRes.x;
            float heightScale = Screen.height / refRes.y;
            float scaleFactor = Mathf.Pow(widthScale, 1f - match)
                              * Mathf.Pow(heightScale, match);

            if (landscape)
            {
                float virtualCanvasWidth = Screen.width / scaleFactor;
                float rightInsetPixels = Mathf.Max(0f, Screen.width - Screen.safeArea.xMax);
                float rightInsetVirtual = rightInsetPixels / scaleFactor;
                float effectivePopupWidth = _landscapePopupWidth + rightInsetVirtual;
                float anchorMinX = 1f - (effectivePopupWidth / virtualCanvasWidth);
                anchorMinX = Mathf.Clamp01(anchorMinX);

                _popupPanel.anchorMin = new Vector2(anchorMinX, 0f);
                _popupPanel.anchorMax = Vector2.one;
                _popupPanel.pivot = _landscapePivot;
                _popupPanel.sizeDelta = Vector2.zero;

                if (_titleText != null)
                    _titleText.alignment = TextAlignmentOptions.Center;

                if (_backgroundImage != null)
                {
                    _backgroundImage.sprite = null;
                    _backgroundImage.type = Image.Type.Simple;
                }
            }
            else
            {
                _popupPanel.anchorMin = Vector2.zero;
                _popupPanel.anchorMax = Vector2.right;
                _popupPanel.pivot = _portraitPivot;
                _popupPanel.sizeDelta = new Vector2(0f, _portraitPopupHeight);

                if (_titleText != null)
                    _titleText.alignment = TextAlignmentOptions.Left;

                if (_backgroundImage != null)
                {
                    _backgroundImage.sprite = _portraitSprite;
                    _backgroundImage.type = Image.Type.Sliced;
                    _backgroundImage.pixelsPerUnitMultiplier = _portraitPixelsPerUnit;
                }
            }

            _popupPanel.anchoredPosition = Vector2.zero;
            ApplySafeAreaToContent(landscape, scaleFactor);
        }

        private void ApplySafeAreaToContent(bool landscape, float scaleFactor)
        {
            if (_contentPanel == null)
                return;

            _contentPanel.anchorMin = Vector2.zero;
            _contentPanel.anchorMax = Vector2.one;

            Rect safeArea = Screen.safeArea;

            float panelLeft = _popupPanel.anchorMin.x * Screen.width;
            float panelRight = _popupPanel.anchorMax.x * Screen.width;
            float panelBottom = _popupPanel.anchorMin.y * Screen.height;
            float panelTop = landscape
                ? _popupPanel.anchorMax.y * Screen.height
                : _portraitPopupHeight * scaleFactor;

            float leftPad = Mathf.Max(0f, safeArea.xMin - panelLeft);
            float rightPad = Mathf.Max(0f, panelRight - safeArea.xMax);
            float bottomPad = Mathf.Max(0f, safeArea.yMin - panelBottom);
            float topPad = Mathf.Max(0f, panelTop - safeArea.yMax);

            _contentPanel.offsetMin = new Vector2(leftPad / scaleFactor, bottomPad / scaleFactor);
            _contentPanel.offsetMax = new Vector2(-rightPad / scaleFactor, -topPad / scaleFactor);
        }

        private void SetLayoutComponentsEnabled(bool enabled)
        {
            if (_layoutGroups != null)
                for (int i = 0; i < _layoutGroups.Length; i++)
                    _layoutGroups[i].enabled = enabled;
            if (_contentSizeFitters != null)
                for (int i = 0; i < _contentSizeFitters.Length; i++)
                    _contentSizeFitters[i].enabled = enabled;
        }

        private void StartSlideAnimation(Vector2 from, Vector2 to, bool fadeIn, Action onComplete)
        {
            if (_activeAnimation != null)
                StopCoroutine(_activeAnimation);

            _activeAnimation = StartCoroutine(SlideCoroutine(from, to, fadeIn, onComplete));
        }

        private IEnumerator SlideCoroutine(Vector2 from, Vector2 to, bool fadeIn, Action onComplete)
        {
            SetLayoutComponentsEnabled(false);

            if (fadeIn)
                yield return null;

            float startTime = Time.realtimeSinceStartup;

            while (true)
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                if (elapsed >= _animationDuration)
                    break;

                float t = elapsed / _animationDuration;
                float eased = EaseOutCubic(t);
                _popupPanel.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
                SetOverlayAlpha(fadeIn ? eased : 1f - eased);
                yield return null;
            }

            _popupPanel.anchoredPosition = to;
            SetOverlayAlpha(fadeIn ? 1f : 0f);

            SetLayoutComponentsEnabled(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_popupPanel);

            _activeAnimation = null;
            _animating = false;
            onComplete?.Invoke();
        }

        private void SetOverlayAlpha(float alpha)
        {
            if (_overlayCanvasGroup == null)
                return;

            _overlayCanvasGroup.alpha = alpha;
        }

        private static float EaseOutCubic(float t)
        {
            float t1 = t - 1f;
            return t1 * t1 * t1 + 1f;
        }
    }
}
