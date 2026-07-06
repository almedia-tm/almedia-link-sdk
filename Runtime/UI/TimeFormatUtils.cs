namespace AlmediaLink.UI
{
    internal static class TimeFormatUtils
    {
        /// <summary>
        /// Returns a relative label like "5min ago" for a parsed UTC instant
        /// (typically <c>AlmediaNotification.ReceivedAt</c>). Returns an empty string
        /// when <paramref name="receivedAt"/> is null
        /// </summary>
        internal static string FormatRelativeTime(System.DateTimeOffset? receivedAt)
        {
            if (!receivedAt.HasValue) return "";

            var elapsed = System.DateTimeOffset.UtcNow - receivedAt.Value;

            if (elapsed.TotalSeconds < 60) return "now";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}min ago";
            if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}h ago";
            if (elapsed.TotalDays < 30) return $"{(int)elapsed.TotalDays}d ago";
            if (elapsed.TotalDays < 365) return $"{(int)(elapsed.TotalDays / 30)}mo ago";
            return $"{(int)(elapsed.TotalDays / 365)}y ago";
        }
    }
}
