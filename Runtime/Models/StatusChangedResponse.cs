namespace AlmediaLink.Models
{
    [System.Serializable]
    internal class StatusChangedResponse
    {
        public string status = "";

        // Optional on the wire; the initializers are the compatibility defaults for an
        // older native plugin that omits the fields. Availability must stay fail-open.
        public string reason = "";
        public bool canShowRewardHub = true;
        public bool canShowOffer = true;
    }
}
