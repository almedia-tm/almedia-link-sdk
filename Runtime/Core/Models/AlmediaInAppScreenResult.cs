using System;

namespace AlmediaSDK
{
    /// <summary>
    /// How an in-app screen (linking, reward hub, offer) was dismissed. Delivered through
    /// <see cref="Almedia.OnScreenDismissed"/>. Keep a default branch when you switch on it: a later
    /// version can add cases.
    /// </summary>
    public abstract class AlmediaInAppScreenResult : IEquatable<AlmediaInAppScreenResult>
    {
        private AlmediaInAppScreenResult() { }

        /// <summary>Closed by the web client through the JS bridge.</summary>
        public sealed class Completed : AlmediaInAppScreenResult
        {
            public Completed() { }
        }

        /// <summary>Closed by the user via the close or back button.</summary>
        public sealed class Cancelled : AlmediaInAppScreenResult
        {
            public Cancelled() { }
        }

        /// <summary>The screen failed to load.</summary>
        public sealed class Failed : AlmediaInAppScreenResult
        {
            public Failed(AlmediaError error)
            {
                Error = error ?? new AlmediaError(AlmediaErrorCode.Unknown, "In-app screen failed to load.");
            }

            public AlmediaError Error { get; }
        }

        public bool Equals(AlmediaInAppScreenResult other)
        {
            if (other is null || other.GetType() != GetType()) return false;
            switch (this)
            {
                case Failed failed:
                    return failed.Error == ((Failed)other).Error;
                default:
                    return true;
            }
        }

        public override bool Equals(object obj) => Equals(obj as AlmediaInAppScreenResult);

        public override int GetHashCode()
        {
            switch (this)
            {
                case Failed failed: return ValueEquality.Hash(GetType().Name, failed.Error);
                default: return ValueEquality.Hash(GetType().Name);
            }
        }

        public static bool operator ==(AlmediaInAppScreenResult a, AlmediaInAppScreenResult b) => a is null ? b is null : a.Equals(b);

        public static bool operator !=(AlmediaInAppScreenResult a, AlmediaInAppScreenResult b) => !(a == b);

        // The 1.x log line "Screen dismissed: X (Completed)" prints this text.
        public override string ToString() => GetType().Name;

        internal string WireName
        {
            get
            {
                switch (this)
                {
                    case Completed _: return "completed";
                    case Failed _: return "failed";
                    default: return "cancelled";
                }
            }
        }

        // Maps the native wire payload to the public result. The result STRING is the sole
        // discriminator - never `error != null`. JsonUtility cannot represent a null nested
        // object: a non-failed payload deserializes `error` to a default-constructed instance
        // (empty code/message), not null, so keying off the field would misclassify every
        // completed/cancelled dismissal as a failure. See ScreenDismissedResponse.
        internal static AlmediaInAppScreenResult FromResponse(ScreenDismissedResponse response)
        {
            switch (response.result)
            {
                case "completed":
                    return new Completed();
                case "cancelled":
                    return new Cancelled();
                case "failed":
                    return new Failed(response.error != null ? AlmediaError.FromCallback(response.error) : null);
                default:
                    AlmediaLog.Warning($"Unrecognized in-app screen result '{response.result}'; treating as cancelled.");
                    return new Cancelled();
            }
        }
    }
}
