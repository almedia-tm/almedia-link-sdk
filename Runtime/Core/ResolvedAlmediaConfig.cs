using System.Collections.Generic;
using System.Text;

namespace AlmediaSDK
{
    /// <summary>
    /// The result of merging an AlmediaConfig with the settings-asset defaults.
    /// Contains final resolved values ready for the native bridge.
    /// </summary>
    /// <remarks>
    /// Declared as a <c>record</c> so two resolved configs compare by value -
    /// <see cref="Almedia.Initialize"/> relies on that to treat a repeat
    /// call with the same effective configuration as a no-op.
    /// </remarks>
    internal record ResolvedAlmediaConfig
    {
        public bool IsValid { get; set; }

        public string IntegrationKey { get; set; }
        public string Gaid { get; set; }
        public string Asid { get; set; }
        public string Oaid { get; set; }
        public string Idfa { get; set; }
        public string Idfv { get; set; }
        public string AdjustDeviceId { get; set; }
        public string AppsFlyerId { get; set; }
        public string AccountId { get; set; }
        public string TrafficSource { get; set; }
        public string Meta1 { get; set; }
        public string Meta2 { get; set; }
        public string Meta3 { get; set; }
        public string Meta4 { get; set; }
        public int NotificationsPollingIntervalSec { get; set; }

        // Canonical wire form (sorted, comma-joined), so the record's value equality
        // treats the same set in a different order as the same configuration.
        public string DisabledFeatures { get; set; }

#if UNITY_EDITOR
        /// <summary>Per-value budget for <c>TrafficSource</c> and <c>Meta1</c>-<c>Meta4</c>.</summary>
        internal const int MetaMaxBytes = 250;

        private static readonly string[] MetaNames = { "TrafficSource", "Meta1", "Meta2", "Meta3", "Meta4" };

        /// <summary>
        /// Meta values over the budget, in <see cref="MetaNames"/> order. The natives drop
        /// and report these themselves; the Editor has no native, so without this the first
        /// sign of a dropped value is a device build.
        /// </summary>
        internal List<string> OversizedMetaKeys()
        {
            var values = new[] { TrafficSource, Meta1, Meta2, Meta3, Meta4 };
            var oversized = new List<string>();

            for (var i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrEmpty(values[i]) && Encoding.UTF8.GetByteCount(values[i]) > MetaMaxBytes)
                    oversized.Add(MetaNames[i]);
            }

            return oversized;
        }
#endif
    }
}
