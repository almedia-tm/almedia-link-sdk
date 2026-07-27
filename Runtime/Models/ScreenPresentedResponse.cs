namespace AlmediaLink.Models
{
    /// <summary>
    /// Wire payload for the screen-presented callback:
    /// <c>{"screen":"linking"|"reward_hub"|"offer"}</c>.
    /// </summary>
    [System.Serializable]
    internal class ScreenPresentedResponse
    {
        public string screen = "";
    }
}
