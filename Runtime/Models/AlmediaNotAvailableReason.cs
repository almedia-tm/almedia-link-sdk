namespace AlmediaLink.Models
{
    /// <summary>
    /// Why the SDK is <see cref="AlmediaStatus.NotAvailable"/> for this player. Read the current
    /// value from <see cref="AlmediaLink.AlmediaLinkSDK.NotAvailableReason"/>.
    /// An unrecognized one reads as <see cref="Unknown"/>, so
    /// treat Unknown as "not available, cause unspecified" rather than an error.
    /// </summary>
    public enum AlmediaNotAvailableReason
    {
        /// <summary>The backend sent no reason, or one this SDK version does not recognize.</summary>
        Unknown,

        /// <summary>The player is in the holdout (control) group.</summary>
        Holdout,

        /// <summary>The game declared linking in its disabled features.</summary>
        Disabled
    }
}
