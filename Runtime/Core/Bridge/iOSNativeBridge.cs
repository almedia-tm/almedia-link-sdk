#if UNITY_IOS
using System.Runtime.InteropServices;

namespace AlmediaSDK.Bridge
{
    internal class iOSNativeBridge : INativeBridge
    {
        [DllImport("__Internal")]
        private static extern void Almedia_Initialize(string json);

        [DllImport("__Internal")]
        private static extern void Almedia_StartLinking(string placementType);

        [DllImport("__Internal")]
        private static extern void Almedia_ShowRewardHub();

        [DllImport("__Internal")]
        private static extern void Almedia_ShowOffer();

        [DllImport("__Internal")]
        private static extern void Almedia_Engage();

        [DllImport("__Internal")]
        private static extern void Almedia_FetchNotifications();

        [DllImport("__Internal")]
        private static extern void Almedia_StartNotificationPolling();

        [DllImport("__Internal")]
        private static extern void Almedia_StopNotificationPolling();

        [DllImport("__Internal")]
        private static extern void Almedia_TrackPromoLoad(string state);

        [DllImport("__Internal")]
        private static extern void Almedia_TrackPromoClick(string state);

        [DllImport("__Internal")]
        private static extern void Almedia_TrackPopupShow();

        [DllImport("__Internal")]
        private static extern void Almedia_TrackPopupDismiss();

        [DllImport("__Internal")]
        private static extern void Almedia_TrackPopupCtaClick();

        [DllImport("__Internal")]
        private static extern void Almedia_TrackNotificationsShow(string json);

        [DllImport("__Internal")]
        private static extern void Almedia_TrackNotificationClick(string notificationId);

        [DllImport("__Internal")]
        private static extern void Almedia_TrackInGameRewardGrantDelivered(string grantId);

        [DllImport("__Internal")]
        private static extern void Almedia_NotifyPlayerQuitting();

        public void Initialize(string json) => Almedia_Initialize(json);
        public void StartLinking(AlmediaPlacementType placement) => Almedia_StartLinking(placement.WireName);
        public void ShowRewardHub() => Almedia_ShowRewardHub();
        public void ShowOffer() => Almedia_ShowOffer();
        public void Engage() => Almedia_Engage();
        public void FetchNotifications() => Almedia_FetchNotifications();
        public void StartNotificationPolling() => Almedia_StartNotificationPolling();
        public void StopNotificationPolling() => Almedia_StopNotificationPolling();
        public void TrackPromoLoad(PromoState state) => Almedia_TrackPromoLoad(state.ToNativeString());
        public void TrackPromoClick(PromoState state) => Almedia_TrackPromoClick(state.ToNativeString());
        public void TrackPopupShow() => Almedia_TrackPopupShow();
        public void TrackPopupDismiss() => Almedia_TrackPopupDismiss();
        public void TrackPopupCtaClick() => Almedia_TrackPopupCtaClick();
        public void TrackNotificationsShow(string notificationIdsJson) => Almedia_TrackNotificationsShow(notificationIdsJson);
        public void TrackNotificationClick(string notificationId) => Almedia_TrackNotificationClick(notificationId);
        public void TrackInGameRewardGrantDelivered(string grantId) => Almedia_TrackInGameRewardGrantDelivered(grantId);
        public void NotifyPlayerQuitting() => Almedia_NotifyPlayerQuitting();
    }
}
#endif
