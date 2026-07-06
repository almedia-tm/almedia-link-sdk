using AlmediaLink.Models;

namespace AlmediaLink
{
    internal static class AlmediaLog
    {
        internal static event System.Action<AlmediaLogLevel, string> OnLog;

        internal static void Verbose(string msg) => OnLog?.Invoke(AlmediaLogLevel.Verbose, msg);
        internal static void Debug(string msg) => OnLog?.Invoke(AlmediaLogLevel.Debug, msg);
        internal static void Info(string msg) => OnLog?.Invoke(AlmediaLogLevel.Info, msg);
        internal static void Warning(string msg) => OnLog?.Invoke(AlmediaLogLevel.Warning, msg);
        internal static void Error(string msg) => OnLog?.Invoke(AlmediaLogLevel.Error, msg);

        internal static (AlmediaLogLevel level, string message) ParseNative(NativeLogResponse log)
        {
            var message = string.IsNullOrEmpty(log.tag) ? log.message : $"[{log.tag}] {log.message}";
            var level = ParseLevel(log.level);
            return (level, message);
        }

        internal static void LogNative(NativeLogResponse log)
        {
            if (OnLog == null) return;
            var (level, message) = ParseNative(log);
            OnLog.Invoke(level, message);
        }

        private static AlmediaLogLevel ParseLevel(string level)
        {
            switch (level?.ToLowerInvariant())
            {
                case "verbose": return AlmediaLogLevel.Verbose;
                case "debug": return AlmediaLogLevel.Debug;
                case "info": return AlmediaLogLevel.Info;
                case "warning": return AlmediaLogLevel.Warning;
                case "error": return AlmediaLogLevel.Error;
                default: return AlmediaLogLevel.Debug;
            }
        }

        internal static void ClearSubscribers() => OnLog = null;
    }
}
