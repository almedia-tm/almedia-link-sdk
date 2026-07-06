using System;

namespace AlmediaLink.Models
{
    [Serializable]
    internal class NotificationsReceivedResponse
    {
        public NotificationItem[] notifications = Array.Empty<NotificationItem>();
    }
}
