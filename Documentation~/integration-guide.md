# Almedia SDK - Integration Guide

This guide walks through integrating the Almedia SDK into a Unity game, from a fresh install through to a working linking flow and reward notifications. The companion document is the [API reference](./api-reference.md).

Integrations built on the `AlmediaLink` API of releases before 1.3.0 keep working. The [migration guide](./migration-guide.md) shows how to move them to `Almedia`.

---

## Contents

1. [Overview](#overview)
2. [Threading](#threading)
3. [Requirements](#requirements)
4. [Get your integration keys](#get-your-integration-keys)
5. [Install the package](#install-the-package)
6. [Configure the SDK](#configure-the-sdk)
7. [Customize the UI](#customize-the-ui)
8. [Initialize the SDK](#initialize-the-sdk)
9. [Disabled features](#disabled-features)
10. [Status and lifecycle](#status-and-lifecycle)
11. [Show the Link Button](#show-the-link-button)
12. [The linking flow](#the-linking-flow)
13. [Reward hub](#reward-hub)
14. [Offers](#offers)
15. [Pausing your game](#pausing-your-game)
16. [Reward notifications](#reward-notifications)
17. [In-game reward grants](#in-game-reward-grants)
18. [Progress and rewards](#progress-and-rewards)
19. [iOS - App Tracking Transparency](#ios---app-tracking-transparency)
20. [Android - Gradle dependencies](#android---gradle-dependencies)
21. [Logging](#logging)
22. [Error handling](#error-handling)
23. [Crash symbolication](#crash-symbolication)
24. [Editor and play mode](#editor-and-play-mode)
25. [Troubleshooting](#troubleshooting)

---

## Overview

The Almedia SDK lets players connect a Freecash account from inside a Unity game and surfaces reward notifications as they arrive. The linking flow runs in a secure native browser managed by the iOS and Android plugins. The rest is Unity UI: a Link Button prefab drops into a scene, and the SDK spawns the popup, in-game notification card, and notification list at runtime when needed. UI elements are themable from the Settings window or replaceable with Prefab Variants.

The SDK is split into three layers:

- **Public C# API** (`AlmediaSDK.Almedia`) - static methods and events. The only surface most host code touches.
- **UI prefabs** - Canvas-ready prefabs to instantiate or reference from the editor.
- **Native plugins** - one bridge per platform. iOS ships a single `.xcframework`; Android ships two `.aar` files (`AlmediaSDK.aar` for the SDK and `AlmediaSDKBridge.aar` for the Unity glue). Both handle HTTP, the linking-page browser surface, secure storage, and (on iOS) ATT.

---

## Threading

Every public event on `Almedia` - `OnStatusChanged`, `OnLinkCompleted`, `OnNotificationsReceived`, `OnInGameRewardGrantRequested`, `OnProgressUpdated`, `OnTaskCompleted`, `OnBalanceChanged`, `OnErrorOccurred`, `OnScreenPresented`, `OnScreenDismissed`, `OnLog` - fires on the **Unity main thread**. Handlers can touch `GetComponent`, `transform`, `UnityEngine.UI` elements, and any other Unity API directly.

Internally, the native SDKs perform I/O off the main thread and post results back via `UnitySendMessage`, which Unity always delivers on the main thread. The editor mock bridge uses coroutines, which run on the main thread as well, so the threading contract is identical in the editor and on device.

`UnitySendMessage` addresses its target by name. The SDK receives on a persistent GameObject it creates at `Initialize`, named `[Almedia SDK] Bridge` and hidden from the Hierarchy. That name is reserved: do not give any object in your project the same name, and do not rename, destroy or deactivate the SDK's object.

---

## Requirements

| Item             | Minimum         | Tested on                |
|------------------|-----------------|--------------------------|
| Unity Editor     | 2022.3 LTS      | 2022.3.62f2, 6000.4.9f1              |
| iOS              | 13.0            | 16.x, 17.x, 18.x, 26.x               |
| Android API      | 23              | API 27-37                |
| TextMeshPro      | 3.0.6           | (declared as a dependency) |

Additional requirements:

- An **iOS integration key** and an **Android integration key** issued by Almedia.
- For iOS builds: a non-empty `NSUserTrackingUsageDescription` in the final `Info.plist` (required for ATT-gated IDFA reading). The SDK writes a default value on build - see [iOS - App Tracking Transparency](#ios---app-tracking-transparency).
- For Android builds: `minSdkVersion 23` (or higher) in the Gradle template.

The SDK ships with **zero third-party runtime dependencies on the Unity side**. The native plugins pull standard AndroidX and Kotlin libraries - see [Android - Gradle dependencies](#android---gradle-dependencies).

---

## Get your integration keys

Contact your Almedia integration manager. They issue:

- An **iOS integration key** - a per-platform string identifying the host app.
- An **Android integration key** - the Android counterpart, distinct from the iOS key.

Store keys in a secret-management system; the SDK does not encrypt them at rest in the settings asset. If `AlmediaSettings.asset` is committed to source control, treat the file as semi-sensitive - the keys are not cryptographic secrets, but they should not appear in public repos.

Keys can be supplied two ways - see [Configure the SDK](#configure-the-sdk).

---

## Install the package

1. In Unity: **Window → Package Manager**.
2. Click the **+** button → **Add package from Git URL…**
3. Paste:
   ```
   https://github.com/almedia-tm/almedia-link-sdk.git
   ```
   To pin to a specific version, append `#vX.Y.Z`, e.g. `…almedia-link-sdk.git#v1.3.0`.
4. Unity downloads the package and adds it to `Packages/com.almedia.link/`.

The Package Manager also resolves the declared dependency on `com.unity.textmeshpro`.

### Verify the install

After Unity finishes importing:

- The menu bar shows an **Almedia** menu with **Settings**.
- `Assets/Almedia/Resources/AlmediaSettings.asset` exists.
- Console shows: `[Almedia] Default settings created at Assets/Almedia/Resources.`

The default settings asset is seeded once. Edits to it are never overwritten on package upgrade. The SDK only creates it when it is missing.

---

## Configure the SDK

Open **Almedia → Settings**. The Settings window has five sections:

### SDK Configuration

| Field                          | Required | Default | Notes |
|--------------------------------|:--------:|---------|-------|
| iOS Integration Key            | iOS      | empty   | Required for iOS builds. |
| Android Integration Key        | Android  | empty   | Required for Android builds. |
| Polling Interval (sec)         | no       | 30      | Frequency of notification polls when polling is enabled and the backend sets no interval. Minimum 5. |
| Enable Default Notification UI | no       | true    | When off, the SDK does not show its built-in notification card or activity overlay; host code receives `OnNotificationsReceived` and renders them. |
| Auto-Initialize From Prefabs   | no       | on      | A `LinkButton` prefab initializes the SDK with these settings when nothing else has - see [Initialize the SDK](#initialize-the-sdk). |
| Disabled Features              | no       | none    | One toggle each for Linking, Reward Hub, Offer and Notifications. Tick what this game hides from every player - see [Disabled features](#disabled-features). Enable Default Notification UI decides whose UI renders notifications; Notifications here decides whether the player gets any. |


---

## Customize the UI

The SDK ships with sensible defaults. Customization is available at three levels of depth.

### Level 1 - Text and colors

**Almedia → Settings** exposes visible string and primary color on the link popup, notification card, and activity overlay. Edit those fields directly - the built-in prefabs read the settings asset and apply it **at runtime**. No code and no writable prefab are needed. To preview your changes, enter **Play mode** - the editor mock bridge drives the UI, so the popup, notification card, and overlay render with your settings.

> The settings asset is the **default layer**. A host runtime theming or localization system (Unity Localization Package's `LocalizeStringEvent`, dynamic theming, etc.) attached to a UI element still wins, because those components initialize *after* the SDK applies the settings. To fully hand-author a prefab, see Level 2 and untick its **Apply Host Settings** checkbox so these settings are not overlaid onto it.

### Level 2 - Prefab Variants

For anything beyond text and primary colors (layout changes, custom fonts, additional elements, full re-skin):

1. Right-click the default prefab in `Packages/com.almedia.link/Runtime/Prefabs/LinkPopup.prefab` (or `NotificationCard.prefab`, `ActivityOverlay.prefab`) and choose **Create → Prefab Variant**.
2. Save the variant into a host-owned folder, for example `Assets/UI/Almedia/MyLinkPopup.prefab`.
3. Edit the variant freely. Keep the same root component (`LinkPopupController`, etc.); the SDK looks up serialized references on the root.
4. Assign the variant where the SDK takes it from:
   - **Link popup** → the `LinkButton` prefab's (or your scene instance's) **Link Popup** field. For `ShowLink()`, **Almedia → Settings → Default UI Prefabs → Link Popup**.
   - **Notification card / activity overlay** → **Almedia → Settings → Default UI Prefabs**.

The SDK instantiates the variant instead of the default. If you hand-author the variant's strings and colors, untick **Apply Host Settings** on its root so the Level 1 settings are not overlaid at runtime; leave it ticked to keep the variant themable from settings. Properties you don't change in the variant continue to inherit from the base prefab and receive SDK updates on package upgrade.

### Level 3 - Build your own UI

Disable the default notification UI in settings, then implement the visuals directly by subscribing to `OnNotificationsReceived`, `OnStatusChanged`, and `OnLinkCompleted`. The SDK still drives status, notification fetching, and linking; the host manages the UI flow.

Disabling the toggle also clears the notification prefab references to the bundled SDK prefabs, so those prefabs and their art stay out of your build. A prefab you assigned yourself stays assigned - and, like any asset your project references, still ships.

---

## Initialize the SDK

With a `LinkButton` prefab in the scene, calling `Initialize` yourself is optional (unless **Auto-Initialize From Prefabs** in **Almedia → Settings** is off). If nothing has called `Initialize` by the end of the button's first frame, the button calls it with the values from **Almedia → Settings**. With the integration keys set there, dropping a button onto a Canvas is a complete integration.

Calling `Initialize` yourself is still useful, for example to pass runtime values such as `AccountId` or advertising identifiers like `Idfa` and `Gaid`, or to control the timing, e.g. waiting for the player's consent.

For the default behavior, do nothing: drop the prefab and set the keys. To opt out, call `Initialize` yourself and optionally untick **Auto-Initialize From Prefabs**. Your explicit configuration always wins. A call in the button's first frame or earlier runs first and the prefab stands down. A later call with a different configuration re-initializes the session and your configuration applies.

Initialize once per app launch, as early as advertising identifiers and a player ID become available. Calling `Initialize` again is safe: a call with the same effective configuration is a no-op that preserves the status, while a call with a different configuration re-initializes the session. A minimal bootstrap:

```csharp
using UnityEngine;
using AlmediaSDK;

public class AlmediaBootstrap : MonoBehaviour
{
    void Awake()
    {
        Almedia.OnStatusChanged         += HandleStatusChanged;
        Almedia.OnLinkCompleted         += HandleLinkCompleted;
        Almedia.OnNotificationsReceived += HandleNotifications;
        Almedia.OnErrorOccurred         += HandleError;

        Almedia.Initialize(new AlmediaConfig
        {
            // IDs improve attribution and cover devices where automatic collection fails.
            // IDs that don't apply to the current platform are ignored.
            Idfa = "YOUR_IDFA",
            Gaid = "YOUR_GAID",
        });
    }

    void HandleStatusChanged(AlmediaStatus status) { /* ... */ }
    void HandleLinkCompleted(string linkedAt) { /* ... */ }
    void HandleNotifications(System.Collections.Generic.List<AlmediaNotification> items) { /* ... */ }
    void HandleError(AlmediaError error) { /* ... */ }
}
```

The advertising IDs and `AccountId` are runtime-only fields with no settings fallback. The native SDK collects device identifiers on its own where the platform allows. IDs you pass cover the devices where that collection comes up empty, and improve attribution everywhere.

Rules:

- **Call `Initialize` on the Unity main thread.** Callbacks from other SDKs, such as remote config or login, can run on a background thread. Move to the main thread before you call `Initialize` from one.
- **Subscribe to `OnStatusChanged` before calling `Initialize`** if you want to observe every transition (including the first one). Subscriptions added later still receive subsequent transitions; the first one is the only one you can miss.
- **For late-joining components, read `Almedia.Status` directly.** UI that spawns after init has already completed can recover the current state without waiting for the next change. The pattern is:

  ```csharp
  UpdateUi(Almedia.Status);
  Almedia.OnStatusChanged += UpdateUi;
  ```
- **Do not pass keys in code when they are already set in `AlmediaLinkSettings`.** The keys on `AlmediaConfig` are runtime overrides. A non-empty config value wins; an empty value falls back to the settings asset. The platform-correct key is selected automatically (`UNITY_IOS` → iOS key, `UNITY_ANDROID` → Android key).
- **Pass an `AccountId` when available.** This is the host's internal player ID. It is opaque to Almedia; keep its format stable so the same player is recognized across sessions.
- **Pass advertising identifiers when available.** All eight improve attribution: `AccountId`, `Idfa` (iOS), `Idfv` (iOS), `Gaid` (Android), `Asid` (Android App Set ID), `Oaid` (Huawei/CN), `AdjustDeviceId`, `AppsFlyerId`. On Android the native SDK collects identifiers on its own where the platform allows. Identifiers that don't apply to the current platform are ignored by the native SDK, so they can be set unconditionally. Pass empty strings or omit the rest when unavailable. `Idfv` is optional: when omitted the iOS SDK collects it automatically; supply a value only to override the device-issued one. `AdjustDeviceId` and `AppsFlyerId` are collected automatically too: when the Adjust or AppsFlyer SDK is in the app and the backend has enabled it for your integration, the native SDK reads the ID the vendor SDK has stored on the device, so passing them is only needed to override that value.

`TrafficSource` is where the user was originally acquired, sent as `sub3` on the linking magic link only. Pass your MMP's media source verbatim, e.g. `applovin_int` or `googleadwords_int`. It follows the same rules as meta below.

`Meta1`-`Meta4` are four optional passthrough values appended to the linking magic link and to nothing else. The SDK never interprets them; they exist for publishers who need their own payload returned in S2S reporting, and can be left unset otherwise. Each is capped at 250 UTF-8 bytes, a longer value is dropped with a warning, and changing one re-initializes the session - set them once, at startup. The API reference covers the detail.

### Config-vs-settings precedence

When `Initialize` runs, the SDK merges any supplied `config` with `AlmediaLinkSettings`:

| Field                              | Priority order |
|------------------------------------|----------------|
| Integration key                    | `config.IosIntegrationKey` / `config.AndroidIntegrationKey` → settings asset |
| `NotificationsPollingIntervalSec`  | backend (if set) → config (if non-null) → settings → 30 |
| `DisabledFeatures`                 | settings ∪ config - the asset is the floor, code adds and never removes |
| `Gaid` / `Asid` / `Oaid` / `Idfa` / `Idfv` / `AdjustDeviceId` / `AppsFlyerId` / `AccountId` / `TrafficSource` / `Meta1`-`Meta4` | config only (no settings fallback) |

If neither the config nor the settings asset supplies an integration key, `Initialize` fires `OnErrorOccurred` with `AlmediaErrorCode.InvalidConfiguration` and does not contact the backend.

---

## Disabled features

If your game hides part of the Link experience from some or all of its players - an A/B test of the entry point, a staged reward hub rollout, offers in one region only - declare it. To us an undeclared rollout looks like a broken integration: a population that saw Link and did not convert. Broken integrations get deprioritized, and holdout and funnel numbers for your game come out wrong. Declaring it costs one line and makes the backend do the hiding for you, so no entry point can lead to a screen that will not open.

Declare permanent choices in **Almedia → Settings → Disabled Features**. Declare per-player choices in code:

```csharp
Almedia.Initialize(new AlmediaConfig
{
    DisabledFeatures = { AlmediaFeature.Offer, AlmediaFeature.Notifications }
});
```

The two combine as a union: the asset is the floor for every player, code adds for the current player and can never remove what the asset declares. `DisabledFeatures` is never null and takes a collection initializer, as above.

| Feature         | What the player does not get |
|-----------------|------------------------------|
| `Linking`       | The linking entry point. Status reads `NotAvailable` with reason `Disabled`, so the bundled `LinkButton` hides. A player who already linked is untouched. |
| `RewardHub`     | The reward hub. `CanShowRewardHub` is false on a `Linked` status and `ShowRewardHub()` is a no-op. |
| `Offer`         | The offer screen. `CanShowOffer` is false on a `Linked` status and `ShowOffer()` is a no-op. |
| `Notifications` | Any Almedia notification, in the SDK's UI or yours. Reward grants still arrive. |

The set is declared at initialization only. There is no runtime call; a game whose remote config arrives late re-initializes with the new set, and `Initialize` with a different set already re-initializes. The backend enforces the declaration and the SDK reacts to the status it returns, so nothing else in your integration changes. In the Editor, `AlmediaEditorMock.EmitStatus(new AlmediaStatus.NotAvailable("disabled"))` models a player whose linking is disabled.

---

## Status and lifecycle

After `Initialize`, the SDK transitions through a small state machine. `Almedia.Status` holds the current status, and `OnStatusChanged` fires on every change:

```
NotInitialized
      │  (Initialize called, native auth in progress)
      ▼
  ┌── Eligible ─── account-linking flow available, player not linked
  │
  ├── Linked ───── player already linked at init, or just finished linking
  │
  ├── NotAvailable ─ region/eligibility check failed for this user
  │
  ├── Blocked ──── user blocked by backend (e.g. abuse, fraud)
  │
  └── Disabled ── integration killswitch - backend disabled this app
```

Each status is a nested class of [`AlmediaStatus`](./api-reference.md#almediastatus) and carries its own data. `Linked` carries `CanShowRewardHub` and `CanShowOffer`. `NotAvailable` carries `Reason` and `RawReason`. Test a status with a type pattern:

```csharp
using AlmediaSDK;

private void OnEnable()
{
    Almedia.OnStatusChanged += Refresh;
    Refresh(Almedia.Status);  // late-mount recovery
}

private void OnDisable() => Almedia.OnStatusChanged -= Refresh;

private void Refresh(AlmediaStatus status)
{
    switch (status)
    {
        case AlmediaStatus.Eligible:
            linkButton.SetActive(true);
            rewardsButton.SetActive(false);
            offerButton.SetActive(false);
            break;
        case AlmediaStatus.Linked linked:
            linkButton.SetActive(false);
            rewardsButton.SetActive(linked.CanShowRewardHub);
            offerButton.SetActive(linked.CanShowOffer);
            break;
        default:
            linkButton.SetActive(false);
            rewardsButton.SetActive(false);
            offerButton.SetActive(false);
            break;
    }
}
```

Keep the `default` branch. A later version can add statuses.

`OnStatusChanged` fires on the first status after `Initialize` and on every change after it, including a flag change while the player stays linked and a change of the `NotAvailable` reason. `Almedia.Status` already holds the new value when it fires. Use it both for one-shot setup that should happen once the SDK has resolved (skip `NotInitialized` and use a guard flag) and for live UI that mirrors the current state - the built-in `LinkButton` shows or hides itself the same way.

When `Initialize` runs again with a different configuration, the status returns to `NotInitialized`, `OnStatusChanged` reports it, and the status then resolves again.

For components that mount after `Initialize` has already completed, read `Almedia.Status` directly to recover the latest status, then subscribe to `OnStatusChanged` for further changes - see the "late-joining components" rule under the bootstrap section above.

### Why the service is not available

`NotAvailable` covers several backend decisions. Its `Reason` tells you which one:

```csharp
Almedia.OnStatusChanged += status =>
{
    if (status is AlmediaStatus.NotAvailable notAvailable)
        Analytics.Track("link_unavailable", notAvailable.Reason.ToString());
};
```

Two values are named. `Holdout`: for a small share of eligible players the Link experience is deliberately not offered, which enables continuous, statistically robust measurement of the effect of offering Link. `Disabled`: your game declared linking in its [disabled features](#disabled-features). Both are informational - use them in your statistics; there is nothing to act on. Anything this SDK version does not recognize, reads as `Unknown` in an existing build, so handle `Unknown` as a normal case rather than an error. `RawReason` keeps the reason exactly as the server sent it. Compare the reason with `==`, for example `notAvailable.Reason == AlmediaNotAvailableReason.Holdout`. See [`AlmediaNotAvailableReason`](./api-reference.md#almedianotavailablereason).

**This does not change what your UI does.** The entry point hides on `NotAvailable` whatever the reason. The reason is for analytics, support, and messaging.

### What can actually be shown

The status tells you whether the service is available to the player. It does **not** tell you whether a given screen will open. A `Linked` player can have no reward hub, and an offer can appear or disappear mid-session. `CanShowRewardHub` and `CanShowOffer` on the `Linked` status answer that question - check them before calling `ShowRewardHub()` or `ShowOffer()`. A flag change arrives as a new `Linked` status through `OnStatusChanged`, including while the player stays linked, so the `Refresh` handler above keeps every button current.

If you use the bundled `LinkButton`, this is already handled.

---

## Show the Link Button

The simplest integration is dropping one of the four prebuilt button prefabs into a Canvas. With the integration keys in **Almedia → Settings**, that is the whole integration. The button [initializes the SDK](#initialize-the-sdk) by itself when nothing else has and **Auto-Initialize From Prefabs** is on.

1. From the package, drag one of these into the scene's Canvas:
   - `Packages/com.almedia.link/Runtime/Prefabs/LinkButtonA.prefab`
   - `LinkButtonB.prefab`
   - `LinkButtonC.prefab`
   - `LinkButtonD.prefab`
2. Position it as desired.
3. Press Play. The button is two-state and stays visible as much as possible:

| Player status | Button | Tapping it |
|---|---|---|
| `Eligible` | visible - linking entry | opens the `LinkPopup` (which starts linking) |
| `Linked`, reward hub available | visible - rewards entry | opens the reward hub |
| `Linked`, no reward hub | hidden | - |
| anything else | hidden | - |

A linked player whose reward hub the backend has withdrawn gets no button rather than one that opens nothing, and the button reappears by itself if the hub comes back - it tracks [`CanShowRewardHub`](#what-can-actually-be-shown) live. Promo analytics follow what the player saw, so that case reports `promo_load` with `Hidden`.

The bundled UI needs an active `EventSystem` in the scene to receive taps. A bundled prefab shown without one logs a warning through `OnLog` that names the prefab and the scene.

For full control, ignore the prefabs and drive it yourself - see [Trigger linking programmatically](#trigger-linking-programmatically) and [Reward hub](#reward-hub).

---

## The linking flow

```
Player taps LinkButton, or your code calls ShowLink()
        │  (both open the LinkPopup)
        ▼
LinkPopup appears
        │
        ▼
Player taps CTA
        │  (LinkPopup's CTA button starts linking)
        ▼
Linking page opens (Freecash)
        │
        ▼
Player completes linking on linking page
        │
        ▼
Linking page closes; SDK syncs state (Status → Linked via OnStatusChanged)
        │
        ▼
OnScreenDismissed(Linking) fires - resume your game
        │
        ▼
OnLinkCompleted(linkedAt) fires - fresh-link celebration UX
```

The bundled flow is fully wired: `LinkButton` opens `LinkPopup`, and the popup's CTA starts linking. Host code only needs to drop a `LinkButton` prefab into a Canvas. To start the flow from your own button, see the next section.

### Trigger linking programmatically

Call `ShowLink()` to start linking from your own button. It shows the link popup, and the popup's button starts linking. The popup reports the same analytics as when the `LinkButton` opens it.

```csharp
myLinkButton.onClick.AddListener(() => Almedia.ShowLink());
```

`ShowLink()` does nothing unless the status is `Eligible`, and nothing while a link popup is already open. Show your button only while the player can link:

```csharp
private void Awake()
{
    Almedia.OnStatusChanged += OnStatus;
    OnStatus(Almedia.Status);  // late-mount recovery
}

private void OnDestroy() => Almedia.OnStatusChanged -= OnStatus;

private void OnStatus(AlmediaStatus status) =>
    myLinkButton.gameObject.SetActive(status is AlmediaStatus.Eligible);
```

The popup comes from **Almedia → Settings → Default UI Prefabs → Link Popup**. The settings asset the SDK creates on first import has the bundled popup there. An existing settings asset keeps the slot empty after the upgrade. Until you assign a popup there, `ShowLink()` shows the popup assigned to the deprecated `LinkPopupOverride`, or else starts linking directly and logs a warning through `OnLog` once. A popup assigned in the slot ships in every build. Clear the slot if you never call `ShowLink()`.

#### Your own popup design

To give the popup your own look, create a Prefab Variant of `LinkPopup` (see [Level 2 - Prefab Variants](#level-2---prefab-variants)), assign it to the **Link Popup** slot, and call `ShowLink()`.

### `OnLinkCompleted`

`OnLinkCompleted` fires **only when the player completes linking during the current session**. Players who were already linked at `Initialize` time receive a `Linked` status via `OnStatusChanged` instead. This event therefore distinguishes "they just linked, right now" from "they were already linked at launch".

Use it for fresh-link UX: a celebration toast, a one-time analytics event, unlocking a tutorial step - anything that should fire only on the transition itself. The argument is the backend's ISO-8601 timestamp of the linking event.

```csharp
Almedia.OnLinkCompleted += linkedAt =>
{
    ShowThanksForLinkingToast();
    Analytics.Track("link_succeeded", new { linkedAt });
};
```

For a basic "is the player linked right now?" check - for example, to enable a menu item or update UI affordances - use `OnStatusChanged` and react when status reaches `Linked`. That fires for both fresh links and already-linked sessions.

If linking fails or the player closes the linking page, `OnErrorOccurred` may fire with `AlmediaErrorCode.LinkingFailed`. Status stays `Eligible` so the player can try again.

---

## Reward hub

Linked players have a reward progression screen - a webview showing what they have earned and what is next. Open it with:

```csharp
Almedia.ShowRewardHub();
```

Check `CanShowRewardHub` on the `Linked` status before calling, gate your entry point on it, and refresh on `OnStatusChanged` - see [What can actually be shown](#what-can-actually-be-shown). A linked player does not always have the reward hub, and availability can change mid-session.

Calling in any state is still safe. The screen opens only for a linked player for whom the reward hub is currently available; otherwise the call does nothing and the reason is logged through `OnLog`. Before the SDK is ready (the first `OnStatusChanged`), it no-ops with a warning.

Only one in-app screen can be open at a time. A call made while another screen is already open is ignored with a log.

### Screen lifecycle

The reward hub reports its lifecycle through the unified screen events - see [Pausing your game](#pausing-your-game). The dismissal reports how it was closed:

```csharp
Almedia.OnScreenDismissed += (screen, result) =>
{
    if (screen != AlmediaScreen.RewardHub) return;
    switch (result)
    {
        case AlmediaInAppScreenResult.Completed _:   // closed by the web client
            break;
        case AlmediaInAppScreenResult.Cancelled _:   // player closed it themselves
            break;
        case AlmediaInAppScreenResult.Failed failed: // the screen could not load
            Debug.LogWarning(failed.Error.Message);
            break;
        default:
            break;
    }
};
```

Only `Failed` carries an `Error`. The SDK re-syncs the player's state **before** the dismissal fires, so state read in the handler is already up to date; an `OnStatusChanged` may arrive around the same moment.

---

## Offers

An offer is an extra monetization webview surface for a linked player. Open it with:

```csharp
Almedia.ShowOffer();
```

### Availability comes and goes

The offer screen opens only for a linked player who currently has an offer, and that can change between syncs: an offer available at launch may be gone an hour later, and one that was absent may appear. Check `CanShowOffer` on the `Linked` status before calling. Calling in any state is still safe - when there is no offer, the call does nothing and the reason is logged through `OnLog`.

Because availability is transient, **do not cache a "this player has an offer" flag of your own** - it will go stale. Gate your UI on the SDK's live snapshot instead:

```csharp
offerButton.SetActive(Almedia.Status is AlmediaStatus.Linked { CanShowOffer: true });
Almedia.OnStatusChanged += status =>
    offerButton.SetActive(status is AlmediaStatus.Linked { CanShowOffer: true });
```

The event fires whenever the flag changes, also while the player stays linked - see [What can actually be shown](#what-can-actually-be-shown). Availability can still change between rendering the button and the tap; such a tap does nothing and is harmless.

### Offer lifecycle

The offer screen reports its lifecycle through the same unified events, carrying `AlmediaScreen.Offer` and the same [`AlmediaInAppScreenResult`](./api-reference.md#almediainappscreenresult) semantics as the reward hub:

```csharp
Almedia.OnScreenDismissed += (screen, result) =>
{
    if (screen == AlmediaScreen.Offer && result is AlmediaInAppScreenResult.Failed failed)
        Debug.LogWarning(failed.Error.Message);
};
```

Only one in-app screen can be open at a time - calling `ShowOffer()` while the reward hub (or the linking flow) is showing is ignored with a log, and no lifecycle event fires for the ignored call.

---

## Pausing your game

Every SDK screen - the linking webview, the reward hub, and offers - covers the game while it is open. The unified lifecycle pair tells you exactly when to pause and resume, regardless of what opened the screen (an API method or the `LinkButton` prefab):

```csharp
Almedia.OnScreenPresented += _ => PauseGame();
Almedia.OnScreenDismissed += (_, _) => ResumeGame();
```

The contract that makes this safe to wire one-to-one to pause/resume:

- **Matched pairs.** A screen that appears fires exactly one `OnScreenPresented` and, later, exactly one `OnScreenDismissed`. Never two presents in a row, never a dismiss without a present.
- **No-ops fire nothing.** A call that opens nothing (wrong state, missing URL, another screen already open) fires neither event, so the game never pauses for a screen that never appeared.
- **Presented fires early.** It fires when the native container commits to presenting, before the page loads - the pause lands before the player sees the screen.
- **Dismissed fires after the sync.** Native completes its sync-on-close first, so `Almedia.Status` and related state are already up to date in the handler. For webview linking, the dismissal also fires before the outcome link callbacks (`OnLinkCompleted` etc.).
- **System-browser linking is excluded.** When linking runs in the system browser, the app is backgrounded by the OS rather than covered - use the regular application lifecycle for that case; only the link callbacks fire.

The `screen` argument ([`AlmediaScreen`](./api-reference.md#almediascreen)) identifies which screen it was, for analytics or per-screen UX.

Both events fire on the Unity main thread, like every other SDK event - see [Threading](#threading).

---

## Reward notifications

Once the player is linked, the SDK polls the Almedia backend for reward notifications.

### Polling

Polling starts automatically once the player's status reaches `Linked`. The native layer drives the loop; no manual start from host code is required. The loop runs at the interval the backend sets. When the backend sets none, it runs at your configured interval (`AlmediaConfig.NotificationsPollingIntervalSec` at runtime, or `AlmediaLinkSettings.NotificationPollIntervalSeconds` on the asset; default 30s, minimum 5s). It runs on the foreground only. The native plugin pauses polling when the app backgrounds and resumes when it returns to foreground.

Host code only interacts with polling for opt-in pause and resume:

`StopNotificationPolling()` halts the loop. Common use: stop while showing a cinematic or full-screen ad, restart after.

`StartNotificationPolling()` resumes the loop after a `StopNotificationPolling()` call.

`FetchNotifications()` performs a one-shot fetch outside the loop. Useful from a "Refresh" button or after a known reward-granting event. The backend can hold the request for up to a minute until a message arrives, so the result can take that long.

> `OnNotificationsReceived` fires only when at least one notification comes back. An empty result (the player has none) fires nothing, so "fetched and got zero" is indistinguishable from "the fetch has not returned yet" without your own state tracking. In particular, a "clear the badge on refresh" pattern will not fire on an empty result - clear host-side state when you *issue* the fetch, not from the callback.

#### Composition rules

The three methods compose as follows. The native plugins enforce these rules; no guards are needed on the host side.

| Call                                          | If polling is running              | If polling is stopped         |
|-----------------------------------------------|------------------------------------|-------------------------------|
| `StartNotificationPolling()`                  | No-op. The schedule is unchanged - the loop is not double-scheduled, the interval is not shortened, no immediate poll is triggered. | Starts the loop. The first tick fires after one interval. |
| `StopNotificationPolling()`                   | Stops the loop. Any in-flight request is allowed to complete; its result (if any) still arrives on `OnNotificationsReceived`. | No-op. |
| `FetchNotifications()`                        | Fires the request immediately **and** resets the polling clock so the next scheduled tick happens at `now + interval`, not at the originally planned time. This prevents a near-duplicate poll moments after a host-driven refresh. | Fires the request immediately. Does not start the loop. |

Practical consequence: calling `FetchNotifications()` on a user action (e.g. opening a notifications drawer) is safe and free of duplicates. The host doesn't need to `Stop`, `Fetch`, `Start` defensively.

### Default UI

When `AlmediaLinkSettings.EnableDefaultNotificationUI` is on (the default), the SDK creates a screen-space-overlay Canvas at the top of the sorting order and renders incoming notifications using two prefabs:

- **NotificationCard** - a single card that slides up from the bottom and auto-dismisses after about 4 seconds. The dismiss delay is a serialized field on the prefab (`_dismissDelay`), so you can adjust the timing directly in the Inspector - the same way as Bottom Padding, and without needing a Prefab Variant. When more than one notification arrives in the same batch, the card shows the latest and displays a stacked indicator.
- **ActivityOverlay** - tapping the stacked indicator opens a full list of the most recent notifications (up to 3).

The row icon is the sprite authored on the bundled `NotificationRow` prefab. Change it with a Prefab Variant - see [Customize the UI](#customize-the-ui).

### Custom UI

Disable the default UI in **Almedia → Settings → SDK Configuration → Enable Default Notification UI**, then handle notifications directly:

```csharp
Almedia.OnNotificationsReceived += notifications => {
    foreach (var n in notifications)
        MyToastSystem.Show(n.Title, n.Message);
};
```

`AlmediaNotification` is a plain data class with `Id`, `Title`, `Message`, `Timestamp` (raw ISO 8601 string from the backend), `ReceivedAt` (parsed UTC `DateTimeOffset?`, ready for "5 minutes ago" math), `Display` (the presentation hint, `"popup"` or `"tray"`), and `IconUrl` (absolute icon URL, or `null` when the backend sent none).

For relative-time labels in your own UI, use `ReceivedAt` directly:

```csharp
if (n.ReceivedAt.HasValue)
{
    var elapsed = System.DateTimeOffset.UtcNow - n.ReceivedAt.Value;
    label.text = elapsed.TotalMinutes < 1 ? "now" : $"{(int)elapsed.TotalMinutes}min ago";
}
```

---

## In-game reward grants

The backend can instruct the game to grant in-game rewards. Subscribe, credit the player, celebrate:

```csharp
Almedia.OnInGameRewardGrantRequested += grant =>
{
    foreach (var reward in grant.Rewards)
        Wallet.Credit(reward.Code, reward.Amount);
};
```

There is nothing extra to start. Grants travel with notifications, so `FetchNotifications()` fetches them and `StopNotificationPolling()` suspends them.

### One event per grant

A grant is an atomic bundle. "250 gems and 3 spins for finishing the chapter" arrives as a single event with one id and two entries in `Rewards`. Show one celebration for it. Three separate grants arriving together raise three events.

`Amount` is a `double` and may carry fractions, so do not truncate it to an integer. `Code` is the reward code agreed with Almedia, for example `"gems"`. Treat a code you do not recognize as a no-op rather than an error.

### Delivery is at-least-once

The same grant can arrive more than once. The server-to-server reward postback remains the authoritative record of what was granted; reconcile against it if your economy needs certainty.

Each grant has a unique `Id`, and a redelivery carries the same one. Deduplicate on it when a repeat credit matters:

```csharp
private readonly HashSet<string> _credited = new HashSet<string>();

Almedia.OnInGameRewardGrantRequested += grant =>
{
    if (!_credited.Add(grant.Id)) return;   // redelivered, already credited
    foreach (var reward in grant.Rewards)
        Wallet.Credit(reward.Code, reward.Amount);
};
```

A short memory of recent ids is enough.

### Testing without a backend

`AlmediaEditorMock.EmitInGameRewardGrant` delivers a grant to your handler. Call it twice with one id to reproduce a redelivery and prove your dedup holds:

```csharp
var rewards = new[]
{
    new AlmediaInGameReward(250, "gems"),
    new AlmediaInGameReward(3, "spins")
};
AlmediaEditorMock.EmitInGameRewardGrant("grant-1", rewards);
AlmediaEditorMock.EmitInGameRewardGrant("grant-1", rewards);   // must credit once
```

See [Driving non-happy paths](#driving-non-happy-paths) for the mock's rules.

---

## Progress and rewards

A linked player earns coins on Freecash when they complete tasks in your game. The SDK keeps the latest view of that progress. It also tells you when the server reports an event. You build one progress UI and get the same state and events on iOS and Android.

The surface is one accessor, three events and some plain data classes. You do not start anything. Progress travels with the message stream that also carries notifications and grants.

### The snapshot

`Almedia.Progress` is the latest snapshot, or `null` when the SDK has none. `OnProgressUpdated` fires each time the accessor changes: with the newer snapshot, or with `null` when the snapshot is cleared. The accessor is already current when the event fires. Render from the argument or from the accessor, and hide the panel on `null`:

```csharp
void OnEnable()
{
    Render(Almedia.Progress);                     // a scene that loads late still sees current state; null before the first snapshot
    Almedia.OnProgressUpdated += Render;
}

void OnDisable() => Almedia.OnProgressUpdated -= Render;

void Render(AlmediaProgress progress)
{
    if (progress == null) { panel.SetActive(false); return; }

    panel.SetActive(true);
    nameLabel.text    = progress.Username ?? "Player";      // null when the server sent no username
    balanceLabel.text = $"{progress.Balance.Coins:N0} coins";
    earnedLabel.text  = FormatMoney(progress.Earned.InPlayerCurrency);

    list.Clear();
    foreach (var task in progress.Pending)
        list.AddPending(task.Title, task.Progress, task.Reward);   // Progress is null for a yes-or-no task
    foreach (var done in progress.Completed)                        // newest first
        list.AddCompleted(done.Task.Title, done.CompletedAt, done.ActualReward);
}
```

A snapshot is never partial. A newer snapshot replaces the previous one completely. Do not keep state from a previous snapshot. `Username` follows the same rule. If a newer snapshot has no username, `Username` is `null`. Clear a name you showed before.

The snapshot lives in memory only. The native SDK clears it together with its message stream token, for example when the account changes or the server refuses the token, and `OnProgressUpdated` then fires with `null`. It is also `null` after `Initialize` with a different configuration. A `null` snapshot means "no progress to show", both before the first snapshot and after a clear.

### Rewards that change

The reward of a burning task (`AlmediaTaskKind.Burning` or `AlmediaTaskKind.BurningPurchaseBonus`) decreases on a schedule. `RewardDropsAt` is the time of the next decrease, or `null` when none is scheduled. Show a countdown from it. You do not need to check `Kind`:

```csharp
if (task.RewardDropsAt is DateTimeOffset dropsAt)
{
    var left = dropsAt - DateTimeOffset.UtcNow;
    countdownLabel.text = left > TimeSpan.Zero ? $"Reward drops in {(int)left.TotalHours}h {left.Minutes}m" : "";
}
```

After a decrease, the task keeps its `Id`. The next snapshot carries the lower `Reward` and the next `RewardDropsAt`. The paid reward is the amount at completion. `AlmediaCompletedTask.ActualReward` reports it, so it can be lower than the reward the game showed earlier.

`Kind` groups similar tasks. [`AlmediaTaskKind`](./api-reference.md#almediataskkind) lists the known values. The set is open: the server can add a kind without a package update, so show an unknown value as `AlmediaTaskKind.Main`.

### Task completions and balance changes

Use `OnTaskCompleted` and `OnBalanceChanged` for effects: a celebration, a coin animation, a sound. Each event carries the server's message id. `OnTaskCompleted` carries the completion and the reward it paid. `OnBalanceChanged` carries the balance after the change and the signed change:

```csharp
Almedia.OnTaskCompleted += completion =>
    Celebrate(completion.Task.Task.Title, completion.Task.ActualReward.Coins);

Almedia.OnBalanceChanged += change =>
    AnimateCoins(from: change.Balance.Coins - change.Change.Coins, to: change.Balance.Coins);
```

The events describe things that happened. Delivery is best-effort. Design for three consequences:

- **The events do not update the snapshot.** `Progress` changes only when a newer snapshot arrives. A snapshot can arrive before an event, after an event, or without a matching event. Render state from the snapshot. Use the events for effects.
- **An event is not always in the snapshot.** The server limits the `Completed` list, so a completed task can be missing from it. A balance in an event can differ from the balance in the snapshot. Do not look up one in the other.
- **The SDK can replay an event.** This can also happen after the SDK resets its stream token. A replay carries the same `Id`. Keep a short list of recent ids if a repeated effect is a problem:

```csharp
private readonly HashSet<string> _celebrated = new HashSet<string>();

Almedia.OnTaskCompleted += completion =>
{
    if (!_celebrated.Add(completion.Id)) return;
    Celebrate(completion.Task.Task.Title, completion.Task.ActualReward.Coins);
};
```

### AlmediaMoney

Coins are the canonical unit. Each `AlmediaRewardPoints` also carries two display values, `InPlayerCurrency` and `InUsd`. Select the value that fits the game. `AlmediaMoney.Amount` is a `decimal`. The SDK parses it from the wire string, so it is exact and independent of the device locale. `AlmediaMoney.Currency` is the ISO 4217 code. The SDK does not format money. Format it with your own symbols, decimals and fonts:

```csharp
string FormatMoney(AlmediaMoney money)
{
    var culture = CultureInfo.GetCultureInfo(money.Currency == "EUR" ? "de-DE" : "en-US");
    return money.Amount.ToString("N2", culture) + " " + money.Currency;    // "12,50 EUR" / "12.50 USD"
}
```

Each SDK version supports a closed set of player currencies. This release supports `USD`, `EUR`, `GBP`, `CAD`, `AUD`, `PLN`, `CHF`, `KRW`, `JPY` and `SEK`. You can write your formatting against this list. If the player's currency is not in the list, `InPlayerCurrency` is the USD value with the `"USD"` code. The code above then gives a correct label.

### Testing without a backend

`AlmediaEditorMock.EmitProgress` applies a snapshot that you build with the public constructors, or clears the snapshot when you pass `null`. `EmitTaskCompleted` and `EmitBalanceChanged` fire the two events. Emit two snapshots, the second without a username, to see the name clear. Emit `null` to see the panel hide. Emit one completion twice with the same id to test your deduplication:

```csharp
var points = new AlmediaRewardPoints(12500, new AlmediaMoney(11.50m, "EUR"), new AlmediaMoney(12.50m, "USD"));
var task = new AlmediaTask("t-1", AlmediaTaskKind.Burning, "Reach level 10", points, new AlmediaTaskProgress(3, 10),
    DateTimeOffset.UtcNow.AddHours(2).ToString("o"));                        // RewardDropsAt in two hours
var done = new AlmediaCompletedTask(task, "2026-09-14T09:00:00.000Z", points);

AlmediaEditorMock.EmitProgress(new AlmediaProgress("s-1", "2026-09-14T10:00:00.000Z", "demo_player",
    points, points, new[] { task }, new[] { done }, new AlmediaTask[0]));
AlmediaEditorMock.EmitProgress(new AlmediaProgress("s-2", "2026-09-14T10:01:00.000Z", null,
    points, points, new[] { task }, new[] { done }, new AlmediaTask[0]));    // Username is now null
AlmediaEditorMock.EmitProgress(null);                                    // Progress is now null

AlmediaEditorMock.EmitTaskCompleted("m-1", done);
AlmediaEditorMock.EmitTaskCompleted("m-1", done);                         // must celebrate once
AlmediaEditorMock.EmitBalanceChanged("m-2", points, new AlmediaRewardPoints(-100, new AlmediaMoney(-0.09m, "EUR"), new AlmediaMoney(-0.10m, "USD")));
```

In the editor, the mock delivers a sample snapshot after the simulated link flow and with each `FetchNotifications()`. A progress panel shows data as soon as the player links, without test code.

---

## iOS - App Tracking Transparency

Almedia attribution benefits from access to IDFA, which on iOS is gated behind Apple's App Tracking Transparency (ATT). The native SDK reads IDFA only after ATT authorization; the Unity SDK shows no consent UI of its own.

### `Info.plist`

Any ATT usage requires a non-empty `NSUserTrackingUsageDescription` string in the final `Info.plist`. The SDK provides an iOS post-build hook that:

- Runs **after all other SDKs' post-processors**, so it observes the final merged `Info.plist`.
- Adds `NSUserTrackingUsageDescription` with a default string ("Tracking lets us credit your rewards correctly and faster.") **only if** neither the host app nor another SDK has already set the key.
- Logs to the Unity console when it adds the key, and when it leaves an existing value untouched.

To use custom copy, set `NSUserTrackingUsageDescription` directly via **Player Settings → iOS → Other Settings → Custom Info.plist entries** (or any other plugin). The SDK detects the existing value and does not overwrite it.

---

## iOS - App Store privacy labels

The SDK ships a `PrivacyInfo.xcprivacy` that Xcode aggregates into your app automatically. You still need to reflect the SDK's data collection in your **App Store Connect → App Privacy** declarations, otherwise Apple will flag the mismatch during review.

The SDK collects:

| Category | Data type | Linked to identity | Used for tracking | Purposes |
|---|---|---|---|---|
| Identifiers | Device ID | Yes | Yes | App Functionality, Third-Party Advertising |
| Identifiers | User ID | Yes | No | App Functionality |
| Usage Data | Product Interaction | Yes | No | Analytics |

**Device ID** covers IDFV (always) and IDFA (when the player grants ATT). **User ID** applies only if you pass an `AccountId` in config - skip it if you don't. **Product Interaction** covers SDK analytics events (session, linking, notifications).

If your app already declares any of these for its own reasons, don't duplicate - just make sure the existing entry includes the purposes and attributes above.

---

## Android - Gradle dependencies

The native AAR pulls in standard AndroidX and Kotlin libraries:

- `org.jetbrains.kotlinx:kotlinx-coroutines-core:1.10.2`
- `org.jetbrains.kotlinx:kotlinx-coroutines-android:1.10.2`
- `org.jetbrains.kotlinx:kotlinx-serialization-json:1.10.0`
- `androidx.lifecycle:lifecycle-runtime-ktx:2.6.2`
- `androidx.lifecycle:lifecycle-process:2.6.2`
- `androidx.datastore:datastore-preferences:1.0.0`

**Host projects do not configure any of this.** The SDK ships these in a Unity `.androidlib` subproject at `Packages/com.almedia.link/Plugins/Android/AlmediaSDK.androidlib/`. Unity links the subproject into the generated Gradle project automatically; the dependencies resolve as part of the standard Android build.

This works with and without EDM4U:

- **Without EDM4U:** the `.androidlib` is the sole source. no Gradle template edits.
- **With EDM4U:** EDM4U also resolves `Packages/com.almedia.link/Editor/AlmediaSDKDependencies.xml` and injects the same Maven coordinates. Gradle dedupes by coordinate, so there is one copy of each library in the final APK.

### Build-tools / compile SDK

The `.androidlib`'s `build.gradle` reads `unity.compileSdkVersion` and `unity.buildToolsVersion` Gradle properties when present (Unity 6 / 2023.3 LTS+ injects them), so it always matches the SDK components Unity bundled. On Unity 2022.3, which doesn't inject these properties, it falls back to `compileSdk 34` and `buildToolsVersion 34.0.0` — the values that ship with current 2022.3 patches.

### `minSdkVersion`

The `.androidlib` declares `minSdk 23`. Host apps with a lower **Minimum API Level** (Player Settings → Android → Other Settings) will fail at manifest merge with a clear error.

---

## Logging

The SDK's runtime logging is **a no-op by default**. Runtime logs route through the static event `Almedia.OnLog`; when nothing is subscribed, they are discarded and nothing reaches the Unity Console. To see them, wire up the event explicitly:

```csharp
Almedia.OnLog += (level, message) =>
{
    if (level == AlmediaLogLevel.Error) Debug.LogError(message);
    else if (level == AlmediaLogLevel.Warning) Debug.LogWarning(message);
    else Debug.Log(message);
};
```

`AlmediaLogLevel` values, in ascending severity: `Verbose`, `Debug`, `Info`, `Warning`, `Error`. Compare them with `==`, or order them with `<`, `<=`, `>` and `>=`.

The levels are chosen so that `Error` is safe to route into a crash reporter:

| Level     | What it means |
|-----------|---------------|
| `Error`   | Something worth a look: the backend rejected a request (4xx), a response could not be decoded, or work the game asked for (`Initialize`, linking, opening a screen) failed. |
| `Warning` | The SDK handled it. This includes ordinary connectivity loss - offline, DNS, timeouts, captive portals - in work the SDK runs on its own: notification polling, event delivery, status refreshes. One line per failed operation; the SDK retries by itself. |
| `Debug`   | Individual retry attempts and request traces. |

Logs from the native plugins (iOS and Android) are forwarded into the same event by the bridge, so a single stream covers the entire runtime SDK. Route it into an existing pipeline (Sentry, custom analytics, etc.) by handling the event.

**Editor-only logs are an exception.** The settings bootstrap and the iOS post-build hook write directly to the Unity Console; they are design-time feedback and bypass `OnLog` entirely.

---

## Error handling

Subscribe to `OnErrorOccurred` to surface SDK failures:

```csharp
Almedia.OnErrorOccurred += err =>
{
    Debug.LogError($"[Almedia] {err.Code}: {err.Message}");
};
```

Every error that reaches this event is one the game may want to know about, so routing it to `Debug.LogError` or a crash reporter is fine. Connectivity loss during work the SDK runs on its own - notification polling, event delivery, status refreshes, and a `FetchNotifications()` that finds no network - is logged at `Warning`, retried by the SDK, and never fires this event. A device that loses its signal after start-up produces no errors. Only `Initialize` still reports offline, because the game is waiting on its first status: `Disabled` when the configuration cannot be fetched, `NetworkFailure` when the status that follows it cannot.

| `AlmediaErrorCode`     | Source           | Typical meaning |
|------------------------|------------------|-----------------|
| `InvalidConfiguration` | Local or native  | Missing integration key, malformed config. Fired synchronously from `Initialize` when keys are absent. |
| `NetworkFailure`       | Native           | A request the game asked for (`Initialize`, linking, opening a screen) did not reach the backend: offline, DNS, timeout, TLS. The same failure in the SDK's own background work is a `Warning` log, not an error. |
| `ServerError`          | Native           | The backend returned 5xx. |
| `RateLimited`          | Native           | The backend returned 429. Retry later. |
| `Disabled`             | Native           | Integration killswitch is on for this app. The SDK does not run until the backend re-enables it. |
| `LinkingFailed`        | Native           | The linking flow ended without success. The player can retry. |
| `InvalidState`         | Native           | A method was called in a state it does not support (for example, starting linking while already `Linked`). |
| `Unexpected`           | Native           | Catch-all for unhandled native exceptions. |
| `Unknown`              | Native           | The native side sent an error code the C# layer does not recognize; usually a version mismatch. |

Compare codes with `==`, for example `err.Code == AlmediaErrorCode.RateLimited`. Errors are non-fatal. The SDK keeps running and may recover on the next status update or fetch.

Do not retry `Initialize` directly from `OnErrorOccurred`. `Initialize` raises its own configuration errors before it returns, so the handler runs inside `Initialize`. The SDK ignores that retry and logs a `Warning`. Fix the configuration and retry from a later frame. The same configuration fails the same way.

---

## Crash symbolication

The native plugins ship with symbols so Almedia frames in crash reports deobfuscate inside the host's crash reporter (Crashlytics, Sentry, etc.).

### iOS

dSYMs are bundled inside `AlmediaSDK.xcframework`. Xcode picks them up automatically and folds them into the `.xcarchive`. An existing symbol-upload pipeline ships them to Apple or the crash reporter with no extra step.

### Android

Two `mapping.txt` files are attached to each GitHub Release, one per `.aar`:

- `AlmediaSDK-android-sdk-mapping-<version>.txt` - for `AlmediaSDK.aar`
- `AlmediaSDK-android-bridge-mapping-<version>.txt` - for `AlmediaSDKBridge.aar`

Download both and upload to the crash reporter alongside the host app's own mapping file.

---

## Editor and play mode

`Almedia.Initialize` works inside the Unity Editor by falling back to a deterministic mock bridge that simulates the native layer. The mock fires `OnStatusChanged` / `OnNotificationsReceived` on a timer, so the UI can be exercised without a device build. It also simulates the [screen lifecycle pair](#pausing-your-game) for every screen, modeling the **webview** linking strategy - starting linking from the popup's CTA fires `OnScreenPresented`/`OnScreenDismissed` around the simulated flow so pause/resume wiring is exercisable in the editor, whereas production configured with the system-browser linking strategy fires no pair for linking.

Static state and event subscribers are cleared on domain reload (`RuntimeInitializeOnLoadMethod` with `SubsystemRegistration` timing). This means:

- Manual unsubscription in `OnDestroy` is not required to avoid leaking handlers *across play-mode entry and exit* - domain reload clears them. **Within a single play session you must still unsubscribe**, though: a `MonoBehaviour` that subscribes to an `Almedia` event and is then destroyed (scene unload, `Destroy(gameObject)`) stays registered. The next callback fires on the destroyed object and throws `MissingReferenceException`, which the bridge swallows to `OnLog` - it does **not** surface through `OnErrorOccurred`. Unsubscribe in `OnDisable`/`OnDestroy` for any component shorter-lived than the SDK.
- Even with domain reload disabled in **Edit → Project Settings → Editor → Enter Play Mode Options**, subscriptions should still happen in `Awake`, because the SDK explicitly resets its own state on the same lifecycle hook.

### Driving non-happy paths

EditorMock auto-simulate covers the happy path only: `Eligible` after `Initialize`, then `Linked` + `OnLinkCompleted` and a first progress snapshot after linking starts, then three mock notifications and a new snapshot when `FetchNotifications` is called. To exercise the rest of the surface - `NotAvailable` / `Blocked` / `Disabled` statuses, every `AlmediaErrorCode`, empty or oversized notification batches, native log forwarding - reach for `AlmediaEditorMock`. It lets host editor code and play-mode tests drive the SDK into any state in one line, so UI branches keyed off those signals become Editor-testable without a device build.

The public surface lives in `AlmediaSDK.Editor.Testing` and consists of these static methods:

- `EmitStatus(AlmediaStatus)` - delivers a status as if native had reported it; `Almedia.Status` and `OnStatusChanged` reflect it immediately. Build the status with its constructor: `EmitStatus(new AlmediaStatus.Linked(false, false))` is a linked player with nothing to open, and `EmitStatus(new AlmediaStatus.NotAvailable("holdout"))` is a holdout player.
- `EmitError(AlmediaErrorCode, string)` - delivers an error; `OnErrorOccurred` fires with the matching code and message.
- `EmitLinkCompleted()` - fires `OnLinkCompleted` with the current UTC timestamp.
- `EmitNotifications(params AlmediaNotification[])` - fires `OnNotificationsReceived`. Pass no arguments for an empty batch; pass many to test scrolling / paging.
- `EmitInGameRewardGrant(string id, params AlmediaInGameReward[])` - fires `OnInGameRewardGrantRequested`. The id is optional (an overload generates one); pass the same id twice to reproduce an at-least-once redelivery. A call with no rewards is dropped by the SDK as malformed.
- `EmitProgress(AlmediaProgress)` - applies a snapshot. `Progress` holds it and `OnProgressUpdated` fires. `null` models the native SDK clearing the snapshot. A null `Username` models a snapshot that clears the previous username.
- `EmitTaskCompleted(string id, AlmediaCompletedTask)` / `EmitBalanceChanged(string id, AlmediaRewardPoints balance, AlmediaRewardPoints change)` - fire the two progress events. The id is optional. Pass the same id twice to reproduce a replay.
- `EmitScreenPresented(AlmediaScreen)` - fires `OnScreenPresented` for the given screen, without a real webview.
- `EmitScreenDismissed(AlmediaScreen, AlmediaInAppScreenResult)` - fires `OnScreenDismissed` for the given screen and result.
- `EmitNativeLog(AlmediaLogLevel, string)` - delivers a forwarded log line through the same path the iOS/Android plugins use.
- `CancelPending()` - stops any pending auto-simulate coroutine started before manual mode flipped.

**Manual mode.** The mock has two modes: auto-simulate (default; canned coroutines drive the happy path) and manual (every `Initialize`, linking start and `FetchNotifications` becomes a no-op so the test owns the scenario). The first call to *any* `AlmediaEditorMock` method flips manual mode for the rest of the play session and cancels any in-flight canned coroutine, so an `EmitStatus(new AlmediaStatus.Blocked())` issued right after `Almedia.Initialize` cannot be clobbered by the delayed `Eligible`. Manual mode resets on domain reload (entering Play mode, recompiling) - there is no public toggle to leave it manually.

**Pre-init contract.** Calling any `Emit*` method before `Almedia.Initialize(...)` throws `InvalidOperationException`. The throw is deliberate: it surfaces test ordering bugs at test-author time instead of swallowing them as a silent no-op.

**Stripping.** `AlmediaEditorMock` lives in the `AlmediaLink.Editor` assembly definition with `includePlatforms:["Editor"]`. It is not compiled for iOS or Android player targets at the assembly level - host code that references it must be wrapped in `#if UNITY_EDITOR`, and the references will fail to compile on a player target rather than crash at runtime.

A minimal play-mode test that verifies UI hides on `Blocked`:

```csharp
#if UNITY_EDITOR
using System.Collections;
using AlmediaSDK;
using AlmediaSDK.Editor.Testing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class RewardsHudPlayModeTests
{
    [UnityTest]
    public IEnumerator RewardsHud_HidesWhenSdkBlocked()
    {
        var hud = Object.Instantiate(Resources.Load<GameObject>("RewardsHud"));

        Almedia.Initialize(new AlmediaConfig
        {
            IosIntegrationKey = "test", AndroidIntegrationKey = "test"
        });
        AlmediaEditorMock.EmitStatus(new AlmediaStatus.Blocked());

        yield return null;

        Assert.IsFalse(hud.activeSelf, "RewardsHud should hide on Blocked status");
    }
}
#endif
```

The package README has a [shorter pointer to the same surface](../README.md#editor-testing); the API reference covers each method in the [Editor testing](./api-reference.md#editor-testing) section.

---

## Troubleshooting

**The Almedia menu does not appear after install.**
Wait for the package import to finish. If the menu is still missing, check the Console for compile errors from the SDK assembly; the most common cause is a host project on Unity 2021 or older. Upgrade to Unity 2022.3 LTS.

**The Console shows `Assembly with name 'AlmediaLink' already exists` and `GUID … conflicts with` errors.**
Both `com.almedia.link` and `com.almedia.sdk` are in `Packages/manifest.json`. Remove the `com.almedia.link` line. See [Upgrade the package](./migration-guide.md#upgrade-the-package).

**`AlmediaSettings.asset` is missing.**
The SDK copies the default asset once after import. If that did not happen, force it by opening **Almedia → Settings** from the menu bar; the window creates the asset if it is not present.

**Android build fails with "duplicate class" or D8 dexing errors.**
A conflicting version of an AndroidX or Kotlin library is likely present. Most often this means another plugin pulled a newer `androidx.datastore:datastore-preferences` past the SDK's 1.0.0 pin — inspect the generated Gradle output (Library/Bee/Android) and check the resolved version. If a stale `// >>> almedia-link deps` block from an older SDK version is still in `Assets/Plugins/Android/mainTemplate.gradle`, delete it (the SDK no longer manages that block — deps come from `AlmediaSDK.androidlib`). If EDM4U is in use, force a re-resolve via **Assets → External Dependency Manager → Android Resolver → Resolve**.

**iOS build is missing `NSUserTrackingUsageDescription`.**
The SDK's post-build hook adds a default value on every iOS build (unless the host or another SDK already set it). If it's still missing, confirm another post-processor isn't stripping it; you can always set the key explicitly via **Player Settings → iOS → Other Settings → Custom Info.plist entries**.

**Status never leaves `NotInitialized`.**
Confirm `OnErrorOccurred` is not firing first with `InvalidConfiguration`. If both are silent, subscribe to `OnLog` and look for `Initializing SDK` / `Status changed: …` lines. When the log shows `Status changed: NotInitialized` but nothing else, the native HTTP request has not returned. The most common causes are an offline device or an integration key from a different environment.

**The status never leaves `NotInitialized`, and `OnLog` reports that a GameObject named `[Almedia SDK] Bridge` already exists.**
Another GameObject in your project carries the name the SDK reserves for its callback receiver. Native code delivers every callback to that name, and Unity may hand it to your object instead of the SDK's, so `OnStatusChanged` never arrives and nothing appears. The log line names the object and its scene. Rename your object; the SDK's own object is created for you and needs no setup.

**Notifications never arrive after the player links.**
Confirm the player's status is `Linked`. Polling only runs for linked accounts. In the Editor, the mock bridge emits sample notifications on a timer; on device, a real linked Freecash account with issued rewards is required.

**The notification card appears too low or overlaps the bottom HUD.**
The `NotificationCard` prefab has a Bottom Padding field in the Inspector that controls how far above the bottom edge the card rests. To shift it permanently, create a Prefab Variant (see [Customize the UI - Level 2](#level-2---prefab-variants)) and adjust the value or the RectTransform anchors.

For anything not covered here, see the [API reference](./api-reference.md) or contact your Almedia integration manager.
