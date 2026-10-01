namespace AlmediaLink.Models
{
    public enum AlmediaStatus
    {
        NotInitialized,
        Eligible,
        Linked,
        NotAvailable,
        Blocked,
        Disabled
    }

    public static class StatusExtensions
    {
        public static AlmediaStatus FromString(string value)
            => Compat.ToOldApi(AlmediaSDK.StatusExtensions.FromString(value));
    }
}
