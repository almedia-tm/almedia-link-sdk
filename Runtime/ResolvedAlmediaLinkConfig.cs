namespace AlmediaLink
{
    /// <summary>
    /// The result of merging an AlmediaLinkConfig with AlmediaLinkSettings defaults.
    /// Contains final resolved values ready for the native bridge.
    /// </summary>
    /// <remarks>
    /// Declared as a <c>record</c> so two resolved configs compare by value -
    /// <see cref="AlmediaLinkSDK.Initialize"/> relies on that to treat a repeat
    /// call with the same effective configuration as a no-op.
    /// </remarks>
    internal record ResolvedAlmediaLinkConfig
    {
        public bool IsValid { get; set; }

        public string IntegrationKey { get; set; }
        public string Gaid { get; set; }
        public string Asid { get; set; }
        public string Oaid { get; set; }
        public string Idfa { get; set; }
        public string Idfv { get; set; }
        public string AdjustDeviceId { get; set; }
        public string AppsFlyerId { get; set; }
        public string AccountId { get; set; }
        public int NotificationsPollingIntervalSec { get; set; }
        public bool CanRunConsentFlow { get; set; }
    }
}
