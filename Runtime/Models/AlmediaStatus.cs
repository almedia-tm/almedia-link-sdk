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
        {
            TryFromString(value, out var status);
            return status;
        }

        internal static bool TryFromString(string value, out AlmediaStatus status)
        {
            switch (value)
            {
                case "notInitialized": status = AlmediaStatus.NotInitialized; return true;
                case "eligible": status = AlmediaStatus.Eligible; return true;
                case "linked": status = AlmediaStatus.Linked; return true;
                case "notAvailable": status = AlmediaStatus.NotAvailable; return true;
                case "blocked": status = AlmediaStatus.Blocked; return true;
                case "disabled": status = AlmediaStatus.Disabled; return true;
                default: status = AlmediaStatus.NotInitialized; return false;
            }
        }
    }
}
