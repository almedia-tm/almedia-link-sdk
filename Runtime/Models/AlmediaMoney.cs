using System;
using System.Globalization;

namespace AlmediaLink.Models
{
    /// <summary>
    /// A coin amount in real money, for display only. The SDK does not format money.
    /// Format <see cref="Amount"/> with your own symbols, decimals and fonts. Equality is numeric:
    /// <c>12.50</c> equals <c>12.5</c>.
    /// </summary>
    public sealed class AlmediaMoney : IEquatable<AlmediaMoney>
    {
        /// <summary>
        /// An exact decimal value. The SDK parses it from the wire string, independent of the device
        /// locale, and never through a <c>float</c> or a <c>double</c>. The scale from the server
        /// stays: <c>12.50</c> stays <c>12.50</c>.
        /// </summary>
        public decimal Amount { get; }

        /// <summary>The ISO 4217 code of <see cref="Amount"/>, in upper case.</summary>
        public string Currency { get; }

        public AlmediaMoney(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public override string ToString() => $"{Amount.ToString(CultureInfo.InvariantCulture)} {Currency}";

        public bool Equals(AlmediaMoney other)
            => other != null
               && Amount == other.Amount
               && string.Equals(Currency, other.Currency, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as AlmediaMoney);

        public override int GetHashCode() => AlmediaSDK.ValueEquality.Hash(Amount, Currency);

        public static bool operator ==(AlmediaMoney a, AlmediaMoney b) => a is null ? b is null : a.Equals(b);

        public static bool operator !=(AlmediaMoney a, AlmediaMoney b) => !(a == b);
    }
}
