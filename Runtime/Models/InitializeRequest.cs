using UnityEngine;

namespace AlmediaLink.Models
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

        public static InitializeRequest FromResolvedConfig(ResolvedAlmediaLinkConfig config)
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
                notificationsPollingIntervalSec = config.NotificationsPollingIntervalSec
            };
        }
    }
}
