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

        public string Type { get; }

        public AlmediaNotification(string id, string title, string message, string timestamp, string type)
        {
            Id = id;
            Title = title;
            Message = message;
            Timestamp = timestamp;
            Type = type;
            ReceivedAt = ParseTimestamp(timestamp);
        }

        internal static AlmediaNotification FromNotificationItem(NotificationItem item)
        {
            return new AlmediaNotification(item.id, item.title, item.message, item.timestamp, item.type);
        }

        private static DateTimeOffset? ParseTimestamp(string timestamp)
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
