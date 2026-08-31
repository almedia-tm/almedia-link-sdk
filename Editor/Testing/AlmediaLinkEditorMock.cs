using System;
using AlmediaLink.Bridge;
using AlmediaLink.Models;

namespace AlmediaLink.Editor.Testing
{
    /// <summary>
    /// Editor-only test hook for driving AlmediaLinkSDK into any state (status, error,
    /// notifications, in-game reward grants, native log) without going through a real device or
    /// backend. Lives in the AlmediaLink.Editor assembly (includePlatforms:["Editor"]) so
    /// the class does not exist in iOS/Android player builds at the assembly level
    /// host code that references it will fail to compile on a player target.
    ///
    /// The first call to any method here puts the underlying EditorMockBridge into manual
    /// mode for the rest of the play session: every subsequent auto-simulate path
    /// (Initialize/StartLinking/FetchNotifications/...) becomes a no-op so canned coroutines
    /// cannot race against test emissions. Manual mode is reset on domain reload.
    /// </summary>
    public static class AlmediaLinkEditorMock
    {
        /// <summary>
        /// Delivers a status transition. <see cref="AlmediaLinkSDK.CurrentStatus"/>,
        /// <see cref="AlmediaLinkSDK.NotAvailableReason"/>,
        /// <see cref="AlmediaLinkSDK.ScreenAvailability"/> and their events reflect the new
        /// values synchronously. <paramref name="reason"/> models the wire reason and is
        /// meaningful only with <see cref="AlmediaStatus.NotAvailable"/> ("holdout" maps to
        /// <see cref="AlmediaNotAvailableReason.Holdout"/>, anything else to Unknown). Omitted
        /// availability flags default to (status == Linked), mirroring the happy-path native
        /// derivation; pass explicit values to model a linked player losing a screen.
        /// </summary>
        public static void EmitStatus(AlmediaStatus status, string reason = null,
            bool? canShowRewardHub = null, bool? canShowOffer = null)
            => Mock().EmitStatus(status, reason, canShowRewardHub, canShowOffer);

        /// <summary>
        /// Fires <see cref="AlmediaLinkSDK.OnErrorOccurred"/> with the given code and message.
        /// Use this to exercise error-handling UI under every <see cref="AlmediaErrorCode"/> value.
        /// </summary>
        public static void EmitError(AlmediaErrorCode code, string message)
            => Mock().EmitError(code, message);

        /// <summary>
        /// Fires <see cref="AlmediaLinkSDK.OnLinkCompleted"/> with the current UTC timestamp.
        /// Use this to test fresh-link UX without driving the full linking flow.
        /// </summary>
        public static void EmitLinkCompleted()
            => Mock().EmitLinkCompleted();

        /// <summary>
        /// Fires <see cref="AlmediaLinkSDK.OnNotificationsReceived"/> with the supplied items.
        /// Pass no arguments for an empty batch. Note: AlmediaLinkSDK short-circuits empty
        /// batches and does not raise the event in that case.
        /// </summary>
        public static void EmitNotifications(params MockNotification[] items)
        {
            var bridge = Mock();
            var converted = items == null
                ? Array.Empty<NotificationItem>()
                : Array.ConvertAll(items, ToItem);
            bridge.EmitNotifications(converted);
        }

        /// <summary>
        /// Fires <see cref="AlmediaLinkSDK.OnInGameRewardGrantRequested"/> with a generated grant id and
        /// the current UTC timestamp. Pass at least one reward; the SDK drops a rewardless
        /// grant as malformed, which this can also exercise.
        /// </summary>
        public static void EmitInGameRewardGrant(params MockInGameReward[] rewards)
            => EmitInGameRewardGrant(null, rewards);

        /// <summary>
        /// Fires <see cref="AlmediaLinkSDK.OnInGameRewardGrantRequested"/> with an explicit grant id.
        /// Delivery on device is at-least-once, so call this twice with the same id to
        /// reproduce a redelivered grant and exercise host-side deduplication.
        /// A null or empty <paramref name="id"/> generates one.
        /// </summary>
        public static void EmitInGameRewardGrant(string id, params MockInGameReward[] rewards)
        {
            var bridge = Mock();
            var converted = rewards == null
                ? Array.Empty<InGameRewardItem>()
                : Array.ConvertAll(rewards, ToRewardItem);
            bridge.EmitInGameRewardGrant(new InGameRewardGrantResponse
            {
                id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id,
                timestamp = DateTime.UtcNow.ToString("o"),
                rewards = converted
            });
        }

        /// <summary>
        /// Fires <see cref="AlmediaLinkSDK.OnScreenPresented"/> for the given screen, as if the
        /// native container had committed to presenting it. Pair it with a later
        /// <see cref="EmitScreenDismissed"/> to reproduce native's matched-pair contract.
        /// </summary>
        public static void EmitScreenPresented(AlmediaScreen screen)
            => Mock().EmitScreenPresented(screen);

