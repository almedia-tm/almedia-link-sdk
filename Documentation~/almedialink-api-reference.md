# Almedia SDK - AlmediaLink API Reference

Reference for the `AlmediaLink` API of the Almedia SDK for Unity, the API of releases before 1.3.0. Code written against it keeps compiling and behaving as in 1.2.1. SDK changes listed in the [changelog](../CHANGELOG.md) apply to both APIs.

The SDK's entry point is `Almedia`, in the `AlmediaSDK` namespace, and new features arrive there. The [API reference](./api-reference.md) documents it, and the [migration guide](./migration-guide.md) shows how to move existing code to it. The API reference also documents what both APIs share: the settings asset (`AlmediaLinkSettings`), the UI components, the prefabs and the assembly definitions.

The SDK is versioned. The current release is reflected in `AlmediaLinkSDK.Version` and in `Packages/com.almedia.link/package.json`.

---

## Contents

- [Namespaces](#namespaces)
- [`AlmediaLinkSDK`](#almedialinksdk) - public static facade
- [`AlmediaLinkConfig`](#almedialinkconfig) - runtime configuration
- Models
  - [`AlmediaStatus`](#almediastatus)
  - [`AlmediaNotAvailableReason`](#almedianotavailablereason)
  - [`AlmediaScreenAvailability`](#almediascreenavailability)
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
  - [`PlacementType`](#placementtype)
  - [`AlmediaScreen`](#almediascreen)
  - [`AlmediaFeature`](#almediafeature)
  - [`InAppScreenResult`](#inappscreenresult)
  - [`InAppScreenResultType`](#inappscreenresulttype)
  - [`AlmediaLogLevel`](#almedialoglevel)
- [Editor testing](#editor-testing)
  - [`AlmediaLinkEditorMock`](#almedialinkeditormock)
  - [`MockNotification`](#mocknotification)
  - [`MockInGameReward`](#mockingamereward)

---

## Namespaces

| Namespace            | Contents |
|----------------------|----------|
| `AlmediaLink`        | The static `AlmediaLinkSDK` facade, `AlmediaLinkConfig`, `AlmediaLogLevel`. `AlmediaLinkSettings` lives here too, and both APIs use it. |
| `AlmediaLink.Models` | Status, error, notification, progress, placement, feature, and in-app screen result types. |
| `AlmediaLink.Editor.Testing` | `AlmediaLinkEditorMock`, `MockNotification` and `MockInGameReward` - editor-only test hooks for driving non-happy paths. Excluded from player builds. |

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

#### `static AlmediaNotAvailableReason? NotAvailableReason { get; }`

Why the SDK is unavailable for this player. Non-null exactly while `CurrentStatus` is `NotAvailable`, and `null` in every other status. When the SDK reports `NotAvailable`, read this value to answer why.

A `NotAvailable` status whose reason the backend did not send, or sent as a value this SDK version does not recognize, reads as [`AlmediaNotAvailableReason.Unknown`](#almedianotavailablereason) rather than `null`. Treat `Unknown` as "not available, cause unspecified", not as an error.

Updated **before** `OnStatusChanged` fires, so a handler can read it directly:

```csharp
AlmediaLinkSDK.OnStatusChanged += status =>
{
    if (status == AlmediaStatus.NotAvailable
        && AlmediaLinkSDK.NotAvailableReason == AlmediaNotAvailableReason.Holdout)
        Analytics.Track("link_holdout");
};
```

The status itself is what your UI should react to - the entry point hides on `NotAvailable` regardless of reason. The reason exists for analytics, diagnostics, and messaging.

#### `static AlmediaScreenAvailability ScreenAvailability { get; }`

Which SDK screens can be presented right now - see [`AlmediaScreenAvailability`](#almediascreenavailability). Reads all-`false` until the SDK is ready.

Updated with every SDK status update, and it can change while the status itself stays the same: a linked player can gain or lose a screen at any time. Check it before showing your own entry point, and subscribe to [`OnScreenAvailabilityChanged`](#static-event-actionalmediascreenavailability-onscreenavailabilitychanged) to keep it current.

```csharp
if (AlmediaLinkSDK.CurrentStatus == AlmediaStatus.Linked
    && AlmediaLinkSDK.ScreenAvailability.CanShowRewardHub)
    rewardsButton.SetActive(true);
```

Updated before either status or availability event fires. Do **not** cache it in your own field - see [Availability comes and goes](./integration-guide.md#availability-comes-and-goes).

#### `static AlmediaProgress Progress { get; }`

The latest progress snapshot, or `null` when the SDK has none. See [`AlmediaProgress`](#almediaprogress). It is `null` before the first snapshot arrives, after the native SDK clears the snapshot with its message stream token (an account change, or a token the server refused), and after `Initialize` with a different configuration. The SDK keeps it in memory only.

The SDK updates it before [`OnProgressUpdated`](#static-event-actionalmediaprogress-onprogressupdated) fires. A scene that loads after the event can read the accessor, then subscribe for updates:

```csharp
void OnEnable()
{
    Render(AlmediaLinkSDK.Progress);                 // null until the first snapshot
    AlmediaLinkSDK.OnProgressUpdated += Render;
}
```

A snapshot is complete. Render the snapshot you get. Do not keep state from a previous snapshot.

---

### Methods

#### `static void Initialize(AlmediaLinkConfig config)`

Boots the SDK. Merges `config` with `AlmediaLinkSettings`, instantiates the native bridge for the current platform, sends an `init` request to the backend, and starts the status lifecycle that resolves into a terminal `AlmediaStatus` (delivered via `OnStatusChanged` and reflected in `CurrentStatus`).

With **Auto-Initialize From Prefabs** enabled in [`AlmediaLinkSettings`](./api-reference.md#almedialinksettings), a `LinkButton` prefab calls this by itself with the settings values when nothing has called it by the end of the button's first frame. A call of your own always takes precedence.

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

#### `static void ShowLink()`

Shows the link popup to a player who can link. The popup's CTA button starts linking. The popup reports the same analytics as when the `LinkButton` prefab opens it. This is the recommended way to start linking from your own button.

The popup comes from **Almedia → Settings → Default UI Prefabs → Link Popup** (see [`AlmediaLinkSettings`](./api-reference.md#default-ui-prefabs)). With that slot empty, `ShowLink()` shows the popup assigned to the deprecated `LinkPopupOverride`. With neither assigned, it starts linking directly and logs a warning through `OnLog` once.

A no-op (with a warning) until the SDK is ready. After that, it does nothing, with a log, unless `CurrentStatus` is `Eligible`. It also does nothing while a link popup is already open. A popup that fails to open, for example a Prefab Variant with a missing reference, is removed and reported through `OnLog` at `Error`.

#### `static void StartLinking(PlacementType placement = PlacementType.Popup)`

Opens the native account-linking flow directly, without the popup. The `placement` value is analytics metadata - it tells the backend which UI surface drove the call.

**In the bundled integration, host code does not call this directly.** The `LinkPopup`'s CTA button calls `StartLinking` for you, whether the popup came from the `LinkButton` prefab or from [`ShowLink()`](#static-void-showlink). For your own popup design, assign a Prefab Variant of `LinkPopup` to the **Link Popup** slot and call `ShowLink()`. Call `StartLinking` yourself only from the CTA of a popup built without `LinkPopupController`.

A no-op (with a warning logged via `OnLog`) if `Initialize` has not been called, or if the SDK has not yet reached a terminal status (the first `OnStatusChanged` has not fired).

#### `static void ShowRewardHub()`

Opens the reward progression screen in a webview. When the screen appears, [`OnScreenPresented`](#static-event-actionalmediascreen-onscreenpresented) fires with `AlmediaScreen.RewardHub`, and its dismissal is reported through [`OnScreenDismissed`](#static-event-actionalmediascreen-inappscreenresult-onscreendismissed).

A no-op (with a warning) until the SDK is ready. Safe to call in any state: native opens the screen only for a linked player for whom the reward hub is currently available, and otherwise does nothing with the reason logged through `OnLog` - a no-op call fires neither lifecycle event. Reward hub availability can change mid-session, so a call that worked earlier may later be a no-op.

Check [`ScreenAvailability.CanShowRewardHub`](#almediascreenavailability) before calling, and gate your entry point on it rather than on a flag you cache yourself. Availability can still change between rendering a button and the tap.

#### `static void ShowOffer()`

Opens the offer screen in a webview. When the screen appears, [`OnScreenPresented`](#static-event-actionalmediascreen-onscreenpresented) fires with `AlmediaScreen.Offer`, and its dismissal is reported through [`OnScreenDismissed`](#static-event-actionalmediascreen-inappscreenresult-onscreendismissed).

A no-op (with a warning) until the SDK is ready. Safe to call in any state: native opens the screen only for a linked player for whom an offer is currently available, and otherwise does nothing with the reason logged through `OnLog` - a no-op call fires neither lifecycle event. Offer availability can change mid-session, so a call that worked earlier may later be a no-op.

Check [`ScreenAvailability.CanShowOffer`](#almediascreenavailability) before calling, and gate your entry point on it rather than on a flag you cache yourself. Availability can still change between rendering a button and the tap; such a call does nothing and logs.

#### `static void Engage()`

Context-aware entry point. Forwards to native, which routes on the player's state:

| Player state | Native action |
|---|---|
| `Eligible` | starts linking |
| `Linked` | opens the reward hub |
| anything else | no-op + log with the reason |

**All routing lives in native - Unity does not switch on status.** `Engage()` forwards the call whenever the SDK is ready, regardless of the current status; the state-based decision (including the no-op case) happens on the native side and is reported through `OnLog`. A no-op (with a warning) until the SDK is ready.

On `Eligible` it starts linking directly, without the popup. To show the popup, call [`ShowLink()`](#static-void-showlink) while the player can link and [`ShowRewardHub()`](#static-void-showrewardhub) once linked.

#### `static void FetchNotifications()`

Issues a one-shot request for the latest reward notifications. Result arrives via `OnNotificationsReceived` if there are any. A no-op (with a warning) until the SDK is ready - i.e. before the first `OnStatusChanged` has fired. Offline, the request fails quietly: a `Warning` log, no `OnErrorOccurred`, and nothing delivered until the next poll or call finds the network again. The backend can hold the request for up to a minute until a message arrives.

**Resets the polling clock when polling is active.** Calling `FetchNotifications` while the polling loop is running fires the immediate request and reschedules the next polling tick to `now + interval` instead of letting it fire at the originally planned time. This dedups the immediate request against a near-due scheduled tick - hosts that call `FetchNotifications` on user action (e.g. opening a notifications drawer) won't see a near-duplicate poll arrive moments later.

#### `static void StartNotificationPolling()`

Resumes the notification polling loop. The loop auto-starts once the player's status reaches `Linked`, so host code only needs this method to recover from an explicit `StopNotificationPolling()` (for example, after a cinematic or full-screen ad). The backend can set the interval. When it sets none, the interval comes from `AlmediaLinkConfig.NotificationsPollingIntervalSec` / `AlmediaLinkSettings.NotificationPollIntervalSeconds` (default 30 s, minimum 5 s). The native plugin pauses polling automatically when the app backgrounds and resumes on foreground. A no-op (with a warning) until the SDK is ready - i.e. before the first `OnStatusChanged` has fired.

**Idempotent.** Calling `StartNotificationPolling` while the loop is already running is a no-op - it does not double-schedule, does not shorten the interval, and does not trigger an immediate poll. To force an immediate fetch, call `FetchNotifications`.

#### `static void StopNotificationPolling()`

Stops the polling loop. Call this from cinematics, full-screen ads, or any UI state where notifications would intrude.

**Idempotent.** Calling `StopNotificationPolling` when the loop is not running is a no-op. Safe to call defensively before a scene where notifications should be suppressed.

---

### Events

All events are static. Subscribe before calling `Initialize` if you want to observe every transition. Components that mount later can recover the current state by reading `CurrentStatus` and then subscribing to `OnStatusChanged` for further transitions.

All events fire on the **Unity main thread**, so handlers can touch any Unity API (UI, `GetComponent`, `transform`, scene loading) directly. See [Threading](./integration-guide.md#threading) in the integration guide for the underlying mechanism.

#### `static event Action<AlmediaStatus> OnStatusChanged`

Fires on every status transition. The first emission carries the first terminal `AlmediaStatus` (any of `Eligible`, `Linked`, `NotAvailable`, `Blocked`, `Disabled`); subsequent emissions reflect later transitions. Use it both for one-shot setup that should happen once the SDK has resolved (gate on `status != NotInitialized` and your own guard flag) and for live UI that mirrors the current state. The bundled `LinkButtonController` subscribes to this event and to `OnScreenAvailabilityChanged`, and shows or hides itself from both. `StartLinking`, `FetchNotifications`, and `StartNotificationPolling` called before this first emission are safe no-ops (a warning is logged), so a mistimed call is harmless rather than a crash.

Can fire more than once for `NotAvailable` when `NotAvailableReason` changes. It does not re-fire for a screen-availability-only change - that is what `OnScreenAvailabilityChanged` is for.

#### `static event Action<AlmediaScreenAvailability> OnScreenAvailabilityChanged`

Fires when `ScreenAvailability` changes, carrying the new snapshot. This is the signal that a linked player has gained or lost the ability to use `ShowRewardHub()` or `ShowOffer()`.

```csharp
AlmediaLinkSDK.OnScreenAvailabilityChanged += availability =>
    offerButton.SetActive(availability.CanShowOffer);
```

**Ordering.** One native update produces at most two events. `CurrentStatus`, `NotAvailableReason`, and `ScreenAvailability` are all current **before either** fires, so a handler for one may read the others and see a consistent picture. When both fire, `OnStatusChanged` comes first.

| What changed in the native update | `OnStatusChanged` | `OnScreenAvailabilityChanged` |
|---|---|---|
| Status (with or without availability) | fires | fires if availability also changed |
| Reason only, status still `NotAvailable` | fires | - |
| Availability only | - | fires |
| Nothing (identical payload) | - | - |

A handler that throws is logged through `OnLog` and does not prevent the other event from firing.

#### `static event Action<string> OnLinkCompleted`

Fires when the player completes account linking flow. The argument is an ISO-8601 timestamp string from the backend, e.g. `"2026-05-13T14:23:01Z"`.

Does **not** fire for players who were already linked at `Initialize` time - they receive `AlmediaStatus.Linked` via `OnStatusChanged` instead. Use this event when you need to distinguish a fresh link from an already-linked session (e.g. celebration UX, one-time analytics).

#### `static event Action<List<AlmediaNotification>> OnNotificationsReceived`

Fires when a polling tick or `FetchNotifications()` returns one or more notifications. Empty results do not fire the event. Notifications are delivered in arrival order (oldest first).

#### `static event Action<AlmediaInGameRewardGrant> OnInGameRewardGrantRequested`

Fires when the backend instructs the game to grant in-game rewards. Credit the player and celebrate here.

One event per grant. A grant is a bundle of one or more rewards granted together ([`AlmediaInGameRewardGrant.Rewards`](#almediaingamerewardgrant)): "250 gems and 3 spins for finishing the chapter" arrives as one event with one id and deserves one celebration. Three separate grants arriving in the same polling tick raise three events.

**Delivery is at-least-once.** The same grant can arrive more than once. The server-to-server reward postback remains the authoritative record of what was granted; treat this event as the real-time convenience, not the ledger. Each grant has a unique [`Id`](#almediaingamerewardgrant) that a redelivery repeats, so deduplicate on it when a repeat credit matters to your economy. A short memory of recent ids suffices:

```csharp
private readonly HashSet<string> _credited = new HashSet<string>();

AlmediaLinkSDK.OnInGameRewardGrantRequested += grant =>
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

AlmediaLinkSDK.OnTaskCompleted += completion =>
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

The event fires for every trigger that actually presents a screen: the API methods (`StartLinking`, `ShowRewardHub`, `ShowOffer`), the `LinkPopup`'s CTA after the `LinkButton` prefab or `ShowLink()` opened it, and `Engage()` routing. The popup itself is not an SDK screen and fires neither event.

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

**`DisabledFeatures`.** The parts of the Link experience this game hides from the current player - see [`AlmediaFeature`](#almediafeature). Getter-only and never null; declare it with a collection initializer: `new AlmediaLinkConfig { DisabledFeatures = { AlmediaFeature.Offer } }`. Joined with the settings asset's **Disabled Features** as a union: the asset is the floor, code adds and never removes. The set is sent once at `Initialize`; a different set on a later call re-initializes.

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
| `NotAvailable`    | Service is not available for this user (region, eligibility, holdout). Linking UI should hide. [`NotAvailableReason`](#almedianotavailablereason) says why. |
| `Blocked`         | User is blocked by the backend (abuse, fraud). Linking UI should hide. |
| `Disabled`        | Integration is killswitched at the backend level. SDK runs in a no-op mode until the backend re-enables it. |

Terminal values: `Eligible`, `Linked`, `NotAvailable`, `Blocked`, `Disabled`. `NotInitialized` is the pre-init default and is never re-entered after the first terminal transition.

The status says whether the service is available; it does not say whether a given screen can be opened. A `Linked` player may still have no reward hub - see [`AlmediaScreenAvailability`](#almediascreenavailability).

---

### `AlmediaNotAvailableReason`

Namespace: `AlmediaLink.Models` · enum

Why the SDK is `NotAvailable` for this player. Read it from `AlmediaLinkSDK.NotAvailableReason`, which is non-null exactly while `CurrentStatus` is `NotAvailable`.

| Value     | Meaning |
|-----------|---------|
| `Unknown` | The backend sent no reason, or one this SDK version does not recognize. Not an error - read it as "not available, cause unspecified". |
| `Holdout` | The player is in the holdout (control) group and is deliberately excluded from the Link experience. |
| `Disabled` | The game declared `Linking` in its [disabled features](#almediafeature). |

The mapping from the backend's value happens inside the SDK, so a reason added by the backend later lands on `Unknown` in an existing build rather than surfacing a value your code cannot name. Handle `Unknown` as a real case; do not `switch` exhaustively without a default.

The reason never changes what the SDK does - the entry point hides on `NotAvailable` whatever the reason. Use it for analytics, support diagnostics, and messaging.

---

### `AlmediaScreenAvailability`

Namespace: `AlmediaLink.Models` · readonly struct

Which SDK screens native can present right now. Read it from `AlmediaLinkSDK.ScreenAvailability`; changes fire [`OnScreenAvailabilityChanged`](#static-event-actionalmediascreenavailability-onscreenavailabilitychanged).

```csharp
public readonly struct AlmediaScreenAvailability : IEquatable<AlmediaScreenAvailability>
{
    public bool CanShowRewardHub { get; }
    public bool CanShowOffer { get; }
}
```

| Member             | Meaning |
|--------------------|---------|
| `CanShowRewardHub` | `ShowRewardHub()` would present a screen right now. |
| `CanShowOffer`     | `ShowOffer()` would present a screen right now. |

Value type with `Equals`, `==`, and `!=`, so two snapshots can be compared directly.

**These are not derived from the status.** Both read `false` for a player who is not linked, but a `Linked` player can also have either flag `false` - a reward hub the backend has withdrawn, or an offer that has not appeared yet - and the flags can flip between syncs without the status moving. Availability is a live signal, not a property of the status.

Both flags read `false` until the SDK reaches its first terminal status.

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
public string           Display    { get; }
public string           IconUrl    { get; }

[Obsolete] public string Type      { get; }   // alias of Display
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

---

### `AlmediaInGameRewardGrant`

Namespace: `AlmediaLink.Models` · class

An in-game-reward grant delivered via [`OnInGameRewardGrantRequested`](#static-event-actionalmediaingamerewardgrant-oningamerewardgrantrequested). Every reward in `Rewards` was granted together and belongs to one celebration.

```csharp
public string                             Id         { get; }
public string                             Timestamp  { get; }
public DateTimeOffset?                    ReceivedAt { get; }
public IReadOnlyList<AlmediaInGameReward> Rewards    { get; }
```

| Property     | Notes |
|--------------|-------|
| `Id`         | Server-issued and unique per grant; a redelivery repeats it. The dedup key for hosts that want exactly-once crediting. |
| `Timestamp`  | Raw ISO 8601 string of when the backend issued the grant. |
| `ReceivedAt` | Parsed UTC `DateTimeOffset?`, or `null` when `Timestamp` is empty or unparseable. |
| `Rewards`    | The reward line items. At least one entry. |

---

### `AlmediaInGameReward`

Namespace: `AlmediaLink.Models` · class

One reward line item of an [`AlmediaInGameRewardGrant`](#almediaingamerewardgrant).

```csharp
public double Amount { get; }
public string Code   { get; }
```

| Property   | Notes |
|------------|-------|
| `Amount`   | Amount to credit. Whole today, but the contract permits fractions - do not truncate to an integer. |
| `Code`     | Reward code agreed with Almedia (e.g. `"gems"`, `"spins"`). Treat unrecognized codes as a no-op rather than an error. |

---

### `AlmediaProgress`

Namespace: `AlmediaLink.Models` · class

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

---

### `AlmediaTask`

Namespace: `AlmediaLink.Models` · class

A task the player can do in this game to earn a reward.

```csharp
public string              Id                     { get; }
public string              Kind                   { get; }
public string              Title                  { get; }
public AlmediaRewardPoints Reward                 { get; }
public AlmediaTaskProgress Progress               { get; }
public string              RewardDropsAtTimestamp { get; }
public DateTimeOffset?     RewardDropsAt          { get; }
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

---

### `AlmediaTaskKind`

Namespace: `AlmediaLink.Models` · static class

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

Namespace: `AlmediaLink.Models` · class

The progress of an [`AlmediaTask`](#almediatask).

```csharp
public long Value  { get; }
public long Target { get; }
```

| Property | Notes |
|----------|-------|
| `Value`  | The value reached so far. |
| `Target` | The value needed to complete the task. |

---

### `AlmediaCompletedTask`

Namespace: `AlmediaLink.Models` · class

One completion of an [`AlmediaTask`](#almediatask). A player can complete the same task more than once.

```csharp
public AlmediaTask         Task         { get; }
public string              Timestamp    { get; }
public DateTimeOffset?     CompletedAt  { get; }
public AlmediaRewardPoints ActualReward { get; }
```

| Property       | Notes |
|----------------|-------|
| `Task`         | The completed task. |
| `Timestamp`    | Raw ISO 8601 string. The time of the completion. |
| `CompletedAt`  | Parsed UTC `DateTimeOffset?`. `null` when `Timestamp` is empty or not valid. |
| `ActualReward` | The reward this completion paid. It differs from `Task.Reward` when the reward decreased before completion, or when Freecash adjusted the credit. |

---

### `AlmediaRewardPoints`

Namespace: `AlmediaLink.Models` · class

A balance or a reward in coins, with the display values the server supplied.

```csharp
public long         Coins            { get; }
public AlmediaMoney InPlayerCurrency { get; }
public AlmediaMoney InUsd            { get; }
```

| Property           | Notes |
|--------------------|-------|
| `Coins`            | The canonical amount. The money values are for display only. The value is signed. A reversal is negative. |
| `InPlayerCurrency` | The amount in the currency the server selected for the player. If this SDK version does not support that currency, the SDK substitutes the `InUsd` value and the `"USD"` code. |
| `InUsd`            | The amount in USD. |

Each SDK version supports a closed set of player currencies. You can write your formatting against this list. This release supports `USD`, `EUR`, `GBP`, `CAD`, `AUD`, `PLN`, `CHF`, `KRW`, `JPY` and `SEK`. A new market needs a new SDK release.

---

### `AlmediaMoney`

Namespace: `AlmediaLink.Models` · class

A coin amount in real money, for display only.

```csharp
public decimal Amount   { get; }
public string  Currency { get; }
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

Namespace: `AlmediaLink.Models` · class

A task the server reports as completed. [`OnTaskCompleted`](#static-event-actionalmediataskcompletion-ontaskcompleted) delivers it.

```csharp
public string               Id   { get; }
public AlmediaCompletedTask Task { get; }
```

| Property | Notes |
|----------|-------|
| `Id`     | The server-generated message id. A replay carries the same id. Deduplicate on it. |
| `Task`   | The completion, with `ActualReward`. The task is not always in the current snapshot. |

---

### `AlmediaBalanceChange`

Namespace: `AlmediaLink.Models` · class

A balance change the server reports. [`OnBalanceChanged`](#static-event-actionalmediabalancechange-onbalancechanged) delivers it.

```csharp
public string              Id      { get; }
public AlmediaRewardPoints Balance { get; }
public AlmediaRewardPoints Change  { get; }
```

| Property  | Notes |
|-----------|-------|
| `Id`      | The server-generated message id. A replay carries the same id. Deduplicate on it. |
| `Balance` | The balance after the change. It can differ from the current snapshot. |
| `Change`  | The signed change. A reversal is negative. |

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

### `AlmediaFeature`

Namespace: `AlmediaLink.Models` · sealed class

A part of the Link experience a game can declare it hides from its players, through [`AlmediaLinkConfig.DisabledFeatures`](#almedialinkconfig) or the settings asset. The backend enforces the declaration; the SDK only reports it and reacts to the status that comes back. Only the static members below exist; there is no constructor and no cast, so an undeclared value cannot be built. A `null` entry in `DisabledFeatures` is ignored.

| Member          | Wire name         | Disabled means |
|-----------------|-------------------|----------------|
| `Linking`       | `"linking"`       | A not-linked player reads `NotAvailable` with reason [`Disabled`](#almedianotavailablereason); `StartLinking()` fails through the existing status check. A linked player is untouched. |
| `RewardHub`     | `"reward_hub"`    | `ScreenAvailability.CanShowRewardHub` is false; `ShowRewardHub()` is a no-op; the bundled button hides. |
| `Offer`         | `"offer"`         | `ScreenAvailability.CanShowOffer` is false; `ShowOffer()` is a no-op. |
| `Notifications` | `"notifications"` | No Almedia notification reaches the game, in the SDK's UI or through `OnNotificationsReceived`. Reward grants are unaffected. |

Linking and the two screens are independent: hiding the entry point does not un-link anyone or close the hub to a player who linked earlier.

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

Editor-only test hook for driving the SDK into any state (status, error, notifications, in-game reward grants, progress, native log) from a play-mode test or an editor host script. See [Driving non-happy paths](./integration-guide.md#driving-non-happy-paths) in the integration guide for the narrative.

The whole API lives in `AlmediaLink.Editor.Testing` (assembly `AlmediaLink.Editor`, `includePlatforms:["Editor"]`). Host references must be wrapped in `#if UNITY_EDITOR` so they don't reach iOS/Android player compilation.

---

### `AlmediaLinkEditorMock`

Namespace: `AlmediaLink.Editor.Testing` · static class

The first call to any method here puts the underlying editor mock into **manual mode** for the rest of the play session: every subsequent `Initialize` / `StartLinking` / `FetchNotifications` becomes a no-op so canned coroutines cannot race against test emissions, and any already-scheduled simulate coroutine is cancelled on the flip. Manual mode resets on domain reload.

Every method **throws `InvalidOperationException`** if called before `AlmediaLinkSDK.Initialize` - the throw is deliberate so test ordering bugs surface at test-author time instead of as silent no-ops.

#### `static void EmitStatus(AlmediaStatus status, string reason = null, bool? canShowRewardHub = null, bool? canShowOffer = null)`

Delivers a status transition. `CurrentStatus`, `NotAvailableReason`, `ScreenAvailability` and their events reflect it on the same call - no `yield return` required before reading.

`reason` is the raw wire string and is meaningful only with `AlmediaStatus.NotAvailable`: `"holdout"` maps to [`AlmediaNotAvailableReason.Holdout`](#almedianotavailablereason), `"disabled"` to `Disabled`, any other string to `Unknown`.

A call that does not pass the availability parameters, e.g. `EmitStatus(AlmediaStatus.Linked)`, gets them defaulted to `true` for `Linked` and `false` for every other status. Pass them explicitly for other shapes:

```csharp
// Holdout player.
AlmediaLinkEditorMock.EmitStatus(AlmediaStatus.NotAvailable, "holdout");

// Linking declared in DisabledFeatures - the bundled LinkButton hides.
AlmediaLinkEditorMock.EmitStatus(AlmediaStatus.NotAvailable, "disabled");

// Linked, but the reward hub is gone - the bundled LinkButton hides.
AlmediaLinkEditorMock.EmitStatus(AlmediaStatus.Linked, canShowRewardHub: false, canShowOffer: false);
```

#### `static void EmitError(AlmediaErrorCode code, string message)`

Fires `OnErrorOccurred` with the given code and message. Use this to exercise error-handling UI under every entry in [`AlmediaErrorCode`](#almediaerrorcode).

#### `static void EmitLinkCompleted()`

Fires `OnLinkCompleted` with the current UTC timestamp. Use it to test "celebrate a fresh link" UX in isolation from the linking flow itself.

#### `static void EmitNotifications(params MockNotification[] items)`

Fires `OnNotificationsReceived` with the supplied items, converted into the SDK's internal model. Pass no arguments for an empty batch (note: `AlmediaLinkSDK` short-circuits empty batches and does **not** raise `OnNotificationsReceived` - useful for verifying the "no new rewards" branch on the SDK side).

#### `static void EmitInGameRewardGrant(params MockInGameReward[] rewards)`

Fires `OnInGameRewardGrantRequested` with a generated grant id and the current UTC timestamp, delivered through the same bridge path the native plugins use. Pass at least one reward; the SDK drops a rewardless grant as malformed, which a bare `EmitInGameRewardGrant()` can exercise.

#### `static void EmitInGameRewardGrant(string id, params MockInGameReward[] rewards)`

Same, with an explicit grant id (null or empty generates one). Delivery on device is at-least-once, so call this twice with the same id to reproduce a redelivered grant and verify your dedup:

```csharp
var rewards = new[] { new MockInGameReward(250, "gems"), new MockInGameReward(3, "spins") };
AlmediaLinkEditorMock.EmitInGameRewardGrant("grant-1", rewards);
AlmediaLinkEditorMock.EmitInGameRewardGrant("grant-1", rewards);   // must credit once
```

#### `static void EmitProgress(AlmediaProgress progress)`

Applies `progress` as the latest snapshot. It uses the same bridge path as the native plugins. `AlmediaLinkSDK.Progress` holds the snapshot and `OnProgressUpdated` fires on the same call. Pass `null` to model the native SDK clearing the snapshot: the accessor becomes `null` and the event fires with `null`. Build a snapshot with the public constructors. Pass a `null` username to model a snapshot that clears the previous username:

```csharp
var points = new AlmediaRewardPoints(12500, new AlmediaMoney(11.50m, "EUR"), new AlmediaMoney(12.50m, "USD"));
var task = new AlmediaTask("t-1", AlmediaTaskKind.Burning, "Reach level 10", points, new AlmediaTaskProgress(3, 10),
    DateTimeOffset.UtcNow.AddHours(2).ToString("o"));                          // RewardDropsAt in two hours
AlmediaLinkEditorMock.EmitProgress(new AlmediaProgress("s-1", "2026-09-14T10:00:00.000Z", "demo_player",
    points, points, new[] { task }, new AlmediaCompletedTask[0], new AlmediaTask[0]));
AlmediaLinkEditorMock.EmitProgress(new AlmediaProgress("s-2", "2026-09-14T10:01:00.000Z", null,
    points, points, new[] { task }, new AlmediaCompletedTask[0], new AlmediaTask[0]));   // Username reads null
AlmediaLinkEditorMock.EmitProgress(null);                                          // Progress reads null
```

#### `static void EmitTaskCompleted(string id, AlmediaCompletedTask task)`

Fires `OnTaskCompleted`. If `id` is null or empty, the mock generates one. Delivery on a device is best-effort and can repeat. Call this method twice with the same id to reproduce a replay and to test your deduplication.

#### `static void EmitBalanceChanged(string id, AlmediaRewardPoints balance, AlmediaRewardPoints change)`

Fires `OnBalanceChanged` with the balance after the change and the signed change. Pass negative coins and negative amounts in `change` to model a reversal.

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
    public readonly string Display;
    public readonly string Timestamp;
    public readonly string IconUrl;

    [Obsolete] public string Type { get; }   // alias of Display

    public MockNotification(string id, string title, string message, string display,
        string timestamp = null, string iconUrl = null);
}
```

`display` models the wire presentation hint (`"popup"` or `"tray"`; native never forwards anything else). A null `timestamp` defaults to `DateTime.UtcNow.ToString("o")` (ISO-8601), matching the format the backend emits. A null `iconUrl` models the omitted wire key and surfaces as `AlmediaNotification.IconUrl == null`.

---

### `MockInGameReward`

Namespace: `AlmediaLink.Editor.Testing` · readonly struct

One reward line item for [`EmitInGameRewardGrant`](#static-void-emitingamerewardgrantparams-mockingamereward-rewards).

```csharp
public readonly struct MockInGameReward
{
    public readonly double Amount;
    public readonly string Code;

    public MockInGameReward(double amount, string code);
}
```
