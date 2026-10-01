
namespace AlmediaSDK
{
    internal static class AlmediaLog
    {
        internal static event System.Action<AlmediaLogLevel, string> OnLog;

        internal static void Verbose(string msg) => Raise(AlmediaLogLevel.Verbose, msg);
        internal static void Debug(string msg) => Raise(AlmediaLogLevel.Debug, msg);
        internal static void Info(string msg) => Raise(AlmediaLogLevel.Info, msg);
        internal static void Warning(string msg) => Raise(AlmediaLogLevel.Warning, msg);
        internal static void Error(string msg) => Raise(AlmediaLogLevel.Error, msg);

        private static void Raise(AlmediaLogLevel level, string message)
            => EventDispatch.Raise(nameof(OnLog), OnLog, level, message);

        internal static (AlmediaLogLevel level, string message) ParseNative(NativeLogResponse log)
        {
            var message = string.IsNullOrEmpty(log.tag) ? log.message : $"[{log.tag}] {log.message}";
            var level = AlmediaLogLevel.FromWire(log.level);
            return (level, message);
        }

        internal static void LogNative(NativeLogResponse log)
        {
            var handlers = OnLog;
            if (handlers == null) return;
            var (level, message) = ParseNative(log);
            EventDispatch.Raise(nameof(OnLog), handlers, level, message);
        }

        internal static void ClearSubscribers() => OnLog = null;
    }
}
