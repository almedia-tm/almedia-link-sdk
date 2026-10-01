using System;
using System.Collections.Generic;

namespace AlmediaSDK
{
    /// <summary>
    /// What went wrong, in <see cref="AlmediaError.Code"/>. A code this SDK version does not recognize
    /// reads as <see cref="Unknown"/>.
    /// </summary>
    public sealed class AlmediaErrorCode : IEquatable<AlmediaErrorCode>
    {
        public static readonly AlmediaErrorCode Unknown = new AlmediaErrorCode("unknown", nameof(Unknown));
        public static readonly AlmediaErrorCode InvalidConfiguration = new AlmediaErrorCode("invalidConfiguration", nameof(InvalidConfiguration));
        public static readonly AlmediaErrorCode NetworkFailure = new AlmediaErrorCode("networkFailure", nameof(NetworkFailure));
        public static readonly AlmediaErrorCode ServerError = new AlmediaErrorCode("serverError", nameof(ServerError));
        public static readonly AlmediaErrorCode RateLimited = new AlmediaErrorCode("rateLimited", nameof(RateLimited));
        public static readonly AlmediaErrorCode Disabled = new AlmediaErrorCode("disabled", nameof(Disabled));
        public static readonly AlmediaErrorCode LinkingFailed = new AlmediaErrorCode("linkingFailed", nameof(LinkingFailed));
        public static readonly AlmediaErrorCode InvalidState = new AlmediaErrorCode("invalidState", nameof(InvalidState));
        public static readonly AlmediaErrorCode Unexpected = new AlmediaErrorCode("unexpected", nameof(Unexpected));

        internal static readonly IReadOnlyList<AlmediaErrorCode> All = new[]
        {
            Unknown, InvalidConfiguration, NetworkFailure, ServerError, RateLimited, Disabled, LinkingFailed,
            InvalidState, Unexpected
        };

        private readonly string _wireName;
        private readonly string _name;

        private AlmediaErrorCode(string wireName, string name)
        {
            _wireName = wireName;
            _name = name;
        }

        internal string WireName => _wireName;

        internal static AlmediaErrorCode FromWire(string value)
        {
            switch (value)
            {
                case "invalidConfiguration": return InvalidConfiguration;
                case "networkFailure": return NetworkFailure;
                case "serverError": return ServerError;
                case "rateLimited": return RateLimited;
                case "disabled": return Disabled;
                case "linkingFailed": return LinkingFailed;
                case "invalidState": return InvalidState;
                case "unexpected": return Unexpected;
                default: return Unknown;
            }
        }

        public bool Equals(AlmediaErrorCode other)
            => !(other is null) && string.Equals(_wireName, other._wireName, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as AlmediaErrorCode);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_wireName);

        public static bool operator ==(AlmediaErrorCode a, AlmediaErrorCode b) => a is null ? b is null : a.Equals(b);

        public static bool operator !=(AlmediaErrorCode a, AlmediaErrorCode b) => !(a == b);

        public override string ToString() => _name;
    }

    public sealed class AlmediaError : IEquatable<AlmediaError>
    {
        public AlmediaErrorCode Code { get; }
        public string Message { get; }

        public AlmediaError(AlmediaErrorCode code, string message)
        {
            Code = code ?? AlmediaErrorCode.Unknown;
            Message = message;
        }

        internal static AlmediaError FromCallback(ErrorCallbackResponse response)
            => new AlmediaError(AlmediaErrorCode.FromWire(response.code), response.message);

        public bool Equals(AlmediaError other)
            => !(other is null) && Code == other.Code && string.Equals(Message, other.Message, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as AlmediaError);

        public override int GetHashCode() => ValueEquality.Hash(Code, Message);

        public static bool operator ==(AlmediaError a, AlmediaError b) => a is null ? b is null : a.Equals(b);

        public static bool operator !=(AlmediaError a, AlmediaError b) => !(a == b);
    }
}
