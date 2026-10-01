namespace AlmediaLink.Models
{
    /// <summary>
    /// How an in-app screen (linking, reward hub, offer) was dismissed. Delivered through
    /// <see cref="AlmediaLinkSDK.OnScreenDismissed"/>.
    /// </summary>
    public enum InAppScreenResultType
    {
        /// <summary>Closed by the web client through the JS bridge.</summary>
        Completed,

        /// <summary>Closed by the user via the close/back button.</summary>
        Cancelled,

        /// <summary>The screen failed to load. See <see cref="InAppScreenResult.Error"/>.</summary>
        Failed
    }

    /// <summary>
    /// The outcome of an in-app screen session. <see cref="Error"/> is non-null only when
    /// <see cref="Type"/> is <see cref="InAppScreenResultType.Failed"/>.
    /// </summary>
    public class InAppScreenResult
    {
        public InAppScreenResultType Type { get; }

        /// <summary>The failure detail, or <c>null</c> unless <see cref="Type"/> is Failed.</summary>
        public AlmediaError Error { get; }

        internal InAppScreenResult(InAppScreenResultType type, AlmediaError error = null)
        {
            Type = type;
            Error = error;
        }
    }
}
