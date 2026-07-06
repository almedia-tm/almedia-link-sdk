using AlmediaLink.Models;

namespace AlmediaLink.Bridge
{
    internal interface INativeBridge
    {
        void Initialize(string json);
        void StartLinking(PlacementType placement = PlacementType.Popup);
        void FetchNotifications();
        void StartNotificationPolling();
        void StopNotificationPolling();
        void ContinueWithATT();
        void SkipATT();
        void TrackPromoLoad(PromoState state);
        void TrackPromoClick();
        void TrackPopupShow();
        void TrackPopupDismiss();
        void TrackPopupCtaClick();
        void TrackNotificationsShow(string notificationIdsJson);
        void TrackNotificationClick(string notificationId);
        void TrackATTPreliminaryShow();
    }
}