        /// <summary>
        /// Fires <see cref="AlmediaLinkSDK.OnScreenDismissed"/> for the given screen with the given
        /// result. Supply an error code and message only for
        /// <see cref="InAppScreenResultType.Failed"/>; they are ignored for completed/cancelled
        /// outcomes.
        /// </summary>
        public static void EmitScreenDismissed(AlmediaScreen screen, InAppScreenResultType result,
            AlmediaErrorCode errorCode = AlmediaErrorCode.Unknown, string errorMessage = null)
            => Mock().EmitScreenDismissed(screen, result, errorCode, errorMessage);

        /// <summary>
        /// Compatibility shim: the SDK no longer shows an ATT pre-prompt. Still flips the mock into
        /// manual mode (and still throws before <see cref="AlmediaLinkSDK.Initialize"/>) exactly like
        /// every other emit, but delivers nothing.
        /// </summary>
        [Obsolete("The SDK no longer shows an ATT pre-prompt; this emit has no effect.")]
        public static void EmitShowATTPrePrompt()
        {
            Mock();
            AlmediaLog.Warning("EmitShowATTPrePrompt is a no-op: the ATT pre-prompt was removed in 1.2.0.");
        }

        /// <summary>
        /// Delivers a forwarded log line through the same path the iOS/Android native plugins use.
        /// Subscribers of <see cref="AlmediaLinkSDK.OnLog"/> receive it as if it had come from native.
        /// </summary>
        public static void EmitNativeLog(AlmediaLogLevel level, string message)
            => Mock().EmitNativeLog(level, message);

        /// <summary>
        /// Stops any pending auto-simulate coroutine. The first call to any other Emit* method
        /// invokes this internally as part of the manual-mode flip; call it explicitly when a
        /// test needs to assert that no callback fires after <see cref="AlmediaLinkSDK.Initialize"/>.
        /// </summary>
        public static void CancelPending()
            => Mock().CancelPending();

        private static EditorMockBridge Mock()
        {
            var bridge = NativeBridgeFactory.ActiveMock;
            if (bridge == null)
            {
                throw new InvalidOperationException(
                    "AlmediaLinkEditorMock: SDK not initialized. Call AlmediaLinkSDK.Initialize(...) first.");
            }
            bridge.EnterManualMode();
            return bridge;
        }

        private static NotificationItem ToItem(MockNotification n) => new NotificationItem
        {
            id = n.Id ?? "",
            title = n.Title ?? "",
            message = n.Message ?? "",
            timestamp = n.Timestamp ?? "",
            type = n.Display ?? "",
            iconUrl = n.IconUrl ?? ""
        };

        private static InGameRewardItem ToRewardItem(MockInGameReward r) => new InGameRewardItem
        {
            amount = r.Amount,
            code = r.Code ?? ""
        };
    }

    /// <summary>
    /// Public test-facing notification shape; converted to the internal NotificationItem
    /// DTO when emitted. Field-named so calls stay readable as the protocol evolves.
    /// </summary>
    public readonly struct MockNotification
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Message;
        public readonly string Display;
        public readonly string Timestamp;
        public readonly string IconUrl;

        /// <summary>Alias of <see cref="Display"/>.</summary>
        [Obsolete("Since 1.2.0 the wire field carries the presentation hint (\"popup\"/\"tray\"). Use Display.")]
        public string Type => Display;

        /// <summary>
        /// Constructs a notification for <see cref="AlmediaLinkEditorMock.EmitNotifications"/>.
        /// <paramref name="display"/> models the wire presentation hint ("popup" or "tray";
        /// native never forwards anything else). A null <paramref name="timestamp"/> defaults
        /// to the current UTC time in ISO-8601 (round-trip "o" format), matching the format
        /// the backend emits. A null <paramref name="iconUrl"/> models the omitted wire key.
        /// </summary>
        public MockNotification(string id, string title, string message, string display,
            string timestamp = null, string iconUrl = null)
        {
            Id = id;
            Title = title;
            Message = message;
            Display = display;
            Timestamp = timestamp ?? DateTime.UtcNow.ToString("o");
            IconUrl = iconUrl;
        }
    }

    /// <summary>
    /// One reward line item for <see cref="AlmediaLinkEditorMock.EmitInGameRewardGrant(MockInGameReward[])"/>.
    /// </summary>
    public readonly struct MockInGameReward
    {
        public readonly double Amount;
        public readonly string Code;

        public MockInGameReward(double amount, string code)
        {
            Amount = amount;
            Code = code;
        }
    }
}
