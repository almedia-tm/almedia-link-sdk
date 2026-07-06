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

        internal static AlmediaError FromCallback(ErrorCallbackResponse response)
        {
            var code = MapErrorCode(response.code);
            return new AlmediaError(code, response.message);
        }

        private static AlmediaErrorCode MapErrorCode(string code)
        {
            switch (code)
            {
                case "invalidConfiguration": return AlmediaErrorCode.InvalidConfiguration;
                case "networkFailure": return AlmediaErrorCode.NetworkFailure;
                case "serverError": return AlmediaErrorCode.ServerError;
                case "rateLimited": return AlmediaErrorCode.RateLimited;
                case "disabled": return AlmediaErrorCode.Disabled;
                case "linkingFailed": return AlmediaErrorCode.LinkingFailed;
                case "invalidState": return AlmediaErrorCode.InvalidState;
                case "unexpected": return AlmediaErrorCode.Unexpected;
                default: return AlmediaErrorCode.Unknown;
            }
        }
    }
}
