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

        // Maps the native wire payload to the public result. The result STRING is the sole
        // discriminator - never `error != null`. JsonUtility cannot represent a null nested
        // object: a non-failed payload deserializes `error` to a default-constructed instance
        // (empty code/message), not null, so keying off the field would misclassify every
        // completed/cancelled dismissal as a failure. See ScreenDismissedResponse.
        internal static InAppScreenResult FromResponse(ScreenDismissedResponse response)
        {
            switch (response.result)
            {
                case "completed":
                    return new InAppScreenResult(InAppScreenResultType.Completed);
                case "cancelled":
                    return new InAppScreenResult(InAppScreenResultType.Cancelled);
                case "failed":
                    var error = response.error != null
                        ? AlmediaError.FromCallback(response.error)
                        : new AlmediaError(AlmediaErrorCode.Unknown, "In-app screen failed to load.");
                    return new InAppScreenResult(InAppScreenResultType.Failed, error);
                default:
                    AlmediaLog.Warning($"Unrecognized in-app screen result '{response.result}'; treating as cancelled.");
                    return new InAppScreenResult(InAppScreenResultType.Cancelled);
            }
        }
    }
}
