using System.Collections.Generic;
using AlmediaLink.Models;
using NewModels = AlmediaSDK;

namespace AlmediaLink
{
    // The status and reason casts depend on LegacyStatus and LegacyNotAvailableReason mirroring the
    // 1.x enums member for member, in the same order. Every other type maps by value.
    internal static class Compat
    {
        public static AlmediaStatus ToOldApi(NewModels.LegacyStatus status) => (AlmediaStatus)(int)status;

        public static AlmediaNotAvailableReason? ToOldApi(NewModels.LegacyNotAvailableReason? reason)
            => reason.HasValue ? (AlmediaNotAvailableReason)(int)reason.Value : (AlmediaNotAvailableReason?)null;

        public static AlmediaScreenAvailability ToOldApi(NewModels.LegacyScreenAvailability availability)
            => new AlmediaScreenAvailability(availability.CanShowRewardHub, availability.CanShowOffer);

        public static AlmediaScreen ToOldApi(NewModels.AlmediaScreen screen)
        {
            if (screen == NewModels.AlmediaScreen.RewardHub) return AlmediaScreen.RewardHub;
            if (screen == NewModels.AlmediaScreen.Offer) return AlmediaScreen.Offer;
            return AlmediaScreen.Linking;
        }

        public static AlmediaLogLevel ToOldApi(NewModels.AlmediaLogLevel level)
        {
            if (level == NewModels.AlmediaLogLevel.Verbose) return AlmediaLogLevel.Verbose;
            if (level == NewModels.AlmediaLogLevel.Info) return AlmediaLogLevel.Info;
            if (level == NewModels.AlmediaLogLevel.Warning) return AlmediaLogLevel.Warning;
            if (level == NewModels.AlmediaLogLevel.Error) return AlmediaLogLevel.Error;
            return AlmediaLogLevel.Debug;
        }

        public static AlmediaErrorCode ToOldApi(NewModels.AlmediaErrorCode code)
        {
            if (code == NewModels.AlmediaErrorCode.InvalidConfiguration) return AlmediaErrorCode.InvalidConfiguration;
            if (code == NewModels.AlmediaErrorCode.NetworkFailure) return AlmediaErrorCode.NetworkFailure;
            if (code == NewModels.AlmediaErrorCode.ServerError) return AlmediaErrorCode.ServerError;
            if (code == NewModels.AlmediaErrorCode.RateLimited) return AlmediaErrorCode.RateLimited;
            if (code == NewModels.AlmediaErrorCode.Disabled) return AlmediaErrorCode.Disabled;
            if (code == NewModels.AlmediaErrorCode.LinkingFailed) return AlmediaErrorCode.LinkingFailed;
            if (code == NewModels.AlmediaErrorCode.InvalidState) return AlmediaErrorCode.InvalidState;
            if (code == NewModels.AlmediaErrorCode.Unexpected) return AlmediaErrorCode.Unexpected;
            return AlmediaErrorCode.Unknown;
        }

        public static AlmediaError ToOldApi(NewModels.AlmediaError error)
            => error == null ? null : new AlmediaError(ToOldApi(error.Code), error.Message);

        public static AlmediaNotification ToOldApi(NewModels.AlmediaNotification n)
            => new AlmediaNotification(n.Id, n.Title, n.Message, n.Timestamp, n.ReceivedAt, n.Display, n.IconUrl);

        public static List<AlmediaNotification> ToOldApi(List<NewModels.AlmediaNotification> list)
        {
            if (list == null) return null;
            var result = new List<AlmediaNotification>(list.Count);
            foreach (var n in list) result.Add(ToOldApi(n));
            return result;
        }

        public static AlmediaInGameRewardGrant ToOldApi(NewModels.AlmediaInGameRewardGrant grant)
        {
            var rewards = new AlmediaInGameReward[grant.Rewards.Count];
            for (int i = 0; i < rewards.Length; i++)
                rewards[i] = new AlmediaInGameReward(grant.Rewards[i].Amount, grant.Rewards[i].Code);
            return new AlmediaInGameRewardGrant(grant.Id, grant.Timestamp, grant.ReceivedAt, rewards);
        }

        public static AlmediaProgress ToOldApi(NewModels.AlmediaProgress progress)
        {
            if (progress == null) return null;
            var pending = new AlmediaTask[progress.Pending.Count];
            for (int i = 0; i < pending.Length; i++) pending[i] = ToOldApi(progress.Pending[i]);
            var completed = new AlmediaCompletedTask[progress.Completed.Count];
            for (int i = 0; i < completed.Length; i++) completed[i] = ToOldApi(progress.Completed[i]);
            var expired = new AlmediaTask[progress.Expired.Count];
            for (int i = 0; i < expired.Length; i++) expired[i] = ToOldApi(progress.Expired[i]);
            return new AlmediaProgress(progress.Id, progress.Timestamp, progress.BuiltAt, progress.Username,
                ToOldApi(progress.Balance), ToOldApi(progress.Earned), pending, completed, expired);
        }

        public static AlmediaTaskCompletion ToOldApi(NewModels.AlmediaTaskCompletion completion)
            => new AlmediaTaskCompletion(completion.Id, ToOldApi(completion.Task));

