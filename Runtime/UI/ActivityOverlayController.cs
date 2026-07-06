using System.Collections.Generic;
using AlmediaLink.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AlmediaLink.UI
{
    [DisallowMultipleComponent]
    public class ActivityOverlayController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private NotificationRowView _rowPrefab;
        [SerializeField] private Transform _rowContainer;
        [SerializeField] private RectTransform _contentPanel;

        private float _contentBaselineY;
        private Rect _lastSafeArea;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        /// <summary>
        /// When false, this controller belongs to a host-supplied override Prefab Variant,
        /// so the SDK must not overlay settings onto it. Set by AlmediaLinkUIManager before
        /// the UI activates; true for the SDK's built-in prefabs.
        /// </summary>
        internal bool ApplyHostSettings = true;

        private void Awake()
        {
            if (!Application.isPlaying) return;

            if (_contentPanel != null)
                _contentBaselineY = _contentPanel.anchoredPosition.y;

            if (_closeButton != null)
                _closeButton.onClick.AddListener(Hide);
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            ApplySettings();
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying) return;

            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(Hide);
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;

            bool sizeChanged = Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight;
            bool safeAreaChanged = Screen.safeArea != _lastSafeArea;
            if (!sizeChanged && !safeAreaChanged) return;

            ApplySafeArea();
        }

        public void Show(List<AlmediaNotification> notifications)
        {
            if (notifications == null || notifications.Count == 0) return;

            ApplySettings();
            ClearRows();
            PopulateRows(notifications);
            ApplySafeArea();

            gameObject.SetActive(true);
        }

        private void ApplySafeArea()
        {
            if (_contentPanel == null) return;
            var safeArea = Screen.safeArea;
            float topInsetPixels = Mathf.Max(0f, Screen.height - (safeArea.y + safeArea.height));
            var canvas = GetComponentInParent<Canvas>();
            float topInset = canvas != null ? topInsetPixels / canvas.scaleFactor : topInsetPixels;
            var pos = _contentPanel.anchoredPosition;
            _contentPanel.anchoredPosition = new Vector2(pos.x, _contentBaselineY - topInset);

            _lastSafeArea = safeArea;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ApplySettings()
        {
            if (_titleText == null) return;
            
            if (!Application.isPlaying || !ApplyHostSettings) return;

            var settings = AlmediaLinkSettings.Load();
            _titleText.text = settings != null ? settings.OverlayTitle : "Notifications";
        }

        private void ClearRows()
        {
            if (_rowContainer == null) return;
            for (int i = _rowContainer.childCount - 1; i >= 0; i--)
            {
                var child = _rowContainer.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }

        private void PopulateRows(List<AlmediaNotification> notifications)
        {
            if (_rowPrefab == null || _rowContainer == null) return;
            var iconMap = NotificationIconMap.Load();
            foreach (var notification in notifications)
            {
                var row = Instantiate(_rowPrefab, _rowContainer);
                row.gameObject.SetActive(true);
                var icon = iconMap != null ? iconMap.GetIcon(notification.Type) : null;
                row.Populate(notification, icon);
            }
        }
    }
}
