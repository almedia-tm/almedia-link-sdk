using AlmediaLink.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AlmediaLink.UI
{
    [DisallowMultipleComponent]
    public class NotificationRowView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private TMP_Text _timestampText;
        [SerializeField] private Button _button;

        private string _notificationId;

        private void Awake()
        {
            if (!Application.isPlaying) return;
            if (_button != null)
                _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying) return;
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
        }

        public void Populate(AlmediaNotification notification, Sprite icon)
        {
            _notificationId = notification.Id;
            if (_iconImage != null)
                _iconImage.sprite = icon;
            if (_titleText != null)
                _titleText.text = notification.Title ?? "";
            if (_messageText != null)
                _messageText.text = notification.Message ?? "";
            if (_timestampText != null)
                _timestampText.text = TimeFormatUtils.FormatRelativeTime(notification.ReceivedAt);
        }

        private void HandleClick()
        {
            if (string.IsNullOrEmpty(_notificationId)) return;
            AlmediaLinkSDK.TrackNotificationClick(_notificationId);
        }
    }
}
