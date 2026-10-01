using System;

namespace AlmediaSDK
{
    [Serializable]
    internal class NotificationsReceivedResponse
    {
        public NotificationItem[] notifications = Array.Empty<NotificationItem>();
    }
}
