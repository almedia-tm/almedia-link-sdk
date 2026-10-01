# Almedia SDK - Migration Guide

This guide upgrades a game from a release before 1.3.0. Moving to `com.almedia.sdk` replaces the package. Moving code from the `AlmediaLink` API to `Almedia`, the SDK's entry point in the `AlmediaSDK` namespace, is optional.

The [API reference](./api-reference.md) documents `AlmediaSDK`. The [AlmediaLink API reference](./almedialink-api-reference.md) documents `AlmediaLink`.

---

## Contents

1. [Upgrade the package](#upgrade-the-package)
2. [Compatibility](#compatibility)
3. [Migrate the code](#migrate-the-code)
4. [API mapping](#api-mapping)
5. [Status](#status)
6. [Value sets](#value-sets)
7. [Screen results](#screen-results)
8. [Starting linking](#starting-linking)
9. [Editor mock](#editor-mock)
10. [Event differences](#event-differences)

---

## Upgrade the package

1.3.0 is the last release of `com.almedia.link`. The same 1.3.0 also ships as `com.almedia.sdk`, published from a new repository, and later releases ship only as `com.almedia.sdk`.

1. In `Packages/manifest.json`, replace the `com.almedia.link` line with:

   ```json
   "com.almedia.sdk": "https://github.com/almedia-tm/almedia-sdk.git"
   ```

   Append `#vX.Y.Z` to the URL to pin a release. Never install both packages. If both lines end up in the manifest, the project stops compiling: the Console shows `Assembly with name 'AlmediaLink' already exists` and many `GUID … conflicts with` errors, one for each asset the two packages share. Remove the `com.almedia.link` line and the project compiles again, with nothing else to fix.
2. Open the project in the editor. On the first load, the settings asset moves from `Assets/AlmediaLink/Resources/AlmediaLinkSettings.asset` to `Assets/Almedia/Resources/AlmediaSettings.asset`, and the empty `Assets/AlmediaLink` folder is removed. The Console logs the move. If the move fails, the Console shows a warning, the SDK keeps using the old asset, and the next editor load tries again.
3. Commit the manifest change and the moved asset together.

Every asset keeps its GUID, so scenes, Prefab Variants and settings references resolve as before. If your own code loads the settings asset with `Resources.Load("AlmediaLinkSettings")`, change that call to `AlmediaLinkSettings.Load()`. No other code change is needed.

---

## Compatibility

Code written against `AlmediaLink` keeps compiling and behaving as in 1.2.1. SDK changes listed in the [changelog](../CHANGELOG.md) apply to both APIs. New features arrive on `Almedia`.

Both APIs drive the same SDK. One `Initialize` call, through either API, serves both. A project can move one file at a time.

The settings asset, `AlmediaLinkSettings`, the prefabs and the UI components serve both APIs. They need no change.

A game that declares `Linking` in its disabled features gets the reason `Disabled` on both APIs. On `AlmediaLink` it is the new `AlmediaNotAvailableReason.Disabled`.

---

## Migrate the code

Move one file at a time, and each file in one step:

1. Replace `using AlmediaLink;` and `using AlmediaLink.Models;` with `using AlmediaSDK;`. In editor tests, replace `using AlmediaLink.Editor.Testing;` with `using AlmediaSDK.Editor.Testing;`.
2. Rename types and members with the [API mapping](#api-mapping).
3. Fix what the compiler reports. The sections below cover each case.

Both namespaces define `AlmediaStatus`, `AlmediaLogLevel` and other names. A file that imports both gets ambiguous names. A file that uses `AlmediaSDK` and still needs `AlmediaLinkSettings` writes it in full, as `AlmediaLink.AlmediaLinkSettings`.

Code in an assembly definition needs a reference to `AlmediaSDK`. `AlmediaEditorMock` lives in the `AlmediaLink.Editor` assembly, so editor test assemblies keep that reference.

---

## API mapping

`AlmediaLinkSDK` becomes `Almedia`, and `AlmediaLinkConfig` becomes `AlmediaConfig`. The config keeps every property. These members keep their names:

- `Version`, `Progress`, `Initialize`, `ShowLink`, `ShowRewardHub`, `ShowOffer`, `FetchNotifications`, `StartNotificationPolling` and `StopNotificationPolling`.
- `OnStatusChanged`, `OnLinkCompleted`, `OnNotificationsReceived`, `OnInGameRewardGrantRequested`, `OnProgressUpdated`, `OnTaskCompleted`, `OnBalanceChanged`, `OnErrorOccurred`, `OnScreenPresented`, `OnScreenDismissed` and `OnLog`.

`OnStatusChanged` fires in more cases. See [Event differences](#event-differences).

These types keep their names, members and constructors: `AlmediaNotification`, `AlmediaInGameRewardGrant`, `AlmediaInGameReward`, `AlmediaProgress`, `AlmediaTask`, `AlmediaTaskKind`, `AlmediaTaskProgress`, `AlmediaCompletedTask`, `AlmediaRewardPoints`, `AlmediaMoney`, `AlmediaTaskCompletion`, `AlmediaBalanceChange`, `AlmediaFeature` and `AlmediaError`.

The rest maps as follows.

| `AlmediaLink` | `AlmediaSDK` |
|---|---|
| `AlmediaLinkSDK.CurrentStatus` | `Almedia.Status`. See [Status](#status). |
| `AlmediaLinkSDK.NotAvailableReason` | `Reason` on `AlmediaStatus.NotAvailable` |
| `AlmediaLinkSDK.ScreenAvailability` | `CanShowRewardHub` and `CanShowOffer` on `AlmediaStatus.Linked` |
| `AlmediaLinkSDK.OnScreenAvailabilityChanged` | `Almedia.OnStatusChanged` |
| `AlmediaLinkSDK.StartLinking(PlacementType)` | `Almedia.StartLinking(AlmediaPlacementType)` |
| `AlmediaLinkSDK.Engage()` | `Almedia.ShowLink()` or `Almedia.ShowRewardHub()`. See [Starting linking](#starting-linking). |
| `AlmediaStatus` enum | `AlmediaStatus` class, with one nested class per status |
| `AlmediaNotAvailableReason` enum | `AlmediaNotAvailableReason` class |
| `AlmediaScreenAvailability` | None. Its flags are on `AlmediaStatus.Linked`. |
| `AlmediaErrorCode`, `AlmediaScreen` and `AlmediaLogLevel` enums | Classes with the same member names. See [Value sets](#value-sets). |
| `PlacementType` enum | `AlmediaPlacementType` class |
| `InAppScreenResult` and `InAppScreenResultType` | `AlmediaInAppScreenResult`. See [Screen results](#screen-results). |
| `AlmediaLinkEditorMock` | `AlmediaEditorMock`. See [Editor mock](#editor-mock). |
| `MockNotification` | `AlmediaNotification` |
| `MockInGameReward` | `AlmediaInGameReward` |
| `AlmediaLinkSettings` | Unchanged. Both APIs use it. |

---

## Status

`Almedia.Status` holds the status as an object. It is never `null`. Each status is a nested class of `AlmediaStatus`: `NotInitialized`, `Eligible`, `Linked`, `NotAvailable`, `Blocked` and `Disabled`. `Linked` carries `CanShowRewardHub` and `CanShowOffer`. `NotAvailable` carries `Reason` and `RawReason`.

Test a status with a type pattern. One event replaces the two `AlmediaLink` events.

Before:

```csharp
using AlmediaLink;
using AlmediaLink.Models;

void OnEnable()
{
    AlmediaLinkSDK.OnStatusChanged += OnStatus;
    AlmediaLinkSDK.OnScreenAvailabilityChanged += OnAvailability;
    Refresh();
}

void OnDisable()
{
    AlmediaLinkSDK.OnStatusChanged -= OnStatus;
    AlmediaLinkSDK.OnScreenAvailabilityChanged -= OnAvailability;
}

void OnStatus(AlmediaStatus status) => Refresh();
void OnAvailability(AlmediaScreenAvailability availability) => Refresh();

void Refresh()
{
    var status = AlmediaLinkSDK.CurrentStatus;
    var availability = AlmediaLinkSDK.ScreenAvailability;
    linkButton.SetActive(status == AlmediaStatus.Eligible);
    rewardsButton.SetActive(status == AlmediaStatus.Linked && availability.CanShowRewardHub);
    offerButton.SetActive(availability.CanShowOffer);

    if (status == AlmediaStatus.NotAvailable)
        Analytics.Track("link_unavailable", AlmediaLinkSDK.NotAvailableReason.ToString());
}
```

After:

```csharp
using AlmediaSDK;

void OnEnable()
{
    Almedia.OnStatusChanged += Refresh;
    Refresh(Almedia.Status);
}

void OnDisable() => Almedia.OnStatusChanged -= Refresh;

void Refresh(AlmediaStatus status)
{
    linkButton.SetActive(status is AlmediaStatus.Eligible);
    rewardsButton.SetActive(status is AlmediaStatus.Linked { CanShowRewardHub: true });
    offerButton.SetActive(status is AlmediaStatus.Linked { CanShowOffer: true });

    if (status is AlmediaStatus.NotAvailable notAvailable)
        Analytics.Track("link_unavailable", notAvailable.Reason.ToString());
}
```

A `switch` on the status keeps a `default` branch. A later version can add statuses.

`Reason` is never `null` on `NotAvailable`. `RawReason` holds the reason exactly as the server sent it. It is empty when the server sent none.

`ToString()` on `Linked` and `NotAvailable` includes their data, for example `Linked(canShowRewardHub: True, canShowOffer: False)`. `status.GetType().Name` returns the bare name, as the enum printed it.

---

## Value sets

`AlmediaNotAvailableReason`, `AlmediaErrorCode`, `AlmediaScreen`, `AlmediaLogLevel` and `AlmediaPlacementType` are classes, not enums. Each has a fixed set of static members, named as in the `AlmediaLink` enums. Compare them with `==`. `ToString()` returns the member name.

Three enum habits do not carry over:

- A member is not a constant, so it cannot be a `case` label.
- A member has no numeric value, so casts to `int` and the `Enum` methods do not apply.
- Unity does not serialize these classes. A serialized field of an `AlmediaLink` enum, such as a `PlacementType` chosen in the Inspector, cannot keep its type. Store a game-side enum or string instead, and map it to the SDK value in code.

`AlmediaLogLevel` also supports `<`, `<=`, `>` and `>=`, from `Verbose` up to `Error`.

Before:

```csharp
AlmediaLinkSDK.OnLog += (level, message) =>
{
    switch (level)
    {
        case AlmediaLogLevel.Error:   Debug.LogError(message); break;
        case AlmediaLogLevel.Warning: Debug.LogWarning(message); break;
        default:                      Debug.Log(message); break;
    }
};
```

After:

```csharp
Almedia.OnLog += (level, message) =>
{
    if (level == AlmediaLogLevel.Error) Debug.LogError(message);
    else if (level == AlmediaLogLevel.Warning) Debug.LogWarning(message);
    else Debug.Log(message);
};
```

Statuses, screen results and `AlmediaError` compare by value too.

---

## Screen results

`OnScreenDismissed` delivers an `AlmediaInAppScreenResult`. It is one of three nested classes: `Completed`, `Cancelled` or `Failed`. Only `Failed` carries an `Error`. Test the result with a type pattern.

Before:

```csharp
AlmediaLinkSDK.OnScreenDismissed += (screen, result) =>
{
    switch (result.Type)
    {
        case InAppScreenResultType.Completed: break;
        case InAppScreenResultType.Cancelled: break;
        case InAppScreenResultType.Failed: Debug.LogWarning(result.Error.Message); break;
    }
};
```

After:

```csharp
Almedia.OnScreenDismissed += (screen, result) =>
{
    switch (result)
    {
        case AlmediaInAppScreenResult.Completed _: break;
        case AlmediaInAppScreenResult.Cancelled _: break;
        case AlmediaInAppScreenResult.Failed failed: Debug.LogWarning(failed.Error.Message); break;
        default: break;
    }
};
```

A `switch` on the result keeps a `default` branch. A later version can add results.

---

## Starting linking

`Almedia.Engage()` is a compile error, and the error message names the replacements. Call `ShowLink()` for an eligible player and `ShowRewardHub()` for a linked player with a reward hub.

`ShowLink()` shows the link popup, and the popup's button starts linking. `Engage()` started linking without the popup.

Before:

```csharp
myButton.onClick.AddListener(() => AlmediaLinkSDK.Engage());
```

After:

```csharp
myButton.onClick.AddListener(() =>
{
    var status = Almedia.Status;
    if (status is AlmediaStatus.Eligible)
        Almedia.ShowLink();
    else if (status is AlmediaStatus.Linked { CanShowRewardHub: true })
        Almedia.ShowRewardHub();
});
```

`StartLinking` still starts linking without the popup. Its placement is an `AlmediaPlacementType`, so `StartLinking(PlacementType.Banner)` becomes `StartLinking(AlmediaPlacementType.Banner)`. `StartLinking()` without an argument reports `Popup`, as before.

---

## Editor mock

`AlmediaEditorMock`, in `AlmediaSDK.Editor.Testing`, takes the `AlmediaSDK` types. Manual mode works as in `AlmediaLinkEditorMock`, and a call before `Initialize` throws as before. Either mock works with either API.

| `AlmediaLinkEditorMock` | `AlmediaEditorMock` |
|---|---|
| `EmitStatus(AlmediaStatus.Eligible)` | `EmitStatus(new AlmediaStatus.Eligible())` |
| `EmitStatus(AlmediaStatus.Linked)` | `EmitStatus(new AlmediaStatus.Linked(true, true))` |
| `EmitStatus(AlmediaStatus.Linked, canShowRewardHub: false, canShowOffer: false)` | `EmitStatus(new AlmediaStatus.Linked(false, false))` |
| `EmitStatus(AlmediaStatus.NotAvailable)` | `EmitStatus(new AlmediaStatus.NotAvailable(""))` |
| `EmitStatus(AlmediaStatus.NotAvailable, "holdout")` | `EmitStatus(new AlmediaStatus.NotAvailable("holdout"))` |
| `EmitInGameRewardGrant("grant-1", new MockInGameReward(250, "gems"))` | `EmitInGameRewardGrant("grant-1", new AlmediaInGameReward(250, "gems"))` |
| `EmitScreenDismissed(AlmediaScreen.Offer, InAppScreenResultType.Cancelled)` | `EmitScreenDismissed(AlmediaScreen.Offer, new AlmediaInAppScreenResult.Cancelled())` |
| `EmitScreenDismissed(AlmediaScreen.Offer, InAppScreenResultType.Failed, AlmediaErrorCode.NetworkFailure, "offline")` | `EmitScreenDismissed(AlmediaScreen.Offer, new AlmediaInAppScreenResult.Failed(new AlmediaError(AlmediaErrorCode.NetworkFailure, "offline")))` |

The other methods keep their names and parameters.

`EmitNotifications` takes `AlmediaNotification` instead of `MockNotification`. Its constructor takes six arguments, with the timestamp before the presentation hint:

```csharp
// AlmediaLinkEditorMock
new MockNotification("n-1", "Reward", "You earned 100 coins", "popup");

// AlmediaEditorMock
new AlmediaNotification("n-1", "Reward", "You earned 100 coins",
    DateTime.UtcNow.ToString("o"), "popup", null);
```

A `MockNotification` call with five or six arguments, copied argument for argument, still compiles. The timestamp and the presentation hint then trade places. Check each call.

`MockNotification` sets a missing timestamp to the current time. `AlmediaNotification` leaves it empty, and its `ReceivedAt` is then `null`.

---

## Event differences

`Almedia.OnStatusChanged` replaces both `AlmediaLink` status events. It also reports the reset when `Initialize` runs with a different configuration.

| Change | `AlmediaLinkSDK` | `Almedia` |
|---|---|---|
| The status changes | `OnStatusChanged` | `OnStatusChanged` |
| The reason of `NotAvailable` changes | `OnStatusChanged` | `OnStatusChanged` |
| A flag changes while the player stays linked | `OnScreenAvailabilityChanged` | `OnStatusChanged` |
| `Initialize` with a different configuration resets the status | No event | `OnStatusChanged` with `NotInitialized` |

Neither API fires when the reason changes from one unrecognized value to another.

When both APIs have subscribers, `Almedia.OnStatusChanged` fires first. Both APIs are up to date before either fires.
