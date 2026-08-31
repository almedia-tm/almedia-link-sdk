using System.Collections.Generic;
using AlmediaLink.Models;
using UnityEngine;
using UnityEngine.UI;

namespace AlmediaLink.UI
{
    internal static class AlmediaLinkUIManager
    {
        private const int MaxDisplayedNotifications = 3;

        private static NotificationCardController _notificationCard;
        private static ActivityOverlayController _activityOverlay;
        private static List<AlmediaNotification> _lastNotifications;
        private static bool _initialized;
        private static readonly HashSet<string> _missingPrefabsLogged = new HashSet<string>();

        internal static void Initialize()
        {
            if (_initialized) return;
            AlmediaLinkSDK.OnNotificationsReceived += HandleNotifications;
            NotificationCardController.OnStackedCardTapped += HandleStackedCardTapped;
            _initialized = true;
        }
        
        internal static void ShowLinkPopup(LinkPopupController prefab)
        {
            if (prefab == null) return; // the caller owns the fallback and the warning

            var instance = Object.Instantiate(prefab);
            Object.DontDestroyOnLoad(instance.gameObject);
            instance.Show();
        }

        internal static void Cleanup()
        {
            AlmediaLinkSDK.OnNotificationsReceived -= HandleNotifications;
            NotificationCardController.OnStackedCardTapped -= HandleStackedCardTapped;
            _initialized = false;
            _lastNotifications = null;
            _missingPrefabsLogged.Clear();

            if (_notificationCard != null)
            {
                var canvas = _notificationCard.GetComponentInParent<Canvas>();
                if (canvas != null)
                    Object.Destroy(canvas.gameObject);
            }
            _notificationCard = null;
            _activityOverlay = null; // destroyed with canvas (child)
        }

        private static void HandleNotifications(List<AlmediaNotification> notifications)
        {
            if (!ShouldShowDefaultNotificationUI()) return;
            _lastNotifications = CapToLatest(notifications);
            EnsureNotificationCardExists();
            if (_notificationCard != null)
                _notificationCard.ShowNotifications(_lastNotifications);
        }

        private static List<AlmediaNotification> CapToLatest(List<AlmediaNotification> notifications)
        {
            if (notifications == null || notifications.Count <= MaxDisplayedNotifications)
                return notifications;
            return notifications.GetRange(notifications.Count - MaxDisplayedNotifications, MaxDisplayedNotifications);
        }

        private static void HandleStackedCardTapped()
        {
            if (_lastNotifications == null || _lastNotifications.Count == 0) return;
            EnsureActivityOverlayExists();
            if (_activityOverlay != null)
                _activityOverlay.Show(_lastNotifications);
        }

        private static bool ShouldShowDefaultNotificationUI()
        {
            var settings = AlmediaLinkSettings.Load();
            return settings != null && settings.EnableDefaultNotificationUI;
        }

        private static void EnsureNotificationCardExists()
        {
            if (_notificationCard != null) return;

            var prefab = RequirePrefab(AlmediaLinkSettings.Load()?.NotificationCardPrefab, "Notification Card");
            if (prefab == null) return;

            var canvasGo = new GameObject("AlmediaLinkNotificationCanvas");
            Object.DontDestroyOnLoad(canvasGo);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(780, 1688);
            scaler.matchWidthOrHeight = 0.8f;

            canvasGo.AddComponent<GraphicRaycaster>();

            _notificationCard = Object.Instantiate(prefab, canvasGo.transform);
        }

        private static void EnsureActivityOverlayExists()
        {
            if (_activityOverlay != null) return;

            var prefab = RequirePrefab(AlmediaLinkSettings.Load()?.ActivityOverlayPrefab, "Activity Overlay");
            if (prefab == null) return;

            // Reuse the existing notification canvas
            if (_notificationCard == null)
            {
                AlmediaLog.Warning("Cannot create ActivityOverlay: NotificationCard does not exist.");
                return;
            }
            var canvas = _notificationCard.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                AlmediaLog.Warning("Cannot create ActivityOverlay: Notification Canvas not found.");
                return;
            }

            _activityOverlay = Object.Instantiate(prefab, canvas.transform);
        }

        // Warn-once resolver. The editor keeps these references in lockstep with the
        // EnableDefaultNotificationUI toggle, so a null here means a hand-edited settings asset.
        private static T RequirePrefab<T>(T prefab, string featureName) where T : Object
        {
            if (prefab != null) return prefab;
            if (_missingPrefabsLogged.Add(featureName))
            {
                AlmediaLog.Warning(
                    $"{featureName} prefab is not assigned in AlmediaLinkSettings while the default " +
                    "notification UI is enabled. Assign it in Almedia > Settings (re-ticking " +
                    "'Enable Default Notification UI' restores the bundled prefabs), or disable the default UI.");
            }
            return null;
        }
    }
}
