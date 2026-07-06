namespace AlmediaLink.Models
{
    internal enum PromoState
    {
        Eligible,
        Linked,
        Hidden
    }

    internal static class PromoStateExtensions
    {
        internal static string ToNativeString(this PromoState state)
        {
            switch (state)
            {
                case PromoState.Eligible: return "eligible";
                case PromoState.Linked: return "linked";
                case PromoState.Hidden: return "hidden";
                default: return "hidden";
            }
        }
    }
}
