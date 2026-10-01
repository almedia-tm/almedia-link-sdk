using System;

namespace AlmediaSDK
{
    [System.Serializable]
    internal class InitializeRequest
    {
        public string integrationKey;
        public string accountId;
        public string gaid;
        public string asid;
        public string oaid;
        public string idfa;
        public string idfv;
        public string adid;
        public string afid;
        public int notificationsPollingIntervalSec;
        public string trafficSource;
        public string meta1;
        public string meta2;
        public string meta3;
        public string meta4;
        public string[] disabledFeatures;

        public static InitializeRequest FromResolvedConfig(ResolvedAlmediaConfig config)
        {
            return new InitializeRequest
            {
                integrationKey = config.IntegrationKey ?? "",
                accountId = config.AccountId ?? "",
                gaid = config.Gaid ?? "",
                asid = config.Asid ?? "",
                oaid = config.Oaid ?? "",
                idfa = config.Idfa ?? "",
                idfv = config.Idfv ?? "",
                adid = config.AdjustDeviceId ?? "",
                afid = config.AppsFlyerId ?? "",
                notificationsPollingIntervalSec = config.NotificationsPollingIntervalSec,
                trafficSource = config.TrafficSource ?? "",
                meta1 = config.Meta1 ?? "",
                meta2 = config.Meta2 ?? "",
                meta3 = config.Meta3 ?? "",
                meta4 = config.Meta4 ?? "",
                disabledFeatures = string.IsNullOrEmpty(config.DisabledFeatures)
                    ? Array.Empty<string>()
                    : config.DisabledFeatures.Split(',')
            };
        }
    }
}
