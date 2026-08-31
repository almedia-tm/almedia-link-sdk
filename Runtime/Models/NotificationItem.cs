namespace AlmediaLink.Models
{
    [System.Serializable]
    internal class NotificationItem
    {
        public string id = "";
        public string title = "";
        public string message = "";
        public string timestamp = "";

        // Presentation hint, "popup" or "tray"; native maps unrecognized server
        // values to "popup" before building this payload.
        public string type = "";

        // Optional on the wire: native omits the key entirely when absent, which
        // JsonUtility reads as this default. "" therefore means "no icon".
        public string iconUrl = "";
    }
}
