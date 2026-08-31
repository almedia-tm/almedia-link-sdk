using System;
using System.Globalization;

namespace AlmediaLink.Models
{
    public class AlmediaNotification
    {
        public string Id { get; }
        public string Title { get; }
        public string Message { get; }

        /// <summary>
        /// Raw ISO 8601 timestamp string as delivered by the backend, e.g.
        /// <c>"2025-03-15T10:30:00Z"</c>. Preserved for debugging and re-serialization.
        /// For display logic prefer <see cref="ReceivedAt"/>.
        /// </summary>
        public string Timestamp { get; }

        /// <summary>
        /// The parsed timestamp as a UTC <see cref="DateTimeOffset"/>, or <c>null</c>
        /// when <see cref="Timestamp"/> is empty or cannot be parsed as ISO 8601.
        /// Always normalized to UTC (offset 0) regardless of the offset in the source
        /// string, so subtraction against <see cref="DateTimeOffset.UtcNow"/> works
        /// without further conversion.
        /// </summary>
        public DateTimeOffset? ReceivedAt { get; }

        /// <summary>
        /// Presentation hint: <c>"popup"</c> (banner card) or <c>"tray"</c> (quiet list
        /// entry). The set is open; treat any value you do not recognize as
        /// <c>"popup"</c>. Never null or empty: a missing hint reads as <c>"popup"</c>.
        /// </summary>
        public string Display { get; }

        /// <summary>
        /// Absolute URL of the notification's icon, or <c>null</c> when the backend
        /// supplied none. The bundled UI does not load it; it is exposed for
        /// host-rendered notification UI.
        /// </summary>
        public string IconUrl { get; }

        /// <summary>
        /// Alias of <see cref="Display"/>. Until 1.2.0 this carried a free-form backend
        /// category (e.g. <c>"reward"</c>); those values no longer exist on the wire,
        /// so comparisons against them never match.
        /// </summary>
        [Obsolete("Since 1.2.0 this carries the presentation hint (\"popup\"/\"tray\"), not the old category strings. Use Display.")]
        public string Type => Display;

        public AlmediaNotification(string id, string title, string message, string timestamp, string display, string iconUrl)
        {
            Id = id;
            Title = title;
            Message = message;
            Timestamp = timestamp;
            Display = string.IsNullOrEmpty(display) ? "popup" : display;
            IconUrl = string.IsNullOrEmpty(iconUrl) ? null : iconUrl;
            ReceivedAt = ParseTimestamp(timestamp);
        }

        [Obsolete("The type parameter now feeds Display. Use the (id, title, message, timestamp, display, iconUrl) constructor.")]
        public AlmediaNotification(string id, string title, string message, string timestamp, string type)
            : this(id, title, message, timestamp, type, null)
        {
        }

        internal static AlmediaNotification FromNotificationItem(NotificationItem item)
        {
            return new AlmediaNotification(item.id, item.title, item.message, item.timestamp, item.type, item.iconUrl);
        }

        internal static DateTimeOffset? ParseTimestamp(string timestamp)
        {
            if (string.IsNullOrEmpty(timestamp)) return null;

            if (DateTimeOffset.TryParse(
                    timestamp,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed))
            {
                return parsed;
            }

            return null;
        }
    }
}
