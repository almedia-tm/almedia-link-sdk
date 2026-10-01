using System;
using System.Collections.Generic;

namespace AlmediaSDK
{
    /// <summary>
    /// Why the status is <see cref="AlmediaStatus.NotAvailable"/>. A reason this SDK version does not
    /// recognize reads as <see cref="Unknown"/>. <see cref="AlmediaStatus.NotAvailable.RawReason"/> keeps
    /// what the server sent.
    /// </summary>
    public sealed class AlmediaNotAvailableReason : IEquatable<AlmediaNotAvailableReason>
    {
        /// <summary>The server sent no reason, or one this SDK version does not recognize.</summary>
        public static readonly AlmediaNotAvailableReason Unknown = new AlmediaNotAvailableReason("unknown", nameof(Unknown));

        /// <summary>The player is in the holdout (control) group.</summary>
        public static readonly AlmediaNotAvailableReason Holdout = new AlmediaNotAvailableReason("holdout", nameof(Holdout));

        /// <summary>The game declared linking in its disabled features.</summary>
        public static readonly AlmediaNotAvailableReason Disabled = new AlmediaNotAvailableReason("disabled", nameof(Disabled));

        internal static readonly IReadOnlyList<AlmediaNotAvailableReason> All = new[] { Unknown, Holdout, Disabled };

        private readonly string _wireName;
        private readonly string _name;

        private AlmediaNotAvailableReason(string wireName, string name)
        {
            _wireName = wireName;
            _name = name;
        }

        internal static AlmediaNotAvailableReason FromWire(string value)
        {
            switch (value)
            {
                case "holdout": return Holdout;
                case "disabled": return Disabled;
                default: return Unknown;
            }
        }

        public bool Equals(AlmediaNotAvailableReason other)
            => !(other is null) && string.Equals(_wireName, other._wireName, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as AlmediaNotAvailableReason);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_wireName);

        public static bool operator ==(AlmediaNotAvailableReason a, AlmediaNotAvailableReason b) => a is null ? b is null : a.Equals(b);

        public static bool operator !=(AlmediaNotAvailableReason a, AlmediaNotAvailableReason b) => !(a == b);

        public override string ToString() => _name;
    }
}
