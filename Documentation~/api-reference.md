# Almedia SDK - API Reference

Complete reference for the Almedia SDK for Unity. For task-driven guidance (install, configure, customize), see the [integration guide](./integration-guide.md).

The SDK's entry point is `Almedia`, in the `AlmediaSDK` namespace. Existing integrations can keep using the `AlmediaLink` API of releases before 1.3.0. The [AlmediaLink API reference](./almedialink-api-reference.md) documents it, and the [migration guide](./migration-guide.md) shows how to move code to `Almedia`.

The SDK is versioned. The current release is reflected in `Almedia.Version` and in `Packages/com.almedia.link/package.json`.

---

## Contents

- [Namespaces](#namespaces)
- [`Almedia`](#almedia) - public static entry point
- [`AlmediaConfig`](#almediaconfig) - runtime configuration
- [`AlmediaLinkSettings`](#almedialinksettings) - ScriptableObject configuration
- Models
  - [`AlmediaStatus`](#almediastatus)
  - [`AlmediaNotAvailableReason`](#almedianotavailablereason)
  - [`AlmediaError`](#almediaerror)
  - [`AlmediaErrorCode`](#almediaerrorcode)
  - [`AlmediaNotification`](#almedianotification)
  - [`AlmediaInGameRewardGrant`](#almediaingamerewardgrant)
  - [`AlmediaInGameReward`](#almediaingamereward)
  - [`AlmediaProgress`](#almediaprogress)
  - [`AlmediaTask`](#almediatask)
  - [`AlmediaTaskKind`](#almediataskkind)
  - [`AlmediaTaskProgress`](#almediataskprogress)
  - [`AlmediaCompletedTask`](#almediacompletedtask)
  - [`AlmediaRewardPoints`](#almediarewardpoints)
  - [`AlmediaMoney`](#almediamoney)
  - [`AlmediaTaskCompletion`](#almediataskcompletion)
  - [`AlmediaBalanceChange`](#almediabalancechange)
  - [`AlmediaPlacementType`](#almediaplacementtype)
  - [`AlmediaScreen`](#almediascreen)
  - [`AlmediaFeature`](#almediafeature)
  - [`AlmediaInAppScreenResult`](#almediainappscreenresult)
  - [`AlmediaLogLevel`](#almedialoglevel)
- [Editor testing](#editor-testing)
  - [`AlmediaEditorMock`](#almediaeditormock)
- UI components
  - [`LinkButtonController`](#linkbuttoncontroller)
  - [`LinkPopupController`](#linkpopupcontroller)
  - [`NotificationCardController`](#notificationcardcontroller)
  - [`ActivityOverlayController`](#activityoverlaycontroller)
- [Prefabs](#prefabs)
- [Editor menu items](#editor-menu-items)
- [Assembly definitions](#assembly-definitions)

---

## Namespaces

| Namespace            | Contents |
|----------------------|----------|
| `AlmediaSDK`         | The static `Almedia` entry point, `AlmediaConfig`, and the status, error, notification, progress, placement, feature, in-app screen result and log level types. |
| `AlmediaSDK.Editor.Testing` | `AlmediaEditorMock` - editor-only test hook for driving non-happy paths. Excluded from player builds. |
| `AlmediaLink`        | `AlmediaLinkSettings`, and the `AlmediaLink` API documented in the [AlmediaLink API reference](./almedialink-api-reference.md). |
| `AlmediaLink.UI`     | UI controllers. |
| `AlmediaLink.Editor` | Editor-only tooling (Settings window, iOS post-build hook). Excluded from player builds. |

Most host code only needs `using AlmediaSDK;`.

---

## `Almedia`

Namespace: `AlmediaSDK` · static class

The single entry point for host code. All methods and events are static; the SDK is a process-wide singleton in practice.

### Properties

#### `static string Version { get; }`

The current SDK semantic version, e.g. `"1.3.0"`.

#### `static AlmediaStatus Status { get; }`

The player's current status. See [`AlmediaStatus`](#almediastatus). Never `null`. Reads `AlmediaStatus.NotInitialized` until the native bridge reports the first status, and again while `Initialize` applies a different configuration.

Read this from components that mount after `Initialize` has already completed - by then the first `OnStatusChanged` may have already fired, and `Status` is the way to recover the latest value before subscribing for further changes.

```csharp
Refresh(Almedia.Status);
Almedia.OnStatusChanged += Refresh;
```

Updated **before** `OnStatusChanged` fires, so a handler can read it directly. The flags of a `Linked` status can change at any time. Do **not** cache them in your own field - see [Availability comes and goes](./integration-guide.md#availability-comes-and-goes).

#### `static AlmediaProgress Progress { get; }`

The latest progress snapshot, or `null` when the SDK has none. See [`AlmediaProgress`](#almediaprogress). It is `null` before the first snapshot arrives, after the native SDK clears the snapshot with its message stream token (an account change, or a token the server refused), and after `Initialize` with a different configuration. The SDK keeps it in memory only.

The SDK updates it before [`OnProgressUpdated`](#static-event-actionalmediaprogress-onprogressupdated) fires. A scene that loads after the event can read the accessor, then subscribe for updates:

```csharp
void OnEnable()
{
    Render(Almedia.Progress);                 // null until the first snapshot
    Almedia.OnProgressUpdated += Render;
}
```

A snapshot is complete. Render the snapshot you get. Do not keep state from a previous snapshot.

---

### Methods

#### `static void Initialize(AlmediaConfig config)`

Boots the SDK. Merges `config` with `AlmediaLinkSettings`, instantiates the native bridge for the current platform, sends an `init` request to the backend, and starts the status lifecycle that resolves into the first status (delivered via `OnStatusChanged` and reflected in `Status`). A `null` config reads as an empty `AlmediaConfig`.

With **Auto-Initialize From Prefabs** enabled in [`AlmediaLinkSettings`](#almedialinksettings), a `LinkButton` prefab calls this by itself with the settings values when nothing has called it by the end of the button's first frame. A call of your own always takes precedence.

**Validation.** If the resolved integration key for the current platform is missing or empty (no value supplied by `config` and no value in the settings asset), the method synchronously raises `OnErrorOccurred` with `AlmediaErrorCode.InvalidConfiguration` and returns without initializing the bridge.

**Failure modes.** `Initialize` never throws. All initialization failures are surfaced through `OnErrorOccurred`:

| Cause                                                          | `AlmediaError.Code`    | Notes                                                                                  |
|----------------------------------------------------------------|------------------------|----------------------------------------------------------------------------------------|
| Missing / empty integration key for the current platform       | `InvalidConfiguration` | Fired synchronously from `Initialize`.                                                 |
| Running on a platform other than iOS or Android (player build) | `InvalidConfiguration` | Fired synchronously from `Initialize`. The SDK stays inactive for the app lifetime.    |
| Native bridge construction fails for any other reason          | `Unexpected`           | Fired synchronously from `Initialize`. `AlmediaError.Message` includes the root cause. |
| Backend / network errors raised after the bridge is up         | See `AlmediaErrorCode` | Fired asynchronously from the native layer.                                            |

Subscribe to `OnErrorOccurred` **before** calling `Initialize` to receive synchronous errors.

**Retry from the error handler.** A call to `Initialize` from an `OnErrorOccurred` handler that `Initialize` raised is ignored and logged at `Warning` via `OnLog`. Retry from a later frame instead.

**Idempotency.** Calling `Initialize` again with the **same effective configuration** is a no-op: `Status` is preserved and the native layer is not contacted again (the call is logged at `Info` level). This holds whether the first call is still in flight or has already resolved. Calling `Initialize` with a **different** configuration tears down the current session and re-initializes. `Status` returns to `NotInitialized`, `OnStatusChanged` reports that when the status had already resolved, and the status then resolves again, driven by the native layer. The native bridge instance is reused across calls; it is never recreated.

**OS support floor.** On devices below **iOS 16** or **Android API 25**, initialization completes normally but resolves to `AlmediaStatus.NotAvailable`, and every further SDK call is inert - nothing crosses into native code. A single warning stating the device OS version and the required one is reported through [`OnLog`](#static-event-actionalmedialoglevel-string-onlog); it is the only diagnostic emitted below the floor, so wire `OnLog` into your logging pipeline if you need to distinguish this case from the other `NotAvailable` causes in the field. On devices at or above the floor, behavior is unchanged. Editor play mode is unaffected.

#### `static void ShowLink()`

Shows the link popup to a player who can link. The popup's CTA button starts linking. The popup reports the same analytics as when the `LinkButton` prefab opens it. This is the recommended way to start linking from your own button.

The popup comes from **Almedia → Settings → Default UI Prefabs → Link Popup** (see [`AlmediaLinkSettings`](#default-ui-prefabs)). With that slot empty, `ShowLink()` shows the popup assigned to the deprecated `LinkPopupOverride`. With neither assigned, it starts linking directly and logs a warning through `OnLog` once.

A no-op (with a warning) until the SDK is ready. After that, it does nothing, with a log, unless `Status` is `AlmediaStatus.Eligible`. It also does nothing while a link popup is already open. A popup that fails to open, for example a Prefab Variant with a missing reference, is removed and reported through `OnLog` at `Error`.

#### `static void StartLinking()`

Opens the native account-linking flow directly, without the popup, and reports the `Popup` placement. The same as `StartLinking(AlmediaPlacementType.Popup)`.

#### `static void StartLinking(AlmediaPlacementType placement)`

Opens the native account-linking flow directly, without the popup. The `placement` value is analytics metadata - it tells the backend which UI surface drove the call. See [`AlmediaPlacementType`](#almediaplacementtype). A `null` placement reads as `Popup`.

**In the bundled integration, host code does not call this directly.** The `LinkPopup`'s CTA button starts linking for you, whether the popup came from the `LinkButton` prefab or from [`ShowLink()`](#static-void-showlink). For your own popup design, assign a Prefab Variant of `LinkPopup` to the **Link Popup** slot and call `ShowLink()`. Call `StartLinking` yourself only from the CTA of a popup built without `LinkPopupController`.

A no-op (with a warning logged via `OnLog`) if `Initialize` has not been called, or if the SDK has not yet resolved its first status (the first `OnStatusChanged` has not fired).

#### `static void ShowRewardHub()`

Opens the reward progression screen in a webview. When the screen appears, [`OnScreenPresented`](#static-event-actionalmediascreen-onscreenpresented) fires with `AlmediaScreen.RewardHub`, and its dismissal is reported through [`OnScreenDismissed`](#static-event-actionalmediascreen-almediainappscreenresult-onscreendismissed).

A no-op (with a warning) until the SDK is ready. Safe to call in any state: native opens the screen only for a linked player for whom the reward hub is currently available, and otherwise does nothing with the reason logged through `OnLog` - a no-op call fires neither lifecycle event. Reward hub availability can change mid-session, so a call that worked earlier may later be a no-op.

Check `CanShowRewardHub` on a [`Linked`](#almediastatus) status before calling, and gate your entry point on it rather than on a flag you cache yourself. Availability can still change between rendering a button and the tap.

#### `static void ShowOffer()`

Opens the offer screen in a webview. When the screen appears, [`OnScreenPresented`](#static-event-actionalmediascreen-onscreenpresented) fires with `AlmediaScreen.Offer`, and its dismissal is reported through [`OnScreenDismissed`](#static-event-actionalmediascreen-almediainappscreenresult-onscreendismissed).

A no-op (with a warning) until the SDK is ready. Safe to call in any state: native opens the screen only for a linked player for whom an offer is currently available, and otherwise does nothing with the reason logged through `OnLog` - a no-op call fires neither lifecycle event. Offer availability can change mid-session, so a call that worked earlier may later be a no-op.

Check `CanShowOffer` on a [`Linked`](#almediastatus) status before calling, and gate your entry point on it rather than on a flag you cache yourself. Availability can still change between rendering a button and the tap; such a call does nothing and logs.

#### `static void FetchNotifications()`

Issues a one-shot request for the latest reward notifications. Result arrives via `OnNotificationsReceived` if there are any. A no-op (with a warning) until the SDK is ready - i.e. before the first `OnStatusChanged` has fired. Offline, the request fails quietly: a `Warning` log, no `OnErrorOccurred`, and nothing delivered until the next poll or call finds the network again. The backend can hold the request for up to a minute until a message arrives.

**Resets the polling clock when polling is active.** Calling `FetchNotifications` while the polling loop is running fires the immediate request and reschedules the next polling tick to `now + interval` instead of letting it fire at the originally planned time. This dedups the immediate request against a near-due scheduled tick - hosts that call `FetchNotifications` on user action (e.g. opening a notifications drawer) won't see a near-duplicate poll arrive moments later.

#### `static void StartNotificationPolling()`

Resumes the notification polling loop. The loop auto-starts once the player's status reaches `Linked`, so host code only needs this method to recover from an explicit `StopNotificationPolling()` (for example, after a cinematic or full-screen ad). The backend can set the interval. When it sets none, the interval comes from `AlmediaConfig.NotificationsPollingIntervalSec` / `AlmediaLinkSettings.NotificationPollIntervalSeconds` (default 30 s, minimum 5 s). The native plugin pauses polling automatically when the app backgrounds and resumes on foreground. A no-op (with a warning) until the SDK is ready - i.e. before the first `OnStatusChanged` has fired.

**Idempotent.** Calling `StartNotificationPolling` while the loop is already running is a no-op - it does not double-schedule, does not shorten the interval, and does not trigger an immediate poll. To force an immediate fetch, call `FetchNotifications`.

#### `static void StopNotificationPolling()`

Stops the polling loop. Call this from cinematics, full-screen ads, or any UI state where notifications would intrude.

**Idempotent.** Calling `StopNotificationPolling` when the loop is not running is a no-op. Safe to call defensively before a scene where notifications should be suppressed.

---

### Events

All events are static. Subscribe before calling `Initialize` if you want to observe every change. Components that mount later can recover the current state by reading `Status` and then subscribing to `OnStatusChanged` for further changes.

All events fire on the **Unity main thread**, so handlers can touch any Unity API (UI, `GetComponent`, `transform`, scene loading) directly. See [Threading](./integration-guide.md#threading) in the integration guide for the underlying mechanism.

A handler that throws is reported through `OnLog` at `Error`. It does not stop the other subscribers or later SDK callbacks.

#### `static event Action<AlmediaStatus> OnStatusChanged`

Fires with the new status whenever [`Status`](#static-almediastatus-status--get-) changes. `Status` already holds the new value. The first emission after `Initialize` carries the first resolved status: `Eligible`, `Linked`, `NotAvailable`, `Blocked` or `Disabled`. Later emissions report every change:

| What changed in the native update | `OnStatusChanged` |
|---|---|
| The status | fires |
| `CanShowRewardHub` or `CanShowOffer`, while the player stays linked | fires |
| The reason of `NotAvailable` | fires, unless both the old and the new reason read `Unknown` |
| Nothing (identical payload) | - |

It also fires with `NotInitialized` when `Initialize` runs with a different configuration after the status had resolved.

Use it both for one-shot setup that should happen once the SDK has resolved (skip `NotInitialized` and use your own guard flag) and for live UI that mirrors the current status. `StartLinking`, `FetchNotifications`, and `StartNotificationPolling` called before the first status are safe no-ops (a warning is logged), so a mistimed call is harmless rather than a crash.

#### `static event Action<string> OnLinkCompleted`

Fires when the player completes account linking flow. The argument is an ISO-8601 timestamp string from the backend, e.g. `"2026-05-13T14:23:01Z"`.

Does **not** fire for players who were already linked at `Initialize` time - they receive a `Linked` status via `OnStatusChanged` instead. Use this event when you need to distinguish a fresh link from an already-linked session (e.g. celebration UX, one-time analytics).

#### `static event Action<List<AlmediaNotification>> OnNotificationsReceived`

Fires when a polling tick or `FetchNotifications()` returns one or more notifications. Empty results do not fire the event. Notifications are delivered in arrival order (oldest first).

#### `static event Action<AlmediaInGameRewardGrant> OnInGameRewardGrantRequested`

Fires when the backend instructs the game to grant in-game rewards. Credit the player and celebrate here.

One event per grant. A grant is a bundle of one or more rewards granted together ([`AlmediaInGameRewardGrant.Rewards`](#almediaingamerewardgrant)): "250 gems and 3 spins for finishing the chapter" arrives as one event with one id and deserves one celebration. Three separate grants arriving in the same polling tick raise three events.

**Delivery is at-least-once.** The same grant can arrive more than once. The server-to-server reward postback remains the authoritative record of what was granted; treat this event as the real-time convenience, not the ledger. Each grant has a unique [`Id`](#almediaingamerewardgrant) that a redelivery repeats, so deduplicate on it when a repeat credit matters to your economy. A short memory of recent ids suffices:

```csharp
private readonly HashSet<string> _credited = new HashSet<string>();

Almedia.OnInGameRewardGrantRequested += grant =>
{
    if (!_credited.Add(grant.Id)) return;   // redelivered; already credited
    foreach (var reward in grant.Rewards)
        Wallet.Credit(reward.Code, reward.Amount);
};
```

#### `static event Action<AlmediaProgress> OnProgressUpdated`

Fires when [`Progress`](#static-almediaprogress-progress--get-) changes. The argument is the new snapshot, or `null` when the native SDK cleared the snapshot with its message stream token. The accessor already holds the new value. A handler can read the argument or the accessor. Hide the progress UI on `null`.

The SDK applies only a strictly newer snapshot. The new snapshot replaces the previous one completely. This includes [`Username`](#almediaprogress): a newer snapshot without a username clears the previous username.

#### `static event Action<AlmediaTaskCompletion> OnTaskCompleted`

Fires when the server reports a completed task. The argument carries the message id and the completion, with the reward the completion paid. See [`AlmediaTaskCompletion`](#almediataskcompletion).

The event is historical and best-effort. The task is not always in the current snapshot. The event does not update the snapshot. The same completion can arrive more than once, also after the SDK resets its stream token. The server never reuses an `Id`. Deduplicate on it:

```csharp
private readonly HashSet<string> _seen = new HashSet<string>();

Almedia.OnTaskCompleted += completion =>
{
    if (!_seen.Add(completion.Id)) return;   // replayed; already celebrated
    Celebrate(completion.Task.Task.Title, completion.Task.ActualReward);
};
```

#### `static event Action<AlmediaBalanceChange> OnBalanceChanged`

Fires when the server reports a balance change. The argument carries the balance after the change and the signed change. See [`AlmediaBalanceChange`](#almediabalancechange). The change is negative for a reversal.

The event is historical and best-effort, like `OnTaskCompleted`. The balance can differ from the current snapshot. The event does not update the snapshot. A replay carries the same `Id`.

#### `static event Action<AlmediaError> OnErrorOccurred`

Fires for both synchronous (local validation) and asynchronous (native, network) errors. See [`AlmediaErrorCode`](#almediaerrorcode) for the full set.

Connectivity loss in work the SDK runs on its own - notification polling, event delivery, status refreshes, a `FetchNotifications` that finds no network - does not fire it. Those are logged at `Warning` through [`OnLog`](#static-event-actionalmedialoglevel-string-onlog) and retried by the SDK. A 4xx response, an undecodable response, or a failure of work the game asked for still does.

#### `static event Action<AlmediaScreen> OnScreenPresented`

Fires when an SDK screen ([`AlmediaScreen`](#almediascreen): the linking webview, the reward hub, or an offer) is now on top of the game - pause gameplay here. It fires at the moment the native container commits to presenting, **before** the page loads, so a slow network cannot delay the pause.

The event fires for every trigger that actually presents a screen: the API methods (`StartLinking`, `ShowRewardHub`, `ShowOffer`), and the `LinkPopup`'s CTA after the `LinkButton` prefab or `ShowLink()` opened it. The popup itself is not an SDK screen and fires neither event.

**Matched-pair guarantee.** Every screen that appears produces exactly one `OnScreenPresented` and, later, exactly one matching [`OnScreenDismissed`](#static-event-actionalmediascreen-almediainappscreenresult-onscreendismissed):

| Scenario | `OnScreenPresented` | `OnScreenDismissed` |
|---|---|---|
| A screen actually appears | exactly one | exactly one (later, matching) |
| Call opens nothing (wrong state, missing URL, a screen already open) | never | never |
| System-browser linking (web-browser strategy) | never | never - only the link callbacks fire |

#### `static event Action<AlmediaScreen, AlmediaInAppScreenResult> OnScreenDismissed`

Fires when the screen reported by [`OnScreenPresented`](#static-event-actionalmediascreen-onscreenpresented) is gone - resume gameplay here. Exactly one per `OnScreenPresented` (see the matched-pair table above). The [`AlmediaInAppScreenResult`](#almediainappscreenresult) argument reports how the screen was dismissed - `Completed` (closed by the web client), `Cancelled` (user closed it), or `Failed` (it could not load, with the detail in `Error`).

Native completes its sync-on-close **before** this fires, so state read in the handler (e.g. `Status`) already reflects anything that happened inside the screen. For webview linking the pair fires in addition to the link callbacks, and `OnScreenDismissed` fires before the outcome callbacks (`OnLinkCompleted` / `OnErrorOccurred`).

#### `static event Action<AlmediaLogLevel, string> OnLog`

Fires for every log line emitted by the SDK, including forwarded logs from the native plugins. No subscribers means no console output - the SDK does not call `UnityEngine.Debug` directly. Wire this into your own logging pipeline as needed. See [`AlmediaLogLevel`](#almedialoglevel) for the levels.

---

## `AlmediaConfig`

Namespace: `AlmediaSDK` · class

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
| `TrafficSource`                                    | `string`  | none (config only)                              |
| `Meta1` / `Meta2` / `Meta3` / `Meta4`              | `string`  | none (config only)                              |
| `NotificationsPollingIntervalSec`                  | `int?`    | settings (`NotificationPollIntervalSeconds`) → 30 |
| `DisabledFeatures`                                 | `HashSet<AlmediaFeature>` | settings ∪ config (never null; collection initializer) |

**Polling interval.** The backend can set the polling interval. `NotificationsPollingIntervalSec` applies when it sets none.

**Platform selection.** During `Initialize`, the bridge picks `IosIntegrationKey` on iOS builds and `AndroidIntegrationKey` on Android builds. In the Editor, the SDK prefers iOS but falls back to Android if iOS is empty. Only the platform-relevant key is forwarded to the native layer.

**Empty strings vs null.** For the string identifiers (`Gaid`, `Idfa`, etc.), the SDK treats `null` and `""` interchangeably and forwards an empty string to native when neither config nor caller provides one. Pass `null` if you don't have the value.

**`Idfv` (iOS).** Identifier for Vendor. When omitted (`null` or `""`), the iOS native layer collects it automatically via `UIDevice.current.identifierForVendor`; a supplied value overrides it. Unlike `Idfa`, it is not gated behind ATT. iOS-only - ignored on Android. Requires the matching iOS native binary; older bundled plugins ignore the field.

**Android device identifiers.** The native SDK collects identifiers on its own where the platform allows, and mints its own install-scoped ALID, so initialization succeeds even when nothing can be collected and no identifier is passed - though where Almedia has not enabled it for the integration, that ALID is never stored and the SDK reports not-available. Supplying `Gaid`, `Asid`, `Oaid`, `AdjustDeviceId`, or `AppsFlyerId` improves attribution. The C# layer does not pre-validate identifiers. Identifiers that don't apply to the running platform are ignored, so they can be set unconditionally.

**`AdjustDeviceId` / `AppsFlyerId` (both platforms).** When not supplied, the native layer on iOS and Android reads each one from what the Adjust or AppsFlyer SDK bundled in the app has stored, once the backend enables that identifier for the integration; a supplied value always wins. Requires the matching native binaries; older bundled plugins send only the supplied values.

**`TrafficSource`.** Where the user was originally acquired, sent as `sub3` on the linking URL. Pass your MMP's media source verbatim, e.g. `applovin_int` or `googleadwords_int`. The same rules as `Meta1`-`Meta4` below apply.

**`Meta1`-`Meta4`.** Optional values that the SDK appends to the linking URL. Reward hub and offer URLs never carry them. The SDK does not read or modify them. Use them to receive your own data back in S2S reports.

- Each value is limited to 250 UTF-8 bytes. A multi-byte character counts as more than one byte.
- A longer value is left off the link, and a warning at initialization names it. Initialization and linking still proceed.
- An empty value is treated as unset.
- Your value replaces any parameter of the same name already on the link.
- Changing a value re-initializes the session. Set them once, at startup.

**`DisabledFeatures`.** The parts of the Link experience this game hides from the current player - see [`AlmediaFeature`](#almediafeature). Getter-only and never null; declare it with a collection initializer: `new AlmediaConfig { DisabledFeatures = { AlmediaFeature.Offer } }`. Joined with the settings asset's **Disabled Features** as a union: the asset is the floor, code adds and never removes. The set is sent once at `Initialize`; a different set on a later call re-initializes.

---

## `AlmediaLinkSettings`

Namespace: `AlmediaLink` · `ScriptableObject`

Edit-time configuration backed by `Assets/Almedia/Resources/AlmediaSettings.asset`. The asset is created automatically on first package import and is never overwritten by SDK upgrades. `Almedia` and the `AlmediaLink` API both read it.

Open the settings window via **Almedia → Settings** in the Unity menu bar.

### Properties

All properties are read-only from outside the asset (set via the Unity Inspector).

#### SDK Configuration

| Property                          | Type     | Default | Description                                                                                                                                                                                    |
|-----------------------------------|----------|---------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `IosIntegrationKey`               | `string` | empty   | Almedia-issued key for iOS.                                                                                                                                                                    |
| `AndroidIntegrationKey`           | `string` | empty   | Almedia-issued key for Android.                                                                                                                                                                |
| `NotificationPollIntervalSeconds` | `int`    | 30      | Notification polling cadence when the backend sets none. Min 5.                                                                                                                                                           |
| `EnableDefaultNotificationUI`     | `bool`   | true    | When false, SDK does not show its NotificationCard or ActivityOverlay; host receives `OnNotificationsReceived` and renders.                                                                    |
| Auto-Initialize From Prefabs      | `bool`   | true    | When on, a `LinkButton` prefab initializes the SDK with the settings values unless the host has already called `Initialize`. Settings toggle only, with no public accessor. |

#### Disabled Features

One toggle per [`AlmediaFeature`](#almediafeature), drawn from the SDK's feature list so a new feature appears without an editor update. Stored as wire names. The ticked set is the floor for every player; `AlmediaConfig.DisabledFeatures` adds to it for the current player. Settings toggles only, with no public accessor.

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

#### Default UI Prefabs

| Property                  | Type                          | Description |
|---------------------------|-------------------------------|-------------|
| Link Popup                | `LinkPopupController`         | The popup [`ShowLink()`](#static-void-showlink) presents. The settings asset the SDK creates on first import has the bundled prefab here. An existing one keeps the slot empty after the upgrade. A popup assigned here ships in every build. Settings slot only, with no public accessor. |
| `NotificationCardPrefab`  | `NotificationCardController`  | The notification card the SDK instantiates when notifications arrive. Pre-filled with the bundled prefab; assign a Prefab Variant to customize. |
| `ActivityOverlayPrefab`   | `ActivityOverlayController`   | The notification list overlay opened from a stacked card. Same semantics. |

The two notification slots track **Enable Default Notification UI**: disabling the toggle clears slots that point at the bundled prefabs so they (and their art) stay out of the build, and re-enabling restores the bundled defaults into empty slots. A variant you assigned yourself survives the toggle in both directions. A `LinkButton` does not use the Link Popup slot. It opens the popup assigned on the button itself.

Deprecated members (`LinkPopupOverride`, `NotificationCardOverride`, `ActivityOverlayOverride`) carry their guidance in their `[Obsolete]` messages. One behavioral note: a popup assigned in `LinkPopupOverride` wins over the button's popup reference. `ShowLink()` also shows it while the **Link Popup** slot is empty.

---

## Models

### `AlmediaStatus`

Namespace: `AlmediaSDK` · abstract class

The player's status. [`Status`](#static-almediastatus-status--get-) holds it and [`OnStatusChanged`](#static-event-actionalmediastatus-onstatuschanged) delivers every change. Each status is a nested sealed class that carries its own data.

```csharp
public abstract class AlmediaStatus : IEquatable<AlmediaStatus>
{
    public sealed class NotInitialized : AlmediaStatus { public NotInitialized(); }
    public sealed class Eligible       : AlmediaStatus { public Eligible(); }

    public sealed class Linked : AlmediaStatus
    {
        public Linked(bool canShowRewardHub, bool canShowOffer);
        public bool CanShowRewardHub { get; }
        public bool CanShowOffer     { get; }
    }

    public sealed class NotAvailable : AlmediaStatus
    {
        public NotAvailable(string rawReason);
        public AlmediaNotAvailableReason Reason    { get; }
        public string                    RawReason { get; }
    }

    public sealed class Blocked  : AlmediaStatus { public Blocked(); }
    public sealed class Disabled : AlmediaStatus { public Disabled(); }
}
```

| Status            | Meaning |
|-------------------|---------|
| `NotInitialized`  | Before `Initialize` resolves the first status, and while it applies a different configuration. |
| `Eligible`        | Service is available and the player has not linked yet. Linking UI should be visible. [`ShowLink()`](#static-void-showlink) presents the link popup. |
| `Linked`          | Player has linked their Freecash account. Reward notifications can flow. |
| `NotAvailable`    | Service is not available for this user (region, eligibility, holdout). Linking UI should hide. `Reason` says why. |
| `Blocked`         | User is blocked by the backend (abuse, fraud). Linking UI should hide. Unlike `NotAvailable`, retrying does not help. |
| `Disabled`        | Integration is killswitched at the backend level. SDK runs in a no-op mode until the backend re-enables it. |

| Member                    | Notes |
|---------------------------|-------|
| `Linked.CanShowRewardHub` | `ShowRewardHub()` would present a screen right now. |
| `Linked.CanShowOffer`     | `ShowOffer()` would present a screen right now. |
| `NotAvailable.Reason`     | Why the service is not available. See [`AlmediaNotAvailableReason`](#almedianotavailablereason). Never `null`. |
| `NotAvailable.RawReason`  | The reason exactly as the server sent it. Empty when the server sent none. `Reason` is parsed from it. |

Test a status with a type pattern. Keep a `default` branch in a `switch`: a later version can add statuses.

```csharp
switch (Almedia.Status)
{
    case AlmediaStatus.Eligible:
        ShowLinkButton();
        break;
    case AlmediaStatus.Linked { CanShowRewardHub: true }:
        ShowRewardHubButton();
        break;
    default:
        HideEntryPoints();
        break;
}
```

**The flags are live.** A `Linked` player can have either flag `false` - a reward hub the backend has withdrawn, or an offer that has not appeared yet - and the flags can flip between syncs while the player stays linked. Each change arrives as a new `Linked` status through `OnStatusChanged`.

Statuses compare by value. Two `Linked` statuses with the same flags are equal, and so are two `NotAvailable` statuses with the same `RawReason`. `ToString()` includes the data of `Linked` and `NotAvailable`, for example `Linked(canShowRewardHub: True, canShowOffer: False)`, and returns the class name for the other statuses.

Every constructor is public, so a test can build any status and deliver it with [`AlmediaEditorMock.EmitStatus`](#static-void-emitstatusalmediastatus-status).

---

### `AlmediaNotAvailableReason`

Namespace: `AlmediaSDK` · sealed class

Why the status is `NotAvailable`. Read it from `Reason` on [`AlmediaStatus.NotAvailable`](#almediastatus). Only the static members below exist. Compare them with `==`. `ToString()` returns the member name.

| Member     | Meaning |
|------------|---------|
| `Unknown`  | The backend sent no reason, or one this SDK version does not recognize. Not an error - read it as "not available, cause unspecified". |
| `Holdout`  | The player is in the holdout (control) group and is deliberately excluded from the Link experience. |
| `Disabled` | The game declared `Linking` in its [disabled features](#almediafeature). |

A reason added by the backend later reads as `Unknown` in an existing build, and `RawReason` keeps the string the server sent. Handle `Unknown` as a real case.

The reason never changes what the SDK does - the entry point hides on `NotAvailable` whatever the reason. Use it for analytics, support diagnostics, and messaging:

```csharp
Almedia.OnStatusChanged += status =>
{
    if (status is AlmediaStatus.NotAvailable notAvailable
        && notAvailable.Reason == AlmediaNotAvailableReason.Holdout)
        Analytics.Track("link_holdout");
};
```

---

### `AlmediaError`

Namespace: `AlmediaSDK` · sealed class

Immutable error payload passed to `OnErrorOccurred`.

```csharp
public AlmediaErrorCode Code    { get; }
public string           Message { get; }

public AlmediaError(AlmediaErrorCode code, string message);
```

| Property   | Notes |
|------------|-------|
| `Code`     | See [`AlmediaErrorCode`](#almediaerrorcode). Unrecognized native codes map to `Unknown`. Never `null`: the constructor reads a `null` code as `Unknown`. |
| `Message`  | Human-readable string. May contain native-side detail; safe to log, but do not rely on its format for branching. |

Errors compare by value: two errors with the same code and message are equal.

---

### `AlmediaErrorCode`

Namespace: `AlmediaSDK` · sealed class

Only the static members below exist. Compare them with `==`. `ToString()` returns the member name.

| Member                 | Source           | Typical cause |
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

Namespace: `AlmediaSDK` · class

Immutable reward-notification payload delivered via `OnNotificationsReceived`.

```csharp
public string           Id         { get; }
public string           Title      { get; }
public string           Message    { get; }
public string           Timestamp  { get; }
public DateTimeOffset?  ReceivedAt { get; }
public string           Display    { get; }
public string           IconUrl    { get; }

[Obsolete] public string Type      { get; }   // alias of Display

public AlmediaNotification(string id, string title, string message, string timestamp, string display, string iconUrl);
[Obsolete] public AlmediaNotification(string id, string title, string message, string timestamp, string type);
```

| Property     | Notes |
|--------------|-------|
| `Id`         | Stable, unique per notification. Use for dedup and tracking. |
| `Title`      | Short headline. |
| `Message`    | Body. May be multi-line; render in a wrapping text element. |
| `Timestamp`  | Raw ISO 8601 string as delivered by the backend (e.g. `"2025-03-15T10:30:00Z"`). Preserved for logging / re-serialization; prefer `ReceivedAt` for display logic. |
| `ReceivedAt` | Parsed UTC `DateTimeOffset?`, or `null` when `Timestamp` is empty or unparseable. Always normalized to UTC (offset 0), so `DateTimeOffset.UtcNow - notification.ReceivedAt.Value` gives the elapsed time directly. |
| `Display`    | Presentation hint: `"popup"` or `"tray"`. Open set - treat values you do not recognize as `"popup"`. Never null or empty. |
| `IconUrl`    | Absolute URL of the notification's icon, or `null`. The bundled UI does not render it; the notification prefab supplies the row icon. In custom UI, load it yourself and fall back to your own art when null or unreachable. |
| `Type`       | **Obsolete.** Alias of `Display`. Until 1.2.0 this carried a free-form category (`"reward"`, `"status"`); those values no longer exist on the wire, so comparisons against them never match. |

The constructor reads an empty `display` as `"popup"` and an empty `iconUrl` as `null`, and parses `ReceivedAt` from `timestamp`. Tests pass notifications built this way to [`AlmediaEditorMock.EmitNotifications`](#static-void-emitnotificationsparams-almedianotification-notifications).

---

### `AlmediaInGameRewardGrant`

Namespace: `AlmediaSDK` · class

An in-game-reward grant delivered via [`OnInGameRewardGrantRequested`](#static-event-actionalmediaingamerewardgrant-oningamerewardgrantrequested). Every reward in `Rewards` was granted together and belongs to one celebration.

```csharp
public string                             Id         { get; }
public string                             Timestamp  { get; }
public DateTimeOffset?                    ReceivedAt { get; }
public IReadOnlyList<AlmediaInGameReward> Rewards    { get; }

public AlmediaInGameRewardGrant(string id, string timestamp, params AlmediaInGameReward[] rewards);
```

| Property     | Notes |
|--------------|-------|
| `Id`         | Server-issued and unique per grant; a redelivery repeats it. The dedup key for hosts that want exactly-once crediting. |
| `Timestamp`  | Raw ISO 8601 string of when the backend issued the grant. |
| `ReceivedAt` | Parsed UTC `DateTimeOffset?`, or `null` when `Timestamp` is empty or unparseable. |
| `Rewards`    | The reward line items. At least one entry. |

The constructor throws `ArgumentException` when `rewards` is empty.

---

### `AlmediaInGameReward`

Namespace: `AlmediaSDK` · class

One reward line item of an [`AlmediaInGameRewardGrant`](#almediaingamerewardgrant).

```csharp
public double Amount { get; }
public string Code   { get; }

public AlmediaInGameReward(double amount, string code);
```

| Property   | Notes |
|------------|-------|
| `Amount`   | Amount to credit. Whole today, but the contract permits fractions - do not truncate to an integer. |
| `Code`     | Reward code agreed with Almedia (e.g. `"gems"`, `"spins"`). Treat unrecognized codes as a no-op rather than an error. |

---

### `AlmediaProgress`

Namespace: `AlmediaSDK` · sealed class

A snapshot of the player's progress in this game. [`Progress`](#static-almediaprogress-progress--get-) exposes it and [`OnProgressUpdated`](#static-event-actionalmediaprogress-onprogressupdated) delivers it. A snapshot is never partial. A newer snapshot replaces the previous one. The SDK does not merge snapshots.

```csharp
public string                              Id        { get; }
public string                              Timestamp { get; }
public DateTimeOffset?                     BuiltAt   { get; }
public string                              Username  { get; }
public AlmediaRewardPoints                 Balance   { get; }
public AlmediaRewardPoints                 Earned    { get; }
public IReadOnlyList<AlmediaTask>          Pending   { get; }
public IReadOnlyList<AlmediaCompletedTask> Completed { get; }
public IReadOnlyList<AlmediaTask>          Expired   { get; }

public AlmediaProgress(string id, string timestamp, string username, AlmediaRewardPoints balance, AlmediaRewardPoints earned,
    IReadOnlyList<AlmediaTask> pending, IReadOnlyList<AlmediaCompletedTask> completed, IReadOnlyList<AlmediaTask> expired);
```

| Property    | Notes |
|-------------|-------|
| `Id`        | Opaque and unique for each snapshot. It has no order. Compare `BuiltAt` instead. |
| `Timestamp` | Raw ISO 8601 string. The time when the server built the snapshot. |
| `BuiltAt`   | Parsed UTC `DateTimeOffset?`. `null` when `Timestamp` is empty or not valid. |
| `Username`  | The player's username. `null` when the server sent none. A newer snapshot without a username clears the previous value. |
| `Balance`   | The player's total balance. |
| `Earned`    | The total the player earned in this game. |
| `Pending`   | Tasks the player can still do in this game. Not more than one entry for each task. |
| `Completed` | One entry for each completion, newest first. The server limits the number of entries. |
| `Expired`   | Tasks the player can no longer complete. The server can limit the number of entries. |

The constructor copies the lists, reads a `null` list as empty and reads an empty username as `null`.

---

### `AlmediaTask`

Namespace: `AlmediaSDK` · sealed class

A task the player can do in this game to earn a reward.

```csharp
public string              Id                     { get; }
public string              Kind                   { get; }
public string              Title                  { get; }
public AlmediaRewardPoints Reward                 { get; }
public AlmediaTaskProgress Progress               { get; }
public string              RewardDropsAtTimestamp { get; }
public DateTimeOffset?     RewardDropsAt          { get; }

public AlmediaTask(string id, string kind, string title, AlmediaRewardPoints reward,
    AlmediaTaskProgress progress = null, string rewardDropsAtTimestamp = null);
```

| Property                 | Notes |
|--------------------------|-------|
| `Id`                     | Opaque and stable across snapshots and reward decreases. It identifies the task, not one completion. |
| `Kind`                   | Groups similar tasks. One of [`AlmediaTaskKind`](#almediataskkind). The set is open. Treat an unknown value as `AlmediaTaskKind.Main`. |
| `Title`                  | The title to show the player. |
| `Reward`                 | The reward for one completion. |
| `Progress`               | The player's progress, or `null` for a yes-or-no task. |
| `RewardDropsAtTimestamp` | Raw ISO 8601 string. The time of the next `Reward` decrease, or `null` when none is scheduled. |
| `RewardDropsAt`          | Parsed UTC `DateTimeOffset?`. `null` when `RewardDropsAtTimestamp` is `null` or not valid. |

The constructor reads an empty `rewardDropsAtTimestamp` as `null`.

---

### `AlmediaTaskKind`

Namespace: `AlmediaSDK` · static class

The known values of [`AlmediaTask.Kind`](#almediatask), as `const string` members. The server can add more.

| Member                 | Value                    | Notes |
|------------------------|--------------------------|-------|
| `Main`                 | `"main"`                 | A regular task. |
| `PurchaseBonus`        | `"purchaseBonus"`        | Pays when the player makes a purchase. |
| `Burning`              | `"burning"`              | Its reward decreases on a schedule. See `AlmediaTask.RewardDropsAt`. |
| `BurningPurchaseBonus` | `"burningPurchaseBonus"` | A purchase bonus whose reward decreases on a schedule. |
| `TimeLimited`          | `"timeLimited"`          | Must be completed before a deadline. |
| `Daily`                | `"daily"`                | Can be completed once a day. |
| `Play`                 | `"play"`                 | Pays for time played. |
| `Unlimited`            | `"unlimited"`            | Can be completed any number of times. |

---

### `AlmediaTaskProgress`

Namespace: `AlmediaSDK` · sealed class

The progress of an [`AlmediaTask`](#almediatask).

```csharp
public long Value  { get; }
public long Target { get; }

public AlmediaTaskProgress(long value, long target);
```

| Property | Notes |
|----------|-------|
| `Value`  | The value reached so far. |
| `Target` | The value needed to complete the task. |

---

### `AlmediaCompletedTask`

Namespace: `AlmediaSDK` · sealed class

One completion of an [`AlmediaTask`](#almediatask). A player can complete the same task more than once.

```csharp
public AlmediaTask         Task         { get; }
public string              Timestamp    { get; }
public DateTimeOffset?     CompletedAt  { get; }
public AlmediaRewardPoints ActualReward { get; }

public AlmediaCompletedTask(AlmediaTask task, string timestamp, AlmediaRewardPoints actualReward);
```

| Property       | Notes |
|----------------|-------|
| `Task`         | The completed task. |
| `Timestamp`    | Raw ISO 8601 string. The time of the completion. |
| `CompletedAt`  | Parsed UTC `DateTimeOffset?`. `null` when `Timestamp` is empty or not valid. |
| `ActualReward` | The reward this completion paid. It differs from `Task.Reward` when the reward decreased before completion, or when Freecash adjusted the credit. |

---

### `AlmediaRewardPoints`

Namespace: `AlmediaSDK` · sealed class

A balance or a reward in coins, with the display values the server supplied.

```csharp
public long         Coins            { get; }
public AlmediaMoney InPlayerCurrency { get; }
public AlmediaMoney InUsd            { get; }

public AlmediaRewardPoints(long coins, AlmediaMoney inPlayerCurrency, AlmediaMoney inUsd);
```

| Property           | Notes |
|--------------------|-------|
| `Coins`            | The canonical amount. The money values are for display only. The value is signed. A reversal is negative. |
| `InPlayerCurrency` | The amount in the currency the server selected for the player. If this SDK version does not support that currency, the SDK substitutes the `InUsd` value and the `"USD"` code. |
| `InUsd`            | The amount in USD. |

Each SDK version supports a closed set of player currencies. You can write your formatting against this list. This release supports `USD`, `EUR`, `GBP`, `CAD`, `AUD`, `PLN`, `CHF`, `KRW`, `JPY` and `SEK`. A new market needs a new SDK release.

---

### `AlmediaMoney`

Namespace: `AlmediaSDK` · sealed class

A coin amount in real money, for display only.

```csharp
public decimal Amount   { get; }
public string  Currency { get; }

public AlmediaMoney(decimal amount, string currency);
```

| Property   | Notes |
|------------|-------|
| `Amount`   | An exact `decimal`. The SDK parses it from the wire string, independent of the device locale. The SDK never uses a `float` or a `double`. The scale from the server stays: `12.50` stays `12.50`. |
| `Currency` | The ISO 4217 code, in upper case. |

Every progress type compares by value, so `progress.Completed.Contains(completion.Task)` works and two snapshots with the same content are equal. `AlmediaMoney` compares numerically: `12.50` equals `12.5`.

The SDK does not format money. Format `Amount` with your own symbols, decimals and fonts:

```csharp
var eur = new CultureInfo("de-DE");
label.text = money.Amount.ToString("N2", eur) + " " + money.Currency;   // "12,50 EUR"
```

---

### `AlmediaTaskCompletion`

Namespace: `AlmediaSDK` · sealed class

A task the server reports as completed. [`OnTaskCompleted`](#static-event-actionalmediataskcompletion-ontaskcompleted) delivers it.

```csharp
public string               Id   { get; }
public AlmediaCompletedTask Task { get; }

public AlmediaTaskCompletion(string id, AlmediaCompletedTask task);
```

| Property | Notes |
|----------|-------|
| `Id`     | The server-generated message id. A replay carries the same id. Deduplicate on it. |
| `Task`   | The completion, with `ActualReward`. The task is not always in the current snapshot. |

---

### `AlmediaBalanceChange`

Namespace: `AlmediaSDK` · sealed class

A balance change the server reports. [`OnBalanceChanged`](#static-event-actionalmediabalancechange-onbalancechanged) delivers it.

```csharp
public string              Id      { get; }
public AlmediaRewardPoints Balance { get; }
public AlmediaRewardPoints Change  { get; }

public AlmediaBalanceChange(string id, AlmediaRewardPoints balance, AlmediaRewardPoints change);
```

| Property  | Notes |
|-----------|-------|
| `Id`      | The server-generated message id. A replay carries the same id. Deduplicate on it. |
| `Balance` | The balance after the change. It can differ from the current snapshot. |
| `Change`  | The signed change. A reversal is negative. |

---

### `AlmediaPlacementType`

Namespace: `AlmediaSDK` · sealed class

Analytics tag passed to [`StartLinking(placement)`](#static-void-startlinkingalmediaplacementtype-placement). Only the static members below exist. Compare them with `==`. `ToString()` returns the member name.

| Member       | Native string  | Suggested context |
|--------------|----------------|-------------------|
| `Popup`      | `"popup"`      | Default. Linking initiated from a modal popup. |
| `RewardHub`  | `"reward_hub"` | Linking initiated from a rewards / store UI. |
| `Banner`     | `"banner"`     | Linking initiated from an in-game banner ad slot. |

Behaviorally identical - the difference is reflected only in analytics.

---

### `AlmediaScreen`

Namespace: `AlmediaSDK` · sealed class

Identifies which SDK screen a lifecycle event is about. Delivered to [`OnScreenPresented`](#static-event-actionalmediascreen-onscreenpresented) and [`OnScreenDismissed`](#static-event-actionalmediascreen-almediainappscreenresult-onscreendismissed). Only the static members below exist. Compare them with `==`. `ToString()` returns the member name.

| Member      | Native string  | Meaning |
|-------------|----------------|---------|
| `Linking`   | `"linking"`    | The account-linking flow shown in an in-app webview. System-browser linking is not reported. |
| `RewardHub` | `"reward_hub"` | The reward progression screen ([`ShowRewardHub()`](#static-void-showrewardhub)). |
| `Offer`     | `"offer"`      | The offer screen ([`ShowOffer()`](#static-void-showoffer)). |

An unrecognized screen string from native is logged as a warning and the callback is dropped - the events never fire with a garbage value.

---

### `AlmediaFeature`

Namespace: `AlmediaSDK` · sealed class

A part of the Link experience a game can declare it hides from its players, through [`AlmediaConfig.DisabledFeatures`](#almediaconfig) or the settings asset. The backend enforces the declaration; the SDK only reports it and reacts to the status that comes back. Only the static members below exist; there is no constructor and no cast, so an undeclared value cannot be built. A `null` entry in `DisabledFeatures` is ignored. `ToString()` returns the wire name.

| Member          | Wire name         | Disabled means |
|-----------------|-------------------|----------------|
| `Linking`       | `"linking"`       | A not-linked player reads `NotAvailable` with reason [`Disabled`](#almedianotavailablereason); `StartLinking()` fails through the existing status check. A linked player is untouched. |
| `RewardHub`     | `"reward_hub"`    | `CanShowRewardHub` is false on a `Linked` status; `ShowRewardHub()` is a no-op; the bundled button hides. |
| `Offer`         | `"offer"`         | `CanShowOffer` is false on a `Linked` status; `ShowOffer()` is a no-op. |
| `Notifications` | `"notifications"` | No Almedia notification reaches the game, in the SDK's UI or through `OnNotificationsReceived`. Reward grants are unaffected. |

Linking and the two screens are independent: hiding the entry point does not un-link anyone or close the hub to a player who linked earlier.

---

### `AlmediaInAppScreenResult`

Namespace: `AlmediaSDK` · abstract class

How an in-app screen was dismissed. Delivered to [`OnScreenDismissed`](#static-event-actionalmediascreen-almediainappscreenresult-onscreendismissed). Each result is a nested sealed class.

```csharp
public abstract class AlmediaInAppScreenResult : IEquatable<AlmediaInAppScreenResult>
{
    public sealed class Completed : AlmediaInAppScreenResult { public Completed(); }
    public sealed class Cancelled : AlmediaInAppScreenResult { public Cancelled(); }

    public sealed class Failed : AlmediaInAppScreenResult
    {
        public Failed(AlmediaError error);
        public AlmediaError Error { get; }
    }
}
```

| Result      | Native string | Meaning |
|-------------|---------------|---------|
| `Completed` | `"completed"` | The web client closed the screen through its JS bridge. |
| `Cancelled` | `"cancelled"` | The player closed the screen with the close/back button. |
| `Failed`    | `"failed"`    | The screen could not load. `Error` carries the detail. |

An unrecognized value from native is treated as `Cancelled` and logged. Test a result with a type pattern. Keep a `default` branch in a `switch`: a later version can add results.

```csharp
switch (result)
{
    case AlmediaInAppScreenResult.Completed _: break;
    case AlmediaInAppScreenResult.Cancelled _: break;
    case AlmediaInAppScreenResult.Failed failed: Debug.LogWarning(failed.Error.Message); break;
    default: break;
}
```

Results compare by value. `Failed` built with a `null` error carries an `Unknown` error.

---

### `AlmediaLogLevel`

Namespace: `AlmediaSDK` · sealed class

Severity levels for `OnLog`, ordered low → high: `Verbose`, `Debug`, `Info`, `Warning`, `Error`. Only these static members exist. Compare them with `==`, and order them with `<`, `<=`, `>` and `>=`. `ToString()` returns the member name.

```csharp
Almedia.OnLog += (level, message) =>
{
    if (level >= AlmediaLogLevel.Warning)
        Debug.LogWarning(message);
};
```

---

## Editor testing

Editor-only test hook for driving the SDK into any state (status, error, notifications, in-game reward grants, progress, native log) from a play-mode test or an editor host script. See [Driving non-happy paths](./integration-guide.md#driving-non-happy-paths) in the integration guide for the narrative.

The API lives in `AlmediaSDK.Editor.Testing` (assembly `AlmediaLink.Editor`, `includePlatforms:["Editor"]`). Host references must be wrapped in `#if UNITY_EDITOR` so they don't reach iOS/Android player compilation.

---

### `AlmediaEditorMock`

Namespace: `AlmediaSDK.Editor.Testing` · static class

The first call to any method here puts the underlying editor mock into **manual mode** for the rest of the play session: every subsequent `Initialize` / `StartLinking` / `FetchNotifications` becomes a no-op so canned coroutines cannot race against test emissions, and any already-scheduled simulate coroutine is cancelled on the flip. Manual mode resets on domain reload.

Every method **throws `InvalidOperationException`** if called before `Almedia.Initialize` - the throw is deliberate so test ordering bugs surface at test-author time instead of as silent no-ops. A `null` status, code, screen, result, level, task, balance or change throws `ArgumentNullException`.

#### `static void EmitStatus(AlmediaStatus status)`

Delivers `status` as if native had reported it. `Status` equals it afterwards, and `OnStatusChanged` fires as it would for a native report - not at all when the status equals the current one. No `yield return` is needed before reading.

```csharp
// Holdout player.
AlmediaEditorMock.EmitStatus(new AlmediaStatus.NotAvailable("holdout"));

// Linking declared in DisabledFeatures - the bundled LinkButton hides.
AlmediaEditorMock.EmitStatus(new AlmediaStatus.NotAvailable("disabled"));

// Linked, but the reward hub is gone - the bundled LinkButton hides.
AlmediaEditorMock.EmitStatus(new AlmediaStatus.Linked(false, false));
```

#### `static void EmitError(AlmediaErrorCode code, string message)`

Fires `OnErrorOccurred` with the given code and message. Use this to exercise error-handling UI under every [`AlmediaErrorCode`](#almediaerrorcode).

#### `static void EmitLinkCompleted()`

Fires `OnLinkCompleted` with the current UTC timestamp. Use it to test "celebrate a fresh link" UX in isolation from the linking flow itself.

#### `static void EmitNotifications(params AlmediaNotification[] notifications)`

Fires `OnNotificationsReceived` with the supplied notifications, delivered through the same bridge path the native plugins use. Pass no arguments for an empty batch (note: the SDK short-circuits empty batches and does **not** raise `OnNotificationsReceived` - useful for verifying the "no new rewards" branch on the SDK side). A notification without a timestamp arrives with an empty `Timestamp` and a `null` `ReceivedAt`.

```csharp
AlmediaEditorMock.EmitNotifications(new AlmediaNotification("n-1", "Reward", "You earned 100 coins",
    DateTime.UtcNow.ToString("o"), "popup", null));
```

#### `static void EmitInGameRewardGrant(params AlmediaInGameReward[] rewards)`

Fires `OnInGameRewardGrantRequested` with a generated grant id and the current UTC timestamp, delivered through the same bridge path the native plugins use. Pass at least one reward; the SDK drops a rewardless grant as malformed, which a bare `EmitInGameRewardGrant()` can exercise.

#### `static void EmitInGameRewardGrant(string id, params AlmediaInGameReward[] rewards)`

Same, with an explicit grant id (null or empty generates one). Delivery on device is at-least-once, so call this twice with the same id to reproduce a redelivered grant and verify your dedup:

```csharp
var rewards = new[] { new AlmediaInGameReward(250, "gems"), new AlmediaInGameReward(3, "spins") };
AlmediaEditorMock.EmitInGameRewardGrant("grant-1", rewards);
AlmediaEditorMock.EmitInGameRewardGrant("grant-1", rewards);   // must credit once
```

#### `static void EmitProgress(AlmediaProgress progress)`

Applies `progress` as the latest snapshot. It uses the same bridge path as the native plugins. `Almedia.Progress` holds the snapshot and `OnProgressUpdated` fires on the same call. Pass `null` to model the native SDK clearing the snapshot: the accessor becomes `null` and the event fires with `null`. Build a snapshot with the public constructors. Pass a `null` username to model a snapshot that clears the previous username:

```csharp
var points = new AlmediaRewardPoints(12500, new AlmediaMoney(11.50m, "EUR"), new AlmediaMoney(12.50m, "USD"));
var task = new AlmediaTask("t-1", AlmediaTaskKind.Burning, "Reach level 10", points, new AlmediaTaskProgress(3, 10),
    DateTimeOffset.UtcNow.AddHours(2).ToString("o"));                          // RewardDropsAt in two hours
AlmediaEditorMock.EmitProgress(new AlmediaProgress("s-1", "2026-09-14T10:00:00.000Z", "demo_player",
    points, points, new[] { task }, new AlmediaCompletedTask[0], new AlmediaTask[0]));
AlmediaEditorMock.EmitProgress(new AlmediaProgress("s-2", "2026-09-14T10:01:00.000Z", null,
    points, points, new[] { task }, new AlmediaCompletedTask[0], new AlmediaTask[0]));   // Username reads null
AlmediaEditorMock.EmitProgress(null);                                          // Progress reads null
```

#### `static void EmitTaskCompleted(string id, AlmediaCompletedTask task)`

Fires `OnTaskCompleted`. If `id` is null or empty, the mock generates one. Delivery on a device is best-effort and can repeat. Call this method twice with the same id to reproduce a replay and to test your deduplication.

#### `static void EmitBalanceChanged(string id, AlmediaRewardPoints balance, AlmediaRewardPoints change)`

Fires `OnBalanceChanged` with the balance after the change and the signed change. Pass negative coins and negative amounts in `change` to model a reversal.

#### `static void EmitScreenPresented(AlmediaScreen screen)`

Fires `OnScreenPresented` for the given screen, without opening a real webview. Pair it with a later `EmitScreenDismissed` for the same screen to reproduce native's matched-pair contract in a test.

#### `static void EmitScreenDismissed(AlmediaScreen screen, AlmediaInAppScreenResult result)`

Fires `OnScreenDismissed` for the given screen with the given result:

```csharp
AlmediaEditorMock.EmitScreenDismissed(AlmediaScreen.Offer,
    new AlmediaInAppScreenResult.Failed(new AlmediaError(AlmediaErrorCode.NetworkFailure, "offline")));
```

#### `static void EmitNativeLog(AlmediaLogLevel level, string message)`

Delivers a forwarded log line through the same path the iOS/Android plugins use. Subscribers of `Almedia.OnLog` receive it as if it had come from the native side.

#### `static void CancelPending()`

Stops any pending auto-simulate coroutine. Mostly relevant for tests that want to assert "no callback fires" after `Initialize` - call this immediately after `Almedia.Initialize` and the scheduled `Eligible` status never arrives. The first call to any other `Emit*` method calls this internally as part of the manual-mode flip.

---

## UI components

All UI controllers live in `AlmediaLink.UI`. The SDK ships ready-to-drop prefabs in `Packages/com.almedia.link/Runtime/Prefabs/`; host code never needs to instantiate or call into the controllers directly. Customize by editing a prefab or creating a Prefab Variant. The popup, card, and overlay prefabs each carry an **Apply Host Settings** checkbox controlling whether the text/colors from **Almedia → Settings** are applied to them at runtime - untick it on a fully hand-authored variant.

---

### `LinkButtonController`

The component on the `LinkButton` prefabs. Two-state: visible while `Eligible` (a linking entry - tapping opens the `LinkPopup`) and while `Linked` **with the reward hub available** (a rewards entry - tapping opens it via `ShowRewardHub()`); hidden in every other case, including a linked player whose `CanShowRewardHub` is `false`. Hiding is deliberate there: showing the eligible state to a linked player would be wrong, and a rewards button that opens nothing is the failure this exists to remove. Promo tracking follows what is shown, so that case reports `promo_load` with `Hidden`.

The controller follows the status live, so a reward hub withdrawn mid-session hides the button. Each state is a self-contained subtree in the prefab (`Eligible` / `Linked`, each pairing a content object with its own button and background), so its art and press colours are authored in the prefab and the controller only toggles which one shows.

Drop one of the four `LinkButton` variants (`LinkButtonA`-`LinkButtonD`) into your Canvas - no code needed. With **Auto-Initialize From Prefabs** enabled in **Almedia → Settings**, the button calls `Initialize` with the settings values when nothing has called it by the end of its first frame. An earlier call of your own runs first, and a later call with a different configuration takes over. See [Initialize the SDK](./integration-guide.md#initialize-the-sdk).

To reproduce the behaviour on a button of your own, call [`ShowLink()`](#static-void-showlink) while the status is `Eligible`, and [`ShowRewardHub()`](#static-void-showrewardhub) while the status is `Linked` with `CanShowRewardHub`. `ShowLink()` shows the popup from the settings' **Link Popup** slot, not the button's reference.

The button also carries the **Link Popup** reference it opens on an eligible tap. Assign a Prefab Variant there to customize the popup; with the field emptied, an eligible tap starts linking directly (with a one-time warning).

---

### `LinkPopupController`

The component on the `LinkPopup` prefab. Renders the modal account-linking popup; the CTA button starts linking, and the popup closes itself on the close button, when linking completes, and when the player can no longer link.

Customize via Prefab Variant assigned on the `LinkButton` prefab's **Link Popup** field, and for [`ShowLink()`](#static-void-showlink) in **Almedia → Settings → Default UI Prefabs → Link Popup**. Text and colors are also directly editable in **Almedia → Settings → Link Popup Text**, applied while the popup's **Apply Host Settings** checkbox is ticked.

---

### `NotificationCardController`

The component on the `NotificationCard` prefab. Slides up from the bottom when notifications arrive, auto-dismisses after a few seconds, and opens the `ActivityOverlay` if multiple notifications stack.

Customize via Prefab Variant assigned in **Almedia → Settings → Default UI Prefabs → Notification Card**. The dismiss delay and bottom padding are inspector-tunable on the prefab.

---

### `ActivityOverlayController`

The component on the `ActivityOverlay` prefab. Full-screen notification list, opened by tapping a stacked `NotificationCard`.

Customize via Prefab Variant assigned in **Almedia → Settings → Default UI Prefabs → Activity Overlay**.

---

## Prefabs

All shipping prefabs live under `Packages/com.almedia.link/Runtime/Prefabs/`. They can be:

- Dropped directly into a scene Canvas (`LinkButtonA` to `LinkButtonD`).
- Instantiated by the SDK through serialized references (the rest): the popup from the button's **Link Popup** field, the notification card and overlay from **Almedia → Settings → Default UI Prefabs**.
- Used as the base for Prefab Variants.

Only prefabs that something references are included in a build.

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
| **Almedia → Settings**                | Opens the `AlmediaLinkSettingsEditor` window. Creates `Assets/Almedia/Resources/AlmediaSettings.asset` from the package default if missing. |

Android dependencies are provided automatically by the `AlmediaSDK.androidlib` subproject (`Packages/com.almedia.link/Plugins/Android/AlmediaSDK.androidlib/`). EDM4U-enabled projects additionally resolve `AlmediaSDKDependencies.xml`; Gradle dedupes by Maven coordinate. The iOS post-build hook (`AlmediaLinkBuildPostProcessor`) runs automatically as part of `BuildTarget.iOS` builds and exposes no menu item.

---

## Assembly definitions

| Asmdef                          | Includes platforms       | Auto-referenced | Notes |
|---------------------------------|---------------------------|:---------------:|-------|
| `AlmediaSDK.asmdef`             | Any                       | yes             | `Almedia` and every type in the `AlmediaSDK` namespace. `includePlatforms` is empty, so the runtime assembly compiles on every player target. Platform support is gated at runtime by native bridge selection: only iOS and Android are supported, and other targets raise `OnErrorOccurred` with `InvalidConfiguration` at `Initialize`. Root namespace: `AlmediaSDK`. |
| `AlmediaLink.asmdef`            | Any                       | yes             | `AlmediaLinkSettings`, the UI components and the `AlmediaLink` API. References `AlmediaSDK`. Root namespace: `AlmediaLink`. |
| `AlmediaLink.Editor.asmdef`     | Editor                    | yes             | Editor-only tooling and `AlmediaEditorMock`. Root namespace: `AlmediaLink.Editor`. References the runtime asmdefs. |

If you use Assembly Definition References yourself, add a reference to `AlmediaSDK`. Add `AlmediaLink` only for code that uses `AlmediaLinkSettings` or the UI components, and `AlmediaLink.Editor` only for editor test code that uses `AlmediaEditorMock`.
