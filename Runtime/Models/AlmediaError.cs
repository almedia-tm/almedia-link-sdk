namespace AlmediaLink.Models
{
    public enum AlmediaErrorCode
    {
        Unknown = 0,
        InvalidConfiguration,
        NetworkFailure,
        ServerError,
        RateLimited,
        Disabled,
        LinkingFailed,
        InvalidState,
        Unexpected
    }

    public class AlmediaError
    {
        public AlmediaErrorCode Code { get; }
        public string Message { get; }

        public AlmediaError(AlmediaErrorCode code, string message)
        {
            Code = code;
            Message = message;
        }
    }
}
