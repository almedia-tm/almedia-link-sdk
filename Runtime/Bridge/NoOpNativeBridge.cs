using System.Collections;
using AlmediaLink.Models;
using UnityEngine;

namespace AlmediaLink.Bridge
{
    /// <summary>
    /// The bridge selected on devices below the OS support floor (see <see cref="SupportFloor"/>).
    /// The SDK is inert: nothing here touches native code
    /// </summary>
    internal class NoOpNativeBridge : INativeBridge
    {
        private readonly MonoBehaviour _host;

        public NoOpNativeBridge(MonoBehaviour host)
        {
            _host = host;
        }

        public void Initialize(string json)
        {
            AlmediaLog.Debug("No-op bridge: Initialize - reporting NotAvailable.");
            _host.StartCoroutine(EmitNotAvailable());
        }

        // Deferred one frame to mirror the asynchronous native path
        private IEnumerator EmitNotAvailable()
        {
            yield return null;
            var json = JsonUtility.ToJson(new StatusChangedResponse { status = "notAvailable" });
            _host.gameObject.SendMessage("OnStatusChanged", json);
        }

        public void StartLinking(PlacementType placement) { }
        public void ShowRewardHub() { }
        public void ShowOffer() { }
        public void Engage() { }
        public void FetchNotifications() { }
        public void StartNotificationPolling() { }
        public void StopNotificationPolling() { }
        public void ContinueWithATT() { }
        public void SkipATT() { }
        public void TrackPromoLoad(PromoState state) { }
        public void TrackPromoClick(PromoState state) { }
        public void TrackPopupShow() { }
        public void TrackPopupDismiss() { }
        public void TrackPopupCtaClick() { }
        public void TrackNotificationsShow(string notificationIdsJson) { }
        public void TrackNotificationClick(string notificationId) { }
        public void TrackATTPreliminaryShow() { }
    }
}
