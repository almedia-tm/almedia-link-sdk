#if UNITY_ANDROID
using System;
using AlmediaLink.Models;
using UnityEngine;

namespace AlmediaLink.Bridge
{
    internal class AndroidNativeBridge : INativeBridge, IDisposable
    {
        private readonly AndroidJavaObject _plugin;

        public AndroidNativeBridge()
        {
            using var bridgeClass = new AndroidJavaClass("co.almedia.link.bridge.AlmediaLinkBridge");
            _plugin = bridgeClass.GetStatic<AndroidJavaObject>("INSTANCE");
        }

        private static AndroidJavaObject GetActivity()
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }

        public void Initialize(string json)
        {
            using var activity = GetActivity();
            if (activity == null)
            {
                AlmediaLog.Error("Cannot initialize: Android Activity not available.");
                return;
            }
            _plugin.Call("initialize", activity, json);
        }

        public void StartLinking(PlacementType placement)
        {
            using var activity = GetActivity();
            if (activity == null)
            {
                AlmediaLog.Error("Cannot start linking: Android Activity not available.");
                return;
            }
            _plugin.Call("startLinking", activity, placement.ToNativeString());
        }

        public void ShowRewardHub()
        {
            using var activity = GetActivity();
            if (activity == null)
            {
                AlmediaLog.Error("Cannot show reward hub: Android Activity not available.");
                return;
            }
            _plugin.Call("showRewardHub", activity);
        }

        public void ShowOffer()
        {
            using var activity = GetActivity();
            if (activity == null)
            {
                AlmediaLog.Error("Cannot show offer: Android Activity not available.");
                return;
            }
            _plugin.Call("showOffer", activity);
        }

        public void Engage()
        {
            using var activity = GetActivity();
            if (activity == null)
            {
                AlmediaLog.Error("Cannot engage: Android Activity not available.");
                return;
            }
            _plugin.Call("engage", activity);
        }

        public void FetchNotifications() => _plugin.Call("fetchNotifications");
        public void StartNotificationPolling() => _plugin.Call("startNotificationPolling");
        public void StopNotificationPolling() => _plugin.Call("stopNotificationPolling");

        public void TrackPromoLoad(PromoState state) => _plugin.Call("trackPromoLoad", state.ToNativeString());
        public void TrackPromoClick(PromoState state) => _plugin.Call("trackPromoClick", state.ToNativeString());
        public void TrackPopupShow() => _plugin.Call("trackPopupShow");
        public void TrackPopupDismiss() => _plugin.Call("trackPopupDismiss");
        public void TrackPopupCtaClick() => _plugin.Call("trackPopupCtaClick");
        public void TrackNotificationsShow(string notificationIdsJson) => _plugin.Call("trackNotificationsShow", notificationIdsJson);
        public void TrackNotificationClick(string notificationId) => _plugin.Call("trackNotificationClick", notificationId);

        public void NotifyPlayerQuitting() => _plugin.Call("notifyPlayerQuitting");

        public void Dispose()
        {
            _plugin?.Dispose();
        }
    }
}
#endif
