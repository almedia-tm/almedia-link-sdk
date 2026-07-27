namespace AlmediaLink.Models
{
    /// <summary>
    /// Wire payload for the screen-dismissed callback:
    /// <c>{"screen":"linking"|"reward_hub"|"offer","result":"completed"|"cancelled"|"failed","error":null|{"code","message"}}</c>.
    /// </summary>
    [System.Serializable]
    internal class ScreenDismissedResponse
    {
        public string screen = "";

        public string result = "";

        // Populated only when result == "failed". JsonUtility deserializes a JSON null here to a
        // default-constructed instance rather than null, so consumers must key off `result`, never
        // this field. See InAppScreenResult.FromResponse.
        public ErrorCallbackResponse error;
    }
}
