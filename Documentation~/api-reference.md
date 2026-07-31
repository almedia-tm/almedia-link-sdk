# Almedia Link SDK - API Reference

Complete reference for every public type in the Almedia Link Unity SDK. For task-driven guidance (install, configure, customize), see the [integration guide](./integration-guide.md).

The SDK is versioned. The current release is reflected in `AlmediaLinkSDK.Version` and in `Packages/com.almedia.link/package.json`.

---

## Contents

- [Namespaces](#namespaces)
- [`AlmediaLinkSDK`](#almedialinksdk) - public static facade
- [`AlmediaLinkConfig`](#almedialinkconfig) - runtime configuration
- [`AlmediaLinkSettings`](#almedialinksettings) - ScriptableObject configuration
- Models
  - [`AlmediaStatus`](#almediastatus)
  - [`AlmediaError`](#almediaerror)
  - [`AlmediaErrorCode`](#almediaerrorcode)
  - [`AlmediaNotification`](#almedianotification)
  - [`PlacementType`](#placementtype)
  - [`AlmediaScreen`](#almediascreen)
  - [`InAppScreenResult`](#inappscreenresult)
  - [`InAppScreenResultType`](#inappscreenresulttype)
  - [`AlmediaLogLevel`](#almedialoglevel)
- [Editor testing](#editor-testing)
  - [`AlmediaLinkEditorMock`](#almedialinkeditormock)
  - [`MockNotification`](#mocknotification)
- UI components
  - [`LinkButtonController`](#linkbuttoncontroller)
  - [`LinkPopupController`](#linkpopupcontroller)
  - [`NotificationCardController`](#notificationcardcontroller)
  - [`ActivityOverlayController`](#activityoverlaycontroller)
  - [`NotificationIconMap`](#notificationiconmap)
- [Prefabs](#prefabs)
- [Editor menu items](#editor-menu-items)
- [Assembly definitions](#assembly-definitions)

---

## Namespaces

| Namespace            | Contents |
|----------------------|----------|
| `AlmediaLink`        | The static `AlmediaLinkSDK` facade, `AlmediaLinkConfig`, `AlmediaLinkSettings`, `AlmediaLogLevel`. |
| `AlmediaLink.Models` | Status, error, notification, placement, and in-app screen result types. |
| `AlmediaLink.UI`     | UI controllers and `NotificationIconMap`. |
| `AlmediaLink.Editor` | Editor-only tooling (Settings window, iOS post-build hook). Excluded from player builds. |
| `AlmediaLink.Editor.Testing` | `AlmediaLinkEditorMock` and `MockNotification` - editor-only test hooks for driving non-happy paths. Excluded from player builds. |

Most host code only needs `using AlmediaLink;` and `using AlmediaLink.Models;`.

---

## `AlmediaLinkSDK`

Namespace: `AlmediaLink` · static class

The single entry point for host code. All methods and events are static; the SDK is a process-wide singleton in practice.

### Properties

#### `static string Version { get; }`

The current SDK semantic version, e.g. `"1.0.1"`.

#### `static AlmediaStatus CurrentStatus { get; }`

The SDK's current lifecycle status. Reads `AlmediaStatus.NotInitialized` until the native bridge reports its first terminal status, then tracks every transition.

Read this from components that mount after `Initialize` has already completed - by then the first `OnStatusChanged` may have already fired, and `CurrentStatus` is the way to recover the latest value before subscribing for further transitions.

```csharp
if (AlmediaLinkSDK.CurrentStatus != AlmediaStatus.NotInitialized)
    UpdateUi(AlmediaLinkSDK.CurrentStatus);

AlmediaLinkSDK.OnStatusChanged += UpdateUi;
```

---

### Methods

#### `static void Initialize(AlmediaLinkConfig config)`

Boots the SDK. Merges `config` with `AlmediaLinkSettings`, instantiates the native bridge for the current platform, sends an `init` request to the backend, and starts the status lifecycle that resolves into a terminal `AlmediaStatus` (delivered via `OnStatusChanged` and reflected in `CurrentStatus`).

**Validation.** If the resolved integration key for the current platform is missing or empty (no value supplied by `config` and no value in the settings asset), the method synchronously raises `OnErrorOccurred` with `AlmediaErrorCode.InvalidConfiguration` and returns without initializing the bridge.

**Failure modes.** `Initialize` never throws. All initialization failures are surfaced through `OnErrorOccurred`:

| Cause                                                          | `AlmediaError.Code`    | Notes                                                                                  |
|----------------------------------------------------------------|------------------------|----------------------------------------------------------------------------------------|
| Missing / empty integration key for the current platform       | `InvalidConfiguration` | Fired synchronously from `Initialize`.                                                 |
| Running on a platform other than iOS or Android (player build) | `InvalidConfiguration` | Fired synchronously from `Initialize`. The SDK stays inactive for the app lifetime.    |
| Native bridge construction fails for any other reason          | `Unexpected`           | Fired synchronously from `Initialize`. `AlmediaError.Message` includes the root cause. |
| Backend / network errors raised after the bridge is up         | See `AlmediaErrorCode` | Fired asynchronously from the native layer.                                            |

Subscribe to `OnErrorOccurred` **before** calling `Initialize` to receive synchronous errors.

**Idempotency.** Calling `Initialize` again with the **same effective configuration** is a no-op: `CurrentStatus` is preserved and the native layer is not contacted again (the call is logged at `Info` level). This holds whether the first call is still in flight or has already reached a terminal status. Calling `Initialize` with a **different** configuration tears down the current session and re-initializes - `CurrentStatus` returns to `NotInitialized` for the transition and then resolves again, driven by the native layer. The native bridge instance is reused across calls; it is never recreated.

**OS support floor.** On devices below **iOS 16** or **Android API 25**, initialization completes normally but resolves to `AlmediaStatus.NotAvailable`, and every further SDK call is inert - nothing crosses into native code. A single warning stating the device OS version and the required one is reported through [`OnLog`](#static-event-actionalmedialoglevel-string-onlog); it is the only diagnostic emitted below the floor, so wire `OnLog` into your logging pipeline if you need to distinguish this case from the other `NotAvailable` causes in the field. On devices at or above the floor, behavior is unchanged. Editor play mode is unaffected.

#### `static void StartLinking(PlacementType placement = PlacementType.Popup)`

Opens the native account-linking flow. The `placement` value is analytics metadata - it tells the backend which UI surface drove the call.

**In the bundled integration, host code does not call this directly.** The recommended flow is: `LinkButton` prefab opens the `LinkPopup`, and the popup's CTA button calls `StartLinking` for you. Call this method yourself only when you're replacing the bundled popup with custom UI and need to trigger the linking flow from your own button.

A no-op (with a warning logged via `OnLog`) if `Initialize` has not been called, or if the SDK has not yet reached a terminal status (the first `OnStatusChanged` has not fired).

#### `static void ShowRewardHub()`

Opens the reward progression screen in a webview. When the screen appears, [`OnScreenPresented`](#static-event-actionalmediascreen-onscreenpresented) fires with `AlmediaScreen.RewardHub`, and its dismissal is reported through [`OnScreenDismissed`](#static-event-actionalmediascreen-inappscreenresult-onscreendismissed).

A no-op (with a warning) until the SDK is ready. Safe to call in any state: native opens the screen only for a linked player with a live progression URL, and otherwise no-ops with the reason logged through `OnLog` - a no-op call fires neither lifecycle event.

#### `static void ShowOffer()`

Opens the offer screen in a webview. When the screen appears, [`OnScreenPresented`](#static-event-actionalmediascreen-onscreenpresented) fires with `AlmediaScreen.Offer`, and its dismissal is reported through [`OnScreenDismissed`](#static-event-actionalmediascreen-inappscreenresult-onscreendismissed).

A no-op (with a warning) until the SDK is ready. Safe to call in any state: native opens the screen only for a linked player with a live offer URL, and otherwise no-ops with the reason logged through `OnLog` - a no-op call fires neither lifecycle event. Unlike the progression URL, the offer URL can appear and disappear between syncs, so a call that opened the screen earlier in the session may later be a no-op.

#### `static void Engage()`

Context-aware entry point. Forwards to native, which routes on the player's state:

| Player state | Native action |
|---|---|
| `Eligible` | starts linking |
| `Linked` | opens the reward hub |
| anything else | no-op + log with the reason |

**All routing lives in native - Unity does not switch on status.** `Engage()` forwards the call whenever the SDK is ready, regardless of the current status; the state-based decision (including the no-op case) happens on the native side and is reported through `OnLog`. A no-op (with a warning) until the SDK is ready.

Use this as the single action behind a custom host button that should "do the right thing" for the player.

#### `static void FetchNotifications()`

Issues a one-shot request for the latest reward notifications. Result arrives via `OnNotificationsReceived` if there are any. A no-op (with a warning) until the SDK is ready - i.e. before the first `OnStatusChanged` has fired.

**Resets the polling clock when polling is active.** Calling `FetchNotifications` while the polling loop is running fires the immediate request and reschedules the next polling tick to `now + interval` instead of letting it fire at the originally planned time. This dedups the immediate request against a near-due scheduled tick - hosts that call `FetchNotifications` on user action (e.g. opening a notifications drawer) won't see a near-duplicate poll arrive moments later.

#### `static void StartNotificationPolling()`

Resumes the notification polling loop. The loop auto-starts once the player's status reaches `Linked`, so host code only needs this method to recover from an explicit `StopNotificationPolling()` (for example, after a cinematic or full-screen ad). The interval is configured via `AlmediaLinkConfig.NotificationsPollingIntervalSec` / `AlmediaLinkSettings.NotificationPollIntervalSeconds` (default 30 s, minimum 5 s); the native plugin pauses polling automatically when the app backgrounds and resumes on foreground. A no-op (with a warning) until the SDK is ready - i.e. before the first `OnStatusChanged` has fired.

**Idempotent.** Calling `StartNotificationPolling` while the loop is already running is a no-op - it does not double-schedule, does not shorten the interval, and does not trigger an immediate poll. To force an immediate fetch, call `FetchNotifications`.

#### `static void StopNotificationPolling()`

Stops the polling loop. Call this from cinematics, full-screen ads, or any UI state where notifications would intrude.

**Idempotent.** Calling `StopNotificationPolling` when the loop is not running is a no-op. Safe to call defensively before a scene where notifications should be suppressed.

---

### Events

All events are static. Subscribe before calling `Initialize` if you want to observe every transition. Components that mount later can recover the current state by reading `CurrentStatus` and then subscribing to `OnStatusChanged` for further transitions.

All events fire on the **Unity main thread**, so handlers can touch any Unity API (UI, `GetComponent`, `transform`, scene loading) directly. See [Threading](./integration-guide.md#threading) in the integration guide for the underlying mechanism.

#### `static event Action<AlmediaStatus> OnStatusChanged`

Fires on every status transition. The first emission carries the first terminal `AlmediaStatus` (any of `Eligible`, `Linked`, `NotAvailable`, `Blocked`, `Disabled`); subsequent emissions reflect later transitions. Use it both for one-shot setup that should happen once the SDK has resolved (gate on `status != NotInitialized` and your own guard flag) and for live UI that mirrors the current state. The bundled `LinkButtonController` subscribes to this event and shows or hides itself when status enters or leaves `Eligible`. `StartLinking`, `FetchNotifications`, and `StartNotificationPolling` called before this first emission are safe no-ops (a warning is logged), so a mistimed call is harmless rather than a crash.

#### `static event Action<string> OnLinkCompleted`

Fires when the player completes account linking flow. The argument is an ISO-8601 timestamp string from the backend, e.g. `"2026-05-13T14:23:01Z"`.

Does **not** fire for players who were already linked at `Initialize` time - they receive `AlmediaStatus.Linked` via `OnStatusChanged` instead. Use this event when you need to distinguish a fresh link from an already-linked session (e.g. celebration UX, one-time analytics).

#### `static event Action<List<AlmediaNotification>> OnNotificationsReceived`

Fires when a polling tick or `FetchNotifications()` returns one or more notifications. Empty results do not fire the event. Notifications are delivered in arrival order (oldest first).

#### `static event Action<AlmediaError> OnErrorOccurred`

Fires for both synchronous (local validation) and asynchronous (native, network) errors. See [`AlmediaErrorCode`](#almediaerrorcode) for the full set.

#### `static event Action<AlmediaScreen> OnScreenPresented`

Fires when an SDK screen ([`AlmediaScreen`](#almediascreen): the linking webview, the reward hub, or an offer) is now on top of the game - pause gameplay here. It fires at the moment the native container commits to presenting, **before** the page loads, so a slow network cannot delay the pause.

The event fires for every trigger that actually presents a screen: the API methods (`StartLinking`, `ShowRewardHub`, `ShowOffer`), the `LinkButton` prefab, and `Engage()` routing.

**Matched-pair guarantee.** Every screen that appears produces exactly one `OnScreenPresented` and, later, exactly one matching [`OnScreenDismissed`](#static-event-actionalmediascreen-inappscreenresult-onscreendismissed):

| Scenario | `OnScreenPresented` | `OnScreenDismissed` |
|---|---|---|
| A screen actually appears | exactly one | exactly one (later, matching) |
| Call opens nothing (wrong state, missing URL, a screen already open, `Engage()` no-op) | never | never |
| System-browser linking (web-browser strategy) | never | never - only the link callbacks fire |

#### `static event Action<AlmediaScreen, InAppScreenResult> OnScreenDismissed`

Fires when the screen reported by [`OnScreenPresented`](#static-event-actionalmediascreen-onscreenpresented) is gone - resume gameplay here. Exactly one per `OnScreenPresented` (see the matched-pair table above). The [`InAppScreenResult`](#inappscreenresult) argument reports how the screen was dismissed - `Completed` (closed by the web client), `Cancelled` (user closed it), or `Failed` (it could not load, with the detail in `Error`).

Native completes its sync-on-close **before** this fires, so state read in the handler (e.g. `CurrentStatus`) already reflects anything that happened inside the screen. For webview linking the pair fires in addition to the link callbacks, and `OnScreenDismissed` fires before the outcome callbacks (`OnLinkCompleted` / `OnErrorOccurred`).

#### `static event Action<AlmediaLogLevel, string> OnLog`

Fires for every log line emitted by the SDK, including forwarded logs from the native plugins. No subscribers means no console output - the SDK does not call `UnityEngine.Debug` directly. Wire this into your own logging pipeline as needed.

This event delegates internally to `AlmediaLink.AlmediaLog.OnLog`; subscribing/unsubscribing through `AlmediaLinkSDK.OnLog` is equivalent.

---

## `AlmediaLinkConfig`

Namespace: `AlmediaLink` · class

Mutable POCO you build at runtime and pass to `Initialize`. Code-supplied values override the corresponding `AlmediaLinkSettings` defaults. Nullable wrappers (`int?`, `bool?`) signal "fall back to settings" - set them only when you want a code-supplied value to override the asset.

### Fields

| Property                                           | Type      | Fallback chain                                  |
|----------------------------------------------------|-----------|-------------------------------------------------|
| `IosIntegrationKey`                                | `string`  | settings → none (required on iOS)               |
| `AndroidIntegrationKey`                            | `string`  | settings → none (required on Android)           |
| `Gaid`                                             | `string`  | none (config only)                              |
| `Asid`                                             | `string`  | none (config only)                              |
| `Oaid`                                             | `string`  | none (config only)                              |
| `Idfa`                                             | `string`  | none (config only)                              |
| `Idfv`                                             | `string`  | none (config only)                              |
| `AdjustDeviceId`                                   | `string`  | none (config only)                              |
| `AppsFlyerId`                                      | `string`  | none (config only)                              |
| `AccountId`                                        | `string`  | none (config only)                              |
| `NotificationsPollingIntervalSec`                  | `int?`    | settings (`NotificationPollIntervalSeconds`) → 30 |

**Platform selection.** During `Initialize`, the bridge picks `IosIntegrationKey` on iOS builds and `AndroidIntegrationKey` on Android builds. In the Editor, the SDK prefers iOS but falls back to Android if iOS is empty. Only the platform-relevant key is forwarded to the native layer.

**Empty strings vs null.** For the string identifiers (`Gaid`, `Idfa`, etc.), the SDK treats `null` and `""` interchangeably and forwards an empty string to native when neither config nor caller provides one. Pass `null` if you don't have the value.

**`Idfv` (iOS).** Identifier for Vendor. When omitted (`null` or `""`), the iOS native layer collects it automatically via `UIDevice.current.identifierForVendor`; a supplied value overrides it. Unlike `Idfa`, it is not gated behind ATT. iOS-only - ignored on Android. Requires the matching iOS native binary; older bundled plugins ignore the field.

**Android requires at least one device identifier.** On Android, native initialization succeeds only when at least one of `Gaid`, `Asid`, `Oaid`, `AdjustDeviceId`, or `AppsFlyerId` is non-empty. The C# layer does not pre-validate this - a config without any of them fails at the native layer. Identifiers that don't apply to the running platform are ignored, so they can be set unconditionally.

---

## `AlmediaLinkSettings`

Namespace: `AlmediaLink` · `ScriptableObject`

Edit-time configuration backed by `Assets/AlmediaLink/Resources/AlmediaLinkSettings.asset`. The asset is created automatically on first package import and is never overwritten by SDK upgrades.

Open the settings window via **Almedia → Settings** in the Unity menu bar.

### Properties

All properties are read-only from outside the asset (set via the Unity Inspector).

#### SDK Configuration

| Property                          | Type     | Default | Description |
|-----------------------------------|----------|---------|-------------|
| `IosIntegrationKey`               | `string` | empty   | Almedia-issued key for iOS. |
| `AndroidIntegrationKey`           | `string` | empty   | Almedia-issued key for Android. |
| `NotificationPollIntervalSeconds` | `int`    | 30      | Notification polling cadence. Min 5. |
| `EnableDefaultNotificationUI`     | `bool`   | true    | When false, SDK does not show its NotificationCard or ActivityOverlay; host receives `OnNotificationsReceived` and renders. |

#### Link Popup

| Property                  | Type     | Description |
|---------------------------|----------|-------------|
| `PopupTitle`              | `string` | Headline at top of the popup. |
| `Benefit1Title`           | `string` | Bold title of the first benefit bullet. |
| `Benefit1Description`     | `string` | Description below benefit 1. |
| `Benefit2Title`           | `string` | Bold title of the second benefit bullet. |
| `Benefit2Description`     | `string` | Description below benefit 2. |
| `CtaButtonText`           | `string` | Label on the primary call-to-action button. |
| `OverlayTitle`            | `string` | Title at the top of the Activity Overlay (notification list). |
| `PopupBackgroundColor`    | `Color`  | Popup card background. |
| `CtaButtonColor`          | `Color`  | CTA button fill. |
| `CtaButtonTextColor`      | `Color`  | CTA button text color. |

#### Notifications

| Property                       | Type    | Description |
|--------------------------------|---------|-------------|
| `NotificationBackgroundColor`  | `Color` | Notification card background. |

#### Prefab Overrides

| Property                  | Type                          | Description |
|---------------------------|-------------------------------|-------------|
| `LinkPopupOverride`       | `LinkPopupController`         | Prefab Variant to instantiate instead of the SDK default. |
| `NotificationCardOverride`| `NotificationCardController`  | Prefab Variant for the notification card. |
| `ActivityOverlayOverride` | `ActivityOverlayController`   | Prefab Variant for the notification list overlay. |

---

## Models

### `AlmediaStatus`

Namespace: `AlmediaLink.Models` · enum

Mirrors the SDK's runtime state. Delivered to `OnStatusChanged` on every transition and always available as a snapshot via `CurrentStatus`.

| Value             | Meaning |
|-------------------|---------|
| `NotInitialized`  | Default value before init finishes. Never persists. |
| `Eligible`        | Service is available and the player has not linked yet. Linking UI should be visible. |
| `Linked`          | Player has linked their Freecash account. Reward notifications can flow. |
| `NotAvailable`    | Service is not available for this user (region, eligibility). Linking UI should hide. |
| `Blocked`         | User is blocked by the backend (abuse, fraud). Linking UI should hide. |
| `Disabled`        | Integration is killswitched at the backend level. SDK runs in a no-op mode until the backend re-enables it. |

Terminal values: `Eligible`, `Linked`, `NotAvailable`, `Blocked`, `Disabled`. `NotInitialized` is the pre-init default and is never re-entered after the first terminal transition.

---

### `AlmediaError`

Namespace: `AlmediaLink.Models` · class

Immutable error payload passed to `OnErrorOccurred`.

```csharp
public AlmediaErrorCode Code    { get; }
public string           Message { get; }
```

| Property   | Notes |
|------------|-------|
| `Code`     | See [`AlmediaErrorCode`](#almediaerrorcode). Unrecognized native codes map to `Unknown`. |
| `Message`  | Human-readable string. May contain native-side detail; safe to log, but do not rely on its format for branching. |

---

### `AlmediaErrorCode`

Namespace: `AlmediaLink.Models` · enum

| Value                  | Source           | Typical cause |
|------------------------|------------------|---------------|
| `Unknown`              | Native           | Unrecognized native code (version mismatch). |
| `InvalidConfiguration` | Local or native  | Missing integration key, malformed config. Local errors fire synchronously from `Initialize`. |
| `NetworkFailure`       | Native           | Native HTTP request did not complete (offline, DNS, timeout). |
| `ServerError`          | Native           | Backend returned 5xx. |
| `RateLimited`          | Native           | Backend returned 429. |
| `Disabled`             | Native           | Integration is killswitched. SDK will not function until re-enabled. |
| `LinkingFailed`        | Native           | The linking flow ended without success (user closed, backend rejected). |
| `InvalidState`         | Native           | Method called in a state it does not support. |
| `Unexpected`           | Native           | Catch-all for uncaught native exceptions. |

---

### `AlmediaNotification`

Namespace: `AlmediaLink.Models` · class

Immutable reward-notification payload delivered via `OnNotificationsReceived`.

```csharp
public string           Id         { get; }
public string           Title      { get; }
public string           Message    { get; }
public string           Timestamp  { get; }
public DateTimeOffset?  ReceivedAt { get; }
public string           Type       { get; }
```

| Property     | Notes |
|--------------|-------|
| `Id`         | Stable, unique per notification. Use for dedup and tracking. |
| `Title`      | Short headline. |
| `Message`    | Body. May be multi-line; render in a wrapping text element. |
| `Timestamp`  | Raw ISO 8601 string as delivered by the backend (e.g. `"2025-03-15T10:30:00Z"`). Preserved for logging / re-serialization; prefer `ReceivedAt` for display logic. |
| `ReceivedAt` | Parsed UTC `DateTimeOffset?`, or `null` when `Timestamp` is empty or unparseable. Always normalized to UTC (offset 0), so `DateTimeOffset.UtcNow - notification.ReceivedAt.Value` gives the elapsed time directly. |
| `Type`       | Opaque category string (e.g. `"reward"`, `"status"`). Used by `NotificationIconMap` to pick an icon. |

---

### `PlacementType`

Namespace: `AlmediaLink.Models` · enum

Analytics tag passed to `AlmediaLinkSDK.StartLinking(placement)`.

| Value        | Native string  | Suggested context |
|--------------|----------------|-------------------|
| `Popup`      | `"popup"`      | Default. Linking initiated from a modal popup. |
| `RewardHub`  | `"reward_hub"` | Linking initiated from a rewards / store UI. |
| `Banner`     | `"banner"`     | Linking initiated from an in-game banner ad slot. |

Behaviorally identical - the difference is reflected only in analytics.

---

### `AlmediaScreen`

Namespace: `AlmediaLink.Models` · enum

Identifies which SDK screen a lifecycle event is about. Delivered to [`OnScreenPresented`](#static-event-actionalmediascreen-onscreenpresented) and [`OnScreenDismissed`](#static-event-actionalmediascreen-inappscreenresult-onscreendismissed).

| Value       | Native string  | Meaning |
|-------------|----------------|---------|
| `Linking`   | `"linking"`    | The account-linking flow shown in an in-app webview. System-browser linking is not reported. |
| `RewardHub` | `"reward_hub"` | The reward progression screen ([`ShowRewardHub()`](#static-void-showrewardhub) or `Engage()` routing). |
| `Offer`     | `"offer"`      | The offer screen ([`ShowOffer()`](#static-void-showoffer)). |

An unrecognized screen string from native is logged as a warning and the callback is dropped - the events never fire with a garbage value.

---

### `InAppScreenResult`

Namespace: `AlmediaLink.Models` · class

How an in-app screen was dismissed. Delivered to [`OnScreenDismissed`](#static-event-actionalmediascreen-inappscreenresult-onscreendismissed).

```csharp
public InAppScreenResultType Type  { get; }
public AlmediaError          Error { get; }
```

| Property | Notes |
|----------|-------|
| `Type`   | See [`InAppScreenResultType`](#inappscreenresulttype). |
| `Error`  | Populated only when `Type` is `Failed`; `null` for every other outcome. |

---

### `InAppScreenResultType`

Namespace: `AlmediaLink.Models` · enum

| Value       | Native string | Meaning |
|-------------|---------------|---------|
| `Completed` | `"completed"` | The web client closed the screen through its JS bridge. |
| `Cancelled` | `"cancelled"` | The player closed the screen with the close/back button. |
| `Failed`    | `"failed"`    | The screen could not load. `InAppScreenResult.Error` carries the detail. |

An unrecognized value from native is treated as `Cancelled` and logged.

---

### `AlmediaLogLevel`

Namespace: `AlmediaLink` · enum

Severity levels for `OnLog`, ordered low → high.

```csharp
public enum AlmediaLogLevel
{
    Verbose = 0,
    Debug   = 1,
    Info    = 2,
    Warning = 3,
    Error   = 4
}
```

---

## Editor testing

Editor-only test hook for driving the SDK into any state (status, error, notifications, native log) from a play-mode test or an editor host script. See [Driving non-happy paths](./integration-guide.md#driving-non-happy-paths) in the integration guide for the narrative.

The whole API lives in `AlmediaLink.Editor.Testing` (assembly `AlmediaLink.Editor`, `includePlatforms:["Editor"]`). Host references must be wrapped in `#if UNITY_EDITOR` so they don't reach iOS/Android player compilation.

---

### `AlmediaLinkEditorMock`

Namespace: `AlmediaLink.Editor.Testing` · static class

The first call to any method here puts the underlying editor mock into **manual mode** for the rest of the play session: every subsequent `Initialize` / `StartLinking` / `FetchNotifications` becomes a no-op so canned coroutines cannot race against test emissions, and any already-scheduled simulate coroutine is cancelled on the flip. Manual mode resets on domain reload.

Every method **throws `InvalidOperationException`** if called before `AlmediaLinkSDK.Initialize` - the throw is deliberate so test ordering bugs surface at test-author time instead of as silent no-ops.

#### `static void EmitStatus(AlmediaStatus status)`

Delivers a status transition. `AlmediaLinkSDK.CurrentStatus` and `OnStatusChanged` reflect it on the same call - no `yield return` required before reading.

#### `static void EmitError(AlmediaErrorCode code, string message)`

Fires `OnErrorOccurred` with the given code and message. Use this to exercise error-handling UI under every entry in [`AlmediaErrorCode`](#almediaerrorcode).

#### `static void EmitLinkCompleted()`

Fires `OnLinkCompleted` with the current UTC timestamp. Use it to test "celebrate a fresh link" UX in isolation from the linking flow itself.

#### `static void EmitNotifications(params MockNotification[] items)`

Fires `OnNotificationsReceived` with the supplied items, converted into the SDK's internal model. Pass no arguments for an empty batch (note: `AlmediaLinkSDK` short-circuits empty batches and does **not** raise `OnNotificationsReceived` - useful for verifying the "no new rewards" branch on the SDK side).

#### `static void EmitScreenPresented(AlmediaScreen screen)`

Fires `OnScreenPresented` for the given screen, without opening a real webview. Pair it with a later `EmitScreenDismissed` for the same screen to reproduce native's matched-pair contract in a test.

#### `static void EmitScreenDismissed(AlmediaScreen screen, InAppScreenResultType result, AlmediaErrorCode errorCode = AlmediaErrorCode.Unknown, string errorMessage = null)`

Fires `OnScreenDismissed` for the given screen with the given result. `errorCode` and `errorMessage` apply only to [`InAppScreenResultType.Failed`](#inappscreenresulttype) and are ignored for the completed/cancelled outcomes.

#### `static void EmitNativeLog(AlmediaLogLevel level, string message)`

Delivers a forwarded log line through the same path the iOS/Android plugins use. Subscribers of `AlmediaLinkSDK.OnLog` receive it as if it had come from the native side.

#### `static void CancelPending()`

Stops any pending auto-simulate coroutine. Mostly relevant for tests that want to assert "no callback fires" after `Initialize` - call this immediately after `AlmediaLinkSDK.Initialize` and the scheduled `Eligible` transition never arrives. The first call to any other `Emit*` method calls this internally as part of the manual-mode flip.

---

### `MockNotification`

Namespace: `AlmediaLink.Editor.Testing` · readonly struct

Public test-facing notification shape. Field-named (vs. positional tuple) so calls stay readable as the protocol evolves.

```csharp
public readonly struct MockNotification
{
    public readonly string Id;
    public readonly string Title;
    public readonly string Message;
    public readonly string Type;
    public readonly string Timestamp;

    public MockNotification(string id, string title, string message, string type, string timestamp = null);
}
```

A null `timestamp` defaults to `DateTime.UtcNow.ToString("o")` (ISO-8601), matching the format the backend emits.

---

## UI components

All UI controllers live in `AlmediaLink.UI`. The SDK ships ready-to-drop prefabs in `Packages/com.almedia.link/Runtime/Resources/Prefabs/`; host code never needs to instantiate or call into the controllers directly. Customize each by editing its prefab or by creating a Prefab Variant and assigning it in **Almedia → Settings → Prefab Overrides**.

---

### `LinkButtonController`

The component on the `LinkButton` prefabs. Two-state: visible while `Eligible` (a linking entry - tapping opens the `LinkPopup`) and while `Linked` (a rewards entry - tapping opens the reward hub via `ShowRewardHub()`); hidden in every other status. Each state is a self-contained subtree in the prefab (`Eligible` / `Linked`, each pairing a content object with its own button and background), so its art and press colours are authored in the prefab and the controller only toggles which one shows.

Drop one of the four `LinkButton` variants (`LinkButtonA`-`LinkButtonD`) into your Canvas - no code needed. To reproduce the behaviour on a button of your own, `AlmediaLinkSDK.Engage()` is the one-call equivalent (it links directly rather than opening the popup - see [`Engage()`](#static-void-engage)).

---

### `LinkPopupController`

The component on the `LinkPopup` prefab. Renders the modal account-linking popup; the CTA button triggers `AlmediaLinkSDK.StartLinking()`, and the popup self-dismisses on close or when linking completes.

Customize via Prefab Variant assigned in **Almedia → Settings → Prefab Overrides → Link Popup**. Text and colors are also directly editable in **Almedia → Settings → Link Popup Text**.

---

### `NotificationCardController`

The component on the `NotificationCard` prefab. Slides up from the bottom when notifications arrive, auto-dismisses after a few seconds, and opens the `ActivityOverlay` if multiple notifications stack.

Customize via Prefab Variant assigned in **Almedia → Settings → Prefab Overrides → Notification Card**. The dismiss delay and bottom padding are inspector-tunable on the prefab.

---

### `ActivityOverlayController`

The component on the `ActivityOverlay` prefab. Full-screen notification list, opened by tapping a stacked `NotificationCard`.

Customize via Prefab Variant assigned in **Almedia → Settings → Prefab Overrides → Activity Overlay**.

---

### `NotificationIconMap`

`ScriptableObject` that maps `AlmediaNotification.Type` strings to sprites. Customize by editing the asset at `Assets/AlmediaLink/Resources/NotificationIconMap.asset` in the Inspector - add an entry for each notification type your backend emits, set a default icon for unmapped types.

If you turn off `EnableDefaultNotificationUI` and render notifications yourself, the icon map is no longer consulted.

---

## Prefabs

All shipping prefabs live under `Packages/com.almedia.link/Runtime/Resources/Prefabs/`. They can be:

- Dropped directly into a scene Canvas (`LinkButtonA` to `LinkButtonD`).
- Instantiated by the SDK from Resources at runtime (the rest).
- Used as the base for Prefab Variants assigned in **Almedia → Settings → Prefab Overrides**.

| Prefab                    | Root component               |
|---------------------------|------------------------------|
| `LinkButtonA.prefab`      | `LinkButtonController`       |
| `LinkButtonB.prefab`      | `LinkButtonController`       |
| `LinkButtonC.prefab`      | `LinkButtonController`       |
| `LinkButtonD.prefab`      | `LinkButtonController`       |
| `LinkPopup.prefab`        | `LinkPopupController`        |
| `NotificationCard.prefab` | `NotificationCardController` |
| `ActivityOverlay.prefab`  | `ActivityOverlayController`  |

---

## Editor menu items

| Menu                                  | What it does |
|---------------------------------------|--------------|
| **Almedia → Settings**                | Opens the `AlmediaLinkSettingsEditor` window. Creates `Assets/AlmediaLink/Resources/AlmediaLinkSettings.asset` from the package default if missing. |

Android dependencies are provided automatically by the `AlmediaLink.androidlib` subproject (`Packages/com.almedia.link/Plugins/Android/AlmediaLink.androidlib/`). EDM4U-enabled projects additionally resolve `AlmediaLinkDependencies.xml`; Gradle dedupes by Maven coordinate. The iOS post-build hook (`AlmediaLinkBuildPostProcessor`) runs automatically as part of `BuildTarget.iOS` builds and exposes no menu item.

---

## Assembly definitions

| Asmdef                          | Includes platforms       | Auto-referenced | Notes |
|---------------------------------|---------------------------|:---------------:|-------|
| `AlmediaLink.asmdef`            | Any                       | yes             | `includePlatforms` is empty, so the runtime assembly compiles on every player target. Platform support is gated at runtime by native bridge selection: only iOS and Android are supported, and other targets raise `OnErrorOccurred` with `InvalidConfiguration` at `Initialize`. Root namespace: `AlmediaLink`. |
| `AlmediaLink.Editor.asmdef`     | Editor                    | yes             | Editor-only tooling. Root namespace: `AlmediaLink.Editor`. References the runtime asmdef. |

If you use Assembly Definition References yourself, add a reference to `AlmediaLink` (and only `AlmediaLink`) - `AlmediaLink.Editor` is strictly an editor concern.
