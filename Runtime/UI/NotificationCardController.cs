using System;
using System.Collections;
using System.Collections.Generic;
using AlmediaLink.Models;
using UnityEngine;
using UnityEngine.UI;

namespace AlmediaLink.UI
{
    [DisallowMultipleComponent]
    public class NotificationCardController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NotificationRowView _rowView;
        [SerializeField] private GameObject _stackedIndicator;
        [SerializeField] private Button _button;

        [Header("Colors")]
        [SerializeField] private Image _backgroundImage;

        [Header("Timing")]
        [SerializeField] private float _animationDuration = 0.5f;
        [SerializeField] private float _dismissDelay = 4f;

        [Header("Layout")]
        [Tooltip("Extra padding below the card for stacked indicator")]
        [SerializeField] private float _bottomPadding = 30f;

        internal static event Action OnStackedCardTapped;

        private RectTransform _rectTransform;
        private float _restingY;
        private readonly List<AlmediaNotification> _notifications = new List<AlmediaNotification>();
        private bool _isVisible;
        private Coroutine _dismissCoroutine;
        private Coroutine _activeAnimation;
        private float _safeAreaTopOffset;
        private float _offScreenY;
        private Rect _lastSafeArea;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        /// <summary>
        /// When false, this controller belongs to a host-supplied override Prefab Variant,
        /// so the SDK must not overlay settings onto it. Set by AlmediaLinkUIManager before
        /// the UI activates; true for the SDK's built-in prefabs.
        /// </summary>
        [Tooltip("Apply text and color values from AlmediaLinkSettings to this instance. Untick on a Prefab Variant you have fully authored yourself.")]
        [SerializeField] internal bool ApplyHostSettings = true;

        private void Awake()
        {
            if (!Application.isPlaying) return;

            _rectTransform = GetComponent<RectTransform>();
            _restingY = _rectTransform.anchoredPosition.y;
            if (_button != null)
                _button.onClick.AddListener(HandleCardTap);
            if (_stackedIndicator != null)
                _stackedIndicator.SetActive(false);
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            ApplySettings();
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying) return;

            if (_button != null)
                _button.onClick.RemoveListener(HandleCardTap);
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;
            if (!_isVisible || _activeAnimation != null) return;

            bool sizeChanged = Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight;
            bool safeAreaChanged = Screen.safeArea != _lastSafeArea;
            if (!sizeChanged && !safeAreaChanged) return;

            CalculateSafeAreaOffset();
            _rectTransform.anchoredPosition = new Vector2(
                _rectTransform.anchoredPosition.x,
                _restingY - _safeAreaTopOffset);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnDomainReload()
        {
            OnStackedCardTapped = null;
        }

        /// <summary>
        /// Called by AlmediaLinkUIManager when notifications arrive and enableDefaultNotificationUI is true.
        /// </summary>
        public void ShowNotifications(List<AlmediaNotification> notifications)
        {
            if (notifications == null || notifications.Count == 0) return;

            _notifications.Clear();
            _notifications.AddRange(notifications);

            var latest = notifications[notifications.Count - 1];
            UpdateCardContent(latest);
            UpdateStackedState();

            FireNotificationsShow(notifications);

            if (_isVisible)
            {
                ResetDismissTimer();
            }
            else
            {
                Show();
            }
        }

        private static void FireNotificationsShow(List<AlmediaNotification> notifications)
        {
            var ids = new string[notifications.Count];
            for (int i = 0; i < notifications.Count; i++)
                ids[i] = notifications[i].Id;
            var json = JsonUtility.ToJson(new TrackNotificationsShowRequest { notificationIds = ids });
            AlmediaLinkSDK.TrackNotificationsShow(json);
        }

        private void ApplySettings()
        {
            var settings = AlmediaLinkSettings.Load();
            if (settings == null) return;

            if (!Application.isPlaying || !ApplyHostSettings) return;

            if (_backgroundImage != null)
                _backgroundImage.color = settings.NotificationBackgroundColor;
        }

