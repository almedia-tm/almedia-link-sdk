using AlmediaLink.Models;

namespace AlmediaLink.Bridge
{
    internal interface INativeBridge
    {
        void Initialize(string json);
        void StartLinking(PlacementType placement = PlacementType.Popup);
        void ShowRewardHub();
        void ShowOffer();
        void Engage();
        void FetchNotifications();
        void StartNotificationPolling();
        void StopNotificationPolling();
        void TrackPromoLoad(PromoState state);
        void TrackPromoClick(PromoState state);
        void TrackPopupShow();
        void TrackPopupDismiss();
        void TrackPopupCtaClick();
        void TrackNotificationsShow(string notificationIdsJson);
        void TrackNotificationClick(string notificationId);
        void NotifyPlayerQuitting();
    }
}
