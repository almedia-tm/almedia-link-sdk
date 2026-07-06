namespace AlmediaLink.Models
{
    public enum PlacementType
    {
        Popup,
        RewardHub,
        Banner
    }

    internal static class PlacementTypeExtensions
    {
        internal static string ToNativeString(this PlacementType placement)
        {
            switch (placement)
            {
                case PlacementType.Popup: return "popup";
                case PlacementType.RewardHub: return "reward_hub";
                case PlacementType.Banner: return "banner";
                default: return "popup";
            }
        }
    }
}
