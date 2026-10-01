using System;

namespace AlmediaSDK
{
    // Wire shapes shared with the iOS and Android bridges. Nested objects are default-constructed
    // by JsonUtility when omitted, hence the initializers and the explicit hasProgress flag.

    [Serializable]
    internal class MoneyItem
    {
        // A string, never a JSON number: a number would pass through a double.
        public string amount = "";
        public string currency = "";
    }

    [Serializable]
    internal class RewardPointsItem
    {
        public long coins;
        public MoneyItem inPlayerCurrency = new MoneyItem();
        public MoneyItem inUsd = new MoneyItem();
    }

    [Serializable]
    internal class TaskProgressItem
    {
        public long value;
        public long target;
    }

    [Serializable]
    internal class TaskItem
    {
        public string id = "";
        public string kind = "";
        public string title = "";
        public RewardPointsItem reward = new RewardPointsItem();
        public bool hasProgress;
        public TaskProgressItem progress = new TaskProgressItem();
        public string rewardDropsAt = "";
    }

    [Serializable]
    internal class CompletedTaskItem
    {
        public TaskItem task = new TaskItem();
        public string completedAt = "";
        public RewardPointsItem actualReward = new RewardPointsItem();
    }

    /// <summary>
    /// Wire payload for <c>OnProgressUpdated</c>. Native sends <c>hasProgress</c> false and no
    /// <c>progress</c> key when it clears the snapshot with the stream token; JsonUtility cannot
    /// carry null, so the flag is the discriminator, as on tasks.
    /// </summary>
    [Serializable]
    internal class ProgressUpdatedResponse
    {
        public bool hasProgress;
        public ProgressResponse progress = new ProgressResponse();
    }

    /// <summary>The snapshot inside <see cref="ProgressUpdatedResponse"/>. Native omits <c>username</c> when unset, so "" means none.</summary>
    [Serializable]
    internal class ProgressResponse
    {
        public string id = "";
        public string timestamp = "";
        public string username = "";
        public RewardPointsItem balance = new RewardPointsItem();
        public RewardPointsItem earned = new RewardPointsItem();
        public TaskItem[] pending = Array.Empty<TaskItem>();
        public CompletedTaskItem[] completed = Array.Empty<CompletedTaskItem>();
        public TaskItem[] expired = Array.Empty<TaskItem>();
    }

    /// <summary>Wire payload for <c>OnTaskCompleted</c>.</summary>
    [Serializable]
    internal class TaskCompletedResponse
    {
        public string id = "";
        public CompletedTaskItem task = new CompletedTaskItem();
    }

    /// <summary>Wire payload for <c>OnBalanceChanged</c>.</summary>
    [Serializable]
    internal class BalanceChangedResponse
    {
        public string id = "";
        public RewardPointsItem balance = new RewardPointsItem();
        public RewardPointsItem change = new RewardPointsItem();
    }
}