        public static AlmediaBalanceChange ToOldApi(NewModels.AlmediaBalanceChange change)
            => new AlmediaBalanceChange(change.Id, ToOldApi(change.Balance), ToOldApi(change.Change));

        private static AlmediaCompletedTask ToOldApi(NewModels.AlmediaCompletedTask completed)
            => new AlmediaCompletedTask(ToOldApi(completed.Task), completed.Timestamp, completed.CompletedAt, ToOldApi(completed.ActualReward));

        private static AlmediaTask ToOldApi(NewModels.AlmediaTask task)
            => new AlmediaTask(task.Id, task.Kind, task.Title, ToOldApi(task.Reward),
                task.Progress == null ? null : new AlmediaTaskProgress(task.Progress.Value, task.Progress.Target),
                task.RewardDropsAtTimestamp);

        private static AlmediaRewardPoints ToOldApi(NewModels.AlmediaRewardPoints points)
            => new AlmediaRewardPoints(points.Coins, ToOldApi(points.InPlayerCurrency), ToOldApi(points.InUsd));

        private static AlmediaMoney ToOldApi(NewModels.AlmediaMoney money) => new AlmediaMoney(money.Amount, money.Currency);

        public static InAppScreenResult ToOldApi(NewModels.AlmediaInAppScreenResult result)
        {
            switch (result)
            {
                case NewModels.AlmediaInAppScreenResult.Completed _:
                    return new InAppScreenResult(InAppScreenResultType.Completed);
                case NewModels.AlmediaInAppScreenResult.Failed failed:
                    return new InAppScreenResult(InAppScreenResultType.Failed, ToOldApi(failed.Error));
                default:
                    return new InAppScreenResult(InAppScreenResultType.Cancelled);
            }
        }

        public static NewModels.LegacyStatus ToNewApi(AlmediaStatus status) => (NewModels.LegacyStatus)(int)status;

        public static NewModels.AlmediaErrorCode ToNewApi(AlmediaErrorCode code)
        {
            switch (code)
            {
                case AlmediaErrorCode.InvalidConfiguration: return NewModels.AlmediaErrorCode.InvalidConfiguration;
                case AlmediaErrorCode.NetworkFailure: return NewModels.AlmediaErrorCode.NetworkFailure;
                case AlmediaErrorCode.ServerError: return NewModels.AlmediaErrorCode.ServerError;
                case AlmediaErrorCode.RateLimited: return NewModels.AlmediaErrorCode.RateLimited;
                case AlmediaErrorCode.Disabled: return NewModels.AlmediaErrorCode.Disabled;
                case AlmediaErrorCode.LinkingFailed: return NewModels.AlmediaErrorCode.LinkingFailed;
                case AlmediaErrorCode.InvalidState: return NewModels.AlmediaErrorCode.InvalidState;
                case AlmediaErrorCode.Unexpected: return NewModels.AlmediaErrorCode.Unexpected;
                default: return NewModels.AlmediaErrorCode.Unknown;
            }
        }

        public static NewModels.AlmediaScreen ToNewApi(AlmediaScreen screen)
        {
            switch (screen)
            {
                case AlmediaScreen.RewardHub: return NewModels.AlmediaScreen.RewardHub;
                case AlmediaScreen.Offer: return NewModels.AlmediaScreen.Offer;
                default: return NewModels.AlmediaScreen.Linking;
            }
        }

        // A null entry has no wire name and stays null; Resolve drops it.
        public static NewModels.AlmediaFeature ToNewApi(AlmediaFeature feature)
            => NewModels.AlmediaFeature.TryFromWireName(feature?.WireName, out var mapped) ? mapped : null;

        public static NewModels.AlmediaInAppScreenResult ToNewApi(InAppScreenResultType type, AlmediaErrorCode errorCode, string errorMessage)
        {
            switch (type)
            {
                case InAppScreenResultType.Completed:
                    return new NewModels.AlmediaInAppScreenResult.Completed();
                case InAppScreenResultType.Failed:
                    return new NewModels.AlmediaInAppScreenResult.Failed(new NewModels.AlmediaError(ToNewApi(errorCode), errorMessage));
                default:
                    return new NewModels.AlmediaInAppScreenResult.Cancelled();
            }
        }

        public static NewModels.AlmediaLogLevel ToNewApi(AlmediaLogLevel level)
        {
            switch (level)
            {
                case AlmediaLogLevel.Verbose: return NewModels.AlmediaLogLevel.Verbose;
                case AlmediaLogLevel.Info: return NewModels.AlmediaLogLevel.Info;
                case AlmediaLogLevel.Warning: return NewModels.AlmediaLogLevel.Warning;
                case AlmediaLogLevel.Error: return NewModels.AlmediaLogLevel.Error;
                default: return NewModels.AlmediaLogLevel.Debug;
            }
        }

        public static NewModels.AlmediaPlacementType ToNewApi(PlacementType placement)
        {
            switch (placement)
            {
                case PlacementType.RewardHub: return NewModels.AlmediaPlacementType.RewardHub;
                case PlacementType.Banner: return NewModels.AlmediaPlacementType.Banner;
                default: return NewModels.AlmediaPlacementType.Popup;
            }
        }

        public static NewModels.PromoState ToNewApi(PromoState state) => (NewModels.PromoState)(int)state;
    }
}
