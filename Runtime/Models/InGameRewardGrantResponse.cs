using System;

namespace AlmediaLink.Models
{
    /// <summary>
    /// Wire payload for the in-game-reward-grant callback:
    /// <c>{"id":string,"timestamp":string,"rewards":[{"amount":number,"code":string}]}</c>.
    /// Native emits it once per grant, never batched.
    /// </summary>
    [Serializable]
    internal class InGameRewardGrantResponse
    {
        public string id = "";
        public string timestamp = "";
        public InGameRewardItem[] rewards = Array.Empty<InGameRewardItem>();
    }

    [Serializable]
    internal class InGameRewardItem
    {
        // The contract permits fractional amounts
        public double amount;
        public string code = "";
    }
}
