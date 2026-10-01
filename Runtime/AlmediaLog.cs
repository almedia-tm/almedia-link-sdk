namespace AlmediaLink
{
    internal static class AlmediaLog
    {
        internal static void Verbose(string msg) => AlmediaSDK.AlmediaLog.Verbose(msg);
        internal static void Debug(string msg) => AlmediaSDK.AlmediaLog.Debug(msg);
        internal static void Info(string msg) => AlmediaSDK.AlmediaLog.Info(msg);
        internal static void Warning(string msg) => AlmediaSDK.AlmediaLog.Warning(msg);
        internal static void Error(string msg) => AlmediaSDK.AlmediaLog.Error(msg);
    }
}
