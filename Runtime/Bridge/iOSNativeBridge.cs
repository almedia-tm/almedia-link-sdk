#if UNITY_IOS
using System.Runtime.InteropServices;
using AlmediaLink.Models;

namespace AlmediaLink.Bridge
{
    internal class iOSNativeBridge : INativeBridge
    {
        [DllImport("__Internal")]
        private static extern void AlmediaLink_Initialize(string json);

        [DllImport("__Internal")]
        private static extern void AlmediaLink_StartLinking(string placementType);

        [DllImport("__Internal")]
        private static extern void AlmediaLink_ShowRewardHub();

        [DllImport("__Internal")]
        private static extern void AlmediaLink_ShowOffer();

        [DllImport("__Internal")]
        private static extern void AlmediaLink_Engage();

        [DllImport("__Internal")]
        private static extern void AlmediaLink_FetchNotifications();

        [DllImport("__Internal")]
        private static extern void AlmediaLink_StartNotificationPolling();

        [DllImport("__Internal")]
        private static extern void AlmediaLink_StopNotificationPolling();

        [DllImport("__Internal")]
        private static extern void AlmediaLink_ContinueWithATT();

        [DllImport("__Internal")]
        private static extern void AlmediaLink_SkipATT();

        [DllImport("__Internal")]
        private static extern void AlmediaLink_TrackPromoLoad(string state);

        [DllImport("__Internal")]
        private static extern void AlmediaLink_TrackPromoClick(string state);

        [DllImport("__Internal")]
        private static extern void AlmediaLink_TrackPopupShow();

        [DllImport("__Internal")]
        private static extern void AlmediaLink_TrackPopupDismiss();

        [DllImport("__Internal")]
        private static extern void AlmediaLink_TrackPopupCtaClick();

        [DllImport("__Internal")]
        private static extern void AlmediaLink_TrackNotificationsShow(string json);

        [DllImport("__Internal")]
        private static extern void AlmediaLink_TrackNotificationClick(string notificationId);
        
        [DllImport("__Internal")]
        private static extern void AlmediaLink_TrackATTPreliminaryShow();

        [DllImport("__Internal")]
        private static extern void AlmediaLink_NotifyPlayerQuitting();

        public void Initialize(string json) => AlmediaLink_Initialize(json);
        public void StartLinking(PlacementType placement) => AlmediaLink_StartLinking(placement.ToNativeString());
        public void ShowRewardHub() => AlmediaLink_ShowRewardHub();
        public void ShowOffer() => AlmediaLink_ShowOffer();
        public void Engage() => AlmediaLink_Engage();
        public void FetchNotifications() => AlmediaLink_FetchNotifications();
        public void StartNotificationPolling() => AlmediaLink_StartNotificationPolling();
        public void StopNotificationPolling() => AlmediaLink_StopNotificationPolling();
        public void ContinueWithATT() => AlmediaLink_ContinueWithATT();
        public void SkipATT() => AlmediaLink_SkipATT();
        public void TrackPromoLoad(PromoState state) => AlmediaLink_TrackPromoLoad(state.ToNativeString());
        public void TrackPromoClick(PromoState state) => AlmediaLink_TrackPromoClick(state.ToNativeString());
        public void TrackPopupShow() => AlmediaLink_TrackPopupShow();
        public void TrackPopupDismiss() => AlmediaLink_TrackPopupDismiss();
        public void TrackPopupCtaClick() => AlmediaLink_TrackPopupCtaClick();
        public void TrackNotificationsShow(string notificationIdsJson) => AlmediaLink_TrackNotificationsShow(notificationIdsJson);
        public void TrackNotificationClick(string notificationId) => AlmediaLink_TrackNotificationClick(notificationId);
        public void TrackATTPreliminaryShow() => AlmediaLink_TrackATTPreliminaryShow();
        public void NotifyPlayerQuitting() => AlmediaLink_NotifyPlayerQuitting();
    }
}
#endif