        private void UpdateCardContent(AlmediaNotification notification)
        {
            if (_rowView == null) return;
            _rowView.Populate(notification);
        }

        private void UpdateStackedState()
        {
            bool isStacked = _notifications.Count > 1;
            if (_stackedIndicator != null)
                _stackedIndicator.SetActive(isStacked);
        }

        private void HandleCardTap()
        {
            if (_notifications.Count == 0) return;
            var latest = _notifications[_notifications.Count - 1];
            AlmediaLinkSDK.TrackNotificationClick(latest.Id);
            if (_notifications.Count > 1)
            {
                OnStackedCardTapped?.Invoke();
                DismissInstantly();
            }
        }

        private void DismissInstantly()
        {
            _isVisible = false;
            StopDismissTimer();
            StopActiveAnimation();
            gameObject.SetActive(false);
        }

        #region Show / Hide

        private void Show()
        {
            _isVisible = true;
            gameObject.SetActive(true);
            CalculateSafeAreaOffset();

            Canvas.ForceUpdateCanvases();

            _offScreenY = _rectTransform.rect.height + _bottomPadding;
            _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, _offScreenY);

            StopActiveAnimation();
            _activeAnimation = StartCoroutine(SlideIn());
            ResetDismissTimer();
        }

        private void Hide()
        {
            _isVisible = false;
            StopDismissTimer();
            StopActiveAnimation();
            _activeAnimation = StartCoroutine(SlideOut());
        }

        private void StopActiveAnimation()
        {
            if (_activeAnimation != null)
            {
                StopCoroutine(_activeAnimation);
                _activeAnimation = null;
            }
        }

        #endregion

        #region Animation

        private IEnumerator SlideIn()
        {
            float x = _rectTransform.anchoredPosition.x;
            var from = new Vector2(x, _offScreenY);
            var to = new Vector2(x, _restingY - _safeAreaTopOffset);

            float startTime = Time.realtimeSinceStartup;
            float endTime = startTime + _animationDuration;

            while (Time.realtimeSinceStartup < endTime)
            {
                float t = (Time.realtimeSinceStartup - startTime) / _animationDuration;
                float eased = EaseOutCubic(t);
                _rectTransform.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
                yield return null;
            }

            _rectTransform.anchoredPosition = to;
            _activeAnimation = null;
        }

        private IEnumerator SlideOut()
        {
            var from = _rectTransform.anchoredPosition;
            var to = new Vector2(_rectTransform.anchoredPosition.x, _offScreenY);

            float startTime = Time.realtimeSinceStartup;
            float endTime = startTime + _animationDuration;

            while (Time.realtimeSinceStartup < endTime)
            {
                float t = (Time.realtimeSinceStartup - startTime) / _animationDuration;
                float eased = EaseOutCubic(t);
                _rectTransform.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
                yield return null;
            }

            _rectTransform.anchoredPosition = to;
            _activeAnimation = null;
            gameObject.SetActive(false);
        }

        private static float EaseOutCubic(float t)
        {
            float t1 = t - 1f;
            return t1 * t1 * t1 + 1f;
        }

        #endregion

        #region Dismiss Timer

        private void ResetDismissTimer()
        {
            StopDismissTimer();
            _dismissCoroutine = StartCoroutine(DismissAfterDelay());
        }

        private void StopDismissTimer()
        {
            if (_dismissCoroutine != null)
            {
                StopCoroutine(_dismissCoroutine);
                _dismissCoroutine = null;
            }
        }

        private IEnumerator DismissAfterDelay()
        {
            yield return new WaitForSecondsRealtime(_dismissDelay);
            _dismissCoroutine = null;
            Hide();
        }

        #endregion

        #region Safe Area

        private void CalculateSafeAreaOffset()
        {
            var safeArea = Screen.safeArea;
            float topInsetPixels = Mathf.Max(0f, Screen.height - (safeArea.y + safeArea.height));
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                _safeAreaTopOffset = topInsetPixels / canvas.scaleFactor;
            else
                _safeAreaTopOffset = topInsetPixels;

            _lastSafeArea = safeArea;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }

        #endregion
    }
}
