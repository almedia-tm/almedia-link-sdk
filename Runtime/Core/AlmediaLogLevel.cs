using System;
using System.Collections.Generic;

namespace AlmediaSDK
{
    /// <summary>
    /// The severity of a line delivered through <see cref="Almedia.OnLog"/>, from <see cref="Verbose"/>
    /// to <see cref="Error"/>. Compare levels with <c>&lt;</c>, <c>&lt;=</c>, <c>&gt;</c> and <c>&gt;=</c>.
    /// </summary>
    public sealed class AlmediaLogLevel : IEquatable<AlmediaLogLevel>
    {
        public static readonly AlmediaLogLevel Verbose = new AlmediaLogLevel("verbose", nameof(Verbose), 0);
        public static readonly AlmediaLogLevel Debug = new AlmediaLogLevel("debug", nameof(Debug), 1);
        public static readonly AlmediaLogLevel Info = new AlmediaLogLevel("info", nameof(Info), 2);
        public static readonly AlmediaLogLevel Warning = new AlmediaLogLevel("warning", nameof(Warning), 3);
        public static readonly AlmediaLogLevel Error = new AlmediaLogLevel("error", nameof(Error), 4);

        internal static readonly IReadOnlyList<AlmediaLogLevel> All = new[] { Verbose, Debug, Info, Warning, Error };

        private readonly string _wireName;
        private readonly string _name;
        private readonly int _severity;

        private AlmediaLogLevel(string wireName, string name, int severity)
        {
            _wireName = wireName;
            _name = name;
            _severity = severity;
        }

        internal string WireName => _wireName;

        internal static AlmediaLogLevel FromWire(string value)
        {
            switch (value?.ToLowerInvariant())
            {
                case "verbose": return Verbose;
                case "debug": return Debug;
                case "info": return Info;
                case "warning": return Warning;
                case "error": return Error;
                default: return Debug;
            }
        }

        public bool Equals(AlmediaLogLevel other)
            => !(other is null) && string.Equals(_wireName, other._wireName, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as AlmediaLogLevel);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_wireName);

        public static bool operator ==(AlmediaLogLevel a, AlmediaLogLevel b) => a is null ? b is null : a.Equals(b);

        public static bool operator !=(AlmediaLogLevel a, AlmediaLogLevel b) => !(a == b);

        private static int SeverityOf(AlmediaLogLevel level) => level is null ? -1 : level._severity;

        public static bool operator <(AlmediaLogLevel a, AlmediaLogLevel b) => SeverityOf(a) < SeverityOf(b);

        public static bool operator >(AlmediaLogLevel a, AlmediaLogLevel b) => SeverityOf(a) > SeverityOf(b);

        public static bool operator <=(AlmediaLogLevel a, AlmediaLogLevel b) => SeverityOf(a) <= SeverityOf(b);

        public static bool operator >=(AlmediaLogLevel a, AlmediaLogLevel b) => SeverityOf(a) >= SeverityOf(b);

        public override string ToString() => _name;
    }
}
