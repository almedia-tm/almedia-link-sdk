using System;
using UnityEngine;

namespace AlmediaLink.UI
{
    [CreateAssetMenu(fileName = "NotificationIconMap", menuName = "AlmediaLink/Notification Icon Map")]
    public class NotificationIconMap : ScriptableObject
    {
        private const string ResourcePath = "NotificationIconMap";

        [Serializable]
        public struct Entry
        {
            [Tooltip("Notification type string from the backend (e.g. \"reward\", \"status\")")]
            public string Type;
            public Sprite Icon;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();
        [SerializeField] private Sprite _defaultIcon;

        public Sprite GetIcon(string type)
        {
            if (!string.IsNullOrEmpty(type))
            {
                foreach (var entry in _entries)
                {
                    if (string.Equals(entry.Type, type, StringComparison.OrdinalIgnoreCase))
                        return entry.Icon;
                }
            }
            return _defaultIcon;
        }

        private static NotificationIconMap _cachedInstance;

        public static NotificationIconMap Load()
        {
            if (_cachedInstance != null)
                return _cachedInstance;

            _cachedInstance = Resources.Load<NotificationIconMap>(ResourcePath);
            return _cachedInstance;
        }
    }
}
