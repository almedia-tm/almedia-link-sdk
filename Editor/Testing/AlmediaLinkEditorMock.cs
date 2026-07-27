using System;
using AlmediaLink.Bridge;
using AlmediaLink.Models;

namespace AlmediaLink.Editor.Testing
{
    /// <summary>
    /// Editor-only test hook for driving AlmediaLinkSDK into any state (status, error,
    /// notifications, native log) without going through a real device or
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
        /// Delivers a status transition. <see cref="AlmediaLinkSDK.CurrentStatus"/> and
        /// <see cref="AlmediaLinkSDK.OnStatusChanged"/> reflect the new value synchronously.
        /// </summary>
        public static void EmitStatus(AlmediaStatus status)
            => Mock().EmitStatus(status);

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
        /// Legacy hook for a pre-prompt screen the SDK no longer shows. Raises the bridge's
        /// ShowATTPrePrompt event, which has no subscribers, so no UI appears; like every Emit*
        /// it still flips the mock into manual mode. Kept so existing QA scripts compile;
        /// scheduled for removal in a future release.
        /// </summary>
        [Obsolete("The SDK no longer shows an ATT pre-prompt; this raises an event with no subscribers and has no effect. Scheduled for removal in a future release.")]
        public static void EmitShowATTPrePrompt()
            => Mock().EmitShowATTPrePrompt();

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
            type = n.Type ?? ""
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
        public readonly string Type;
        public readonly string Timestamp;

        /// <summary>
        /// Constructs a notification for <see cref="AlmediaLinkEditorMock.EmitNotifications"/>.
        /// A null <paramref name="timestamp"/> defaults to the current UTC time in ISO-8601 (round-trip "o" format),
        /// matching the format the backend emits.
        /// </summary>
        public MockNotification(string id, string title, string message, string type, string timestamp = null)
        {
            Id = id;
            Title = title;
            Message = message;
            Type = type;
            Timestamp = timestamp ?? DateTime.UtcNow.ToString("o");
        }
    }
}
