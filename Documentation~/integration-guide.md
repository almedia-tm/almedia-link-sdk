# Almedia Link SDK - Integration Guide

This guide walks through integrating the Almedia Link SDK into a Unity game, from a fresh install through to a working linking flow and reward notifications. The companion document is the [API reference](./api-reference.md).

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
9. [Status and lifecycle](#status-and-lifecycle)
10. [Show the Link Button](#show-the-link-button)
11. [The linking flow](#the-linking-flow)
12. [Reward notifications](#reward-notifications)
13. [iOS - App Tracking Transparency](#ios--app-tracking-transparency)
14. [Android - Gradle dependencies](#android--gradle-dependencies)
15. [Logging](#logging)
16. [Error handling](#error-handling)
17. [Crash symbolication](#crash-symbolication)
18. [Editor and play mode](#editor-and-play-mode)
19. [Troubleshooting](#troubleshooting)

---

## Overview

The Almedia Link SDK lets players connect a Freecash account from inside a Unity game and surfaces reward notifications as they arrive. The linking flow runs in a secure native browser managed by the iOS and Android plugins. The rest is Unity UI: a Link Button prefab drops into a scene, and the SDK spawns the popup, in-game notification card, and notification list at runtime when needed. UI elements are themable from the Settings window or replaceable with Prefab Variants.

The SDK is split into three layers:

- **Public C# facade** (`AlmediaLink.AlmediaLinkSDK`) - static methods and events. The only surface most host code touches.
- **UI prefabs** - Canvas-ready prefabs to instantiate or reference from the editor.
- **Native plugins** - one bridge per platform. iOS ships a single `.xcframework`; Android ships two `.aar` files (`AlmediaLinkSDK.aar` for the SDK and `AlmediaLinkBridge.aar` for the Unity glue). Both handle HTTP, the linking-page browser surface, secure storage, and (on iOS) ATT.

---

## Threading

Every public event on `AlmediaLinkSDK` - `OnStatusChanged`, `OnLinkCompleted`, `OnNotificationsReceived`, `OnErrorOccurred`, `OnLog` - fires on the **Unity main thread**. Handlers can touch `GetComponent`, `transform`, `UnityEngine.UI` elements, and any other Unity API directly.

Internally, the native SDKs perform I/O off the main thread and post results back via `UnitySendMessage`, which Unity always delivers on the main thread. The editor mock bridge uses coroutines, which run on the main thread as well, so the threading contract is identical in the editor and on device.

---

## Requirements

| Item             | Minimum         | Tested on                |
|------------------|-----------------|--------------------------|
| Unity Editor     | 2022.3 LTS      | 2022.3.62f2              |
| iOS              | 16.0            | 17.x, 18.x               |
| Android API      | 27              | API 27-34                |
| TextMeshPro      | 3.0.6           | (declared as a dependency) |

Additional requirements:

- An **iOS integration key** and an **Android integration key** issued by Almedia.
- For iOS builds that run the consent flow: a non-empty `NSUserTrackingUsageDescription` in the final `Info.plist`. The SDK can write a default value - see [iOS - App Tracking Transparency](#ios--app-tracking-transparency).
- For Android builds: `minSdkVersion 27` (or higher) in the Gradle template.

The SDK ships with **zero third-party runtime dependencies on the Unity side**. The native plugins pull standard AndroidX and Kotlin libraries - see [Android - Gradle dependencies](#android--gradle-dependencies).

---

## Get your integration keys

Contact your Almedia integration manager. They issue:

- An **iOS integration key** - a per-platform string identifying the host app.
- An **Android integration key** - the Android counterpart, distinct from the iOS key.

Store keys in a secret-management system; the SDK does not encrypt them at rest in the settings asset. If `AlmediaLinkSettings.asset` is committed to source control, treat the file as semi-sensitive - the keys are not cryptographic secrets, but they should not appear in public repos.

Keys can be supplied two ways - see [Configure the SDK](#configure-the-sdk).

---

## Install the package

1. In Unity: **Window → Package Manager**.
2. Click the **+** button → **Add package from Git URL…**
3. Paste:
   ```
   https://github.com/almedia-tm/almedia-link-sdk.git
   ```
   To pin to a specific version, append `#vX.Y.Z`, e.g. `…almedia-link-sdk.git#v1.0.1`.
4. Unity downloads the package and adds it to `Packages/com.almedia.link/`.

The Package Manager also resolves the declared dependency on `com.unity.textmeshpro`.

### Verify the install

After Unity finishes importing:

- The menu bar shows an **Almedia** menu with **Settings**.
- `Assets/AlmediaLink/Resources/AlmediaLinkSettings.asset` exists.
- `Assets/AlmediaLink/Resources/NotificationIconMap.asset` exists.
- Console shows: `[AlmediaLink] Default settings created at Assets/AlmediaLink/Resources.`

Both default assets are seeded once. Edits to them are never overwritten on package upgrade; the SDK only creates files that are missing.

---

## Configure the SDK

Open **Almedia → Settings**. The Settings window has five sections:

### SDK Configuration

| Field                          | Required | Default | Notes |
|--------------------------------|:--------:|---------|-------|
| iOS Integration Key            | iOS      | empty   | Required for iOS builds. |
| Android Integration Key        | Android  | empty   | Required for Android builds. |
| Polling Interval (sec)         | no       | 30      | Frequency of notification polls when polling is enabled. Minimum 5. |
| Enable Default Notification UI | no       | true    | When off, the SDK does not show its built-in notification card or activity overlay; host code receives `OnNotificationsReceived` and renders them. |
| Enable Consent Flow (iOS ATT)  | no       | false   | When on, iOS runs the ATT pre-prompt and system dialog as part of init. See [iOS - App Tracking Transparency](#ios--app-tracking-transparency). |


---

## Customize the UI

The SDK ships with sensible defaults. Customization is available at three levels of depth.

### Level 1 - Text and colors

**Almedia → Settings** exposes visible string and primary color on the link popup, notification card, activity overlay, and ATT pre-prompt. Edit those fields directly - the built-in prefabs read the settings asset and apply it **at runtime**. No code and no writable prefab are needed. To preview your changes, enter **Play mode** - the editor mock bridge drives the UI, so the popup, notification card, and overlay render with your settings.

> The settings asset is the **default layer**. A host runtime theming or localization system (Unity Localization Package's `LocalizeStringEvent`, dynamic theming, etc.) attached to a UI element still wins, because those components initialize *after* the SDK applies the settings. To customize a **replaced** prefab, see Level 2 - an assigned variant is authored directly and is not overlaid with these settings.

### Level 2 - Prefab Variants

For anything beyond text and primary colors (layout changes, custom fonts, additional elements, full re-skin):

1. Right-click the default prefab in `Packages/com.almedia.link/Runtime/Resources/Prefabs/LinkPopup.prefab` (or `NotificationCard.prefab`, `ActivityOverlay.prefab`, `ATTPrePrompt.prefab`) and choose **Create → Prefab Variant**.
2. Save the variant into a host-owned folder, for example `Assets/UI/Almedia/MyLinkPopup.prefab`.
3. Edit the variant freely. Keep the same root component (`LinkPopupController`, etc.); the SDK looks up serialized references on the root.
4. **Almedia → Settings → Prefab Overrides** → assign the variant in the corresponding slot.

The SDK loads the variant instead of the default. An assigned variant is treated as host-owned: the SDK does **not** overlay the Level 1 text/colors onto it at runtime, so set the strings and colors you want directly on the variant (or drive them with your own runtime components). Properties you don't change in the variant continue to inherit from the base prefab and receive SDK updates on package upgrade.

### Level 3 - Build your own UI

Disable the default notification UI in settings, then implement the visuals directly by subscribing to `OnNotificationsReceived`, `OnStatusChanged`, and `OnLinkCompleted`. The SDK still drives status, notification fetching, and linking; the host manages the UI flow.

### Notification icons

`Assets/AlmediaLink/Resources/NotificationIconMap.asset` maps notification `Type` strings (for example, `"reward"`, `"status"`) to sprites.

When the `NotificationCard` and `ActivityOverlay` prefabs are replaced with custom controllers, the icon map is no longer consulted; resolve icons however the host prefers.

---

## Initialize the SDK

Initialize once per app launch, as early as advertising identifiers and a player ID become available. Calling `Initialize` again is safe: a call with the same effective configuration is a no-op that preserves `CurrentStatus`, while a call with a different configuration re-initializes the session. A minimal bootstrap:

```csharp
using UnityEngine;
using AlmediaLink;
using AlmediaLink.Models;

public class AlmediaBootstrap : MonoBehaviour
{
    void Awake()
    {
        AlmediaLinkSDK.OnStatusChanged += HandleStatusChanged;
        AlmediaLinkSDK.OnLinkCompleted += HandleLinkCompleted;
        AlmediaLinkSDK.OnNotificationsReceived += HandleNotifications;
        AlmediaLinkSDK.OnErrorOccurred         += HandleError;

        AlmediaLinkSDK.Initialize(new AlmediaLinkConfig
        {
            // Android needs at least one of: Gaid, Asid, Oaid, AdjustDeviceId, AppsFlyerId.
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

The advertising IDs and `AccountId` are runtime-only fields with no settings fallback. The integration key can live in **Almedia → Settings** instead of code, but the config itself cannot be skipped on Android: initialization succeeds only when at least one device/advertising ID (`Gaid`, `Asid`, `Oaid`, `AdjustDeviceId`, or `AppsFlyerId`) is supplied.

Rules:

- **Subscribe to `OnStatusChanged` before calling `Initialize`** if you want to observe every transition (including the first one). Subscriptions added later still receive subsequent transitions; the first one is the only one you can miss.
- **For late-joining components, read `AlmediaLinkSDK.CurrentStatus` directly.** UI that spawns after init has already completed can recover the current state without waiting for the next transition. The pattern is:

  ```csharp
  if (AlmediaLinkSDK.CurrentStatus != AlmediaStatus.NotInitialized)
      UpdateUi(AlmediaLinkSDK.CurrentStatus);

  AlmediaLinkSDK.OnStatusChanged += UpdateUi;
  ```
- **Do not pass keys in code when they are already set in `AlmediaLinkSettings`.** The keys on `AlmediaLinkConfig` are runtime overrides. A non-empty config value wins; an empty value falls back to the settings asset. The platform-correct key is selected automatically (`UNITY_IOS` → iOS key, `UNITY_ANDROID` → Android key).
- **Pass an `AccountId` when available.** This is the host's internal player ID. It is opaque to Almedia; keep its format stable so the same player is recognized across sessions.
- **Pass advertising identifiers when available.** All eight improve attribution: `AccountId`, `Idfa` (iOS), `Idfv` (iOS), `Gaid` (Android), `Asid` (Android App Set ID), `Oaid` (Huawei/CN), `AdjustDeviceId`, `AppsFlyerId`. On Android, the native SDK requires at least one of `Gaid`, `Asid`, `Oaid`, `AdjustDeviceId`, or `AppsFlyerId` - initialization fails without one. Identifiers that don't apply to the current platform are ignored by the native SDK, so they can be set unconditionally. Pass empty strings or omit the rest when unavailable. `Idfv` is optional: when omitted the iOS SDK collects it automatically; supply a value only to override the device-issued one.

### Config-vs-settings precedence

When `Initialize` runs, the SDK merges any supplied `config` with `AlmediaLinkSettings`:

| Field                              | Priority order |
|------------------------------------|----------------|
| Integration key                    | `config.IosIntegrationKey` / `config.AndroidIntegrationKey` → settings asset |
| `NotificationsPollingIntervalSec`  | config (if non-null) → settings → 30 |
| `CanRunConsentFlow`                | config (if non-null) → settings → false |
| `Gaid` / `Asid` / `Oaid` / `Idfa` / `Idfv` / `AdjustDeviceId` / `AppsFlyerId` / `AccountId` | config only (no settings fallback) |

If neither the config nor the settings asset supplies an integration key, `Initialize` fires `OnErrorOccurred` with `AlmediaErrorCode.InvalidConfiguration` and does not contact the backend.

---

## Status and lifecycle

After `Initialize`, the SDK transitions through a small state machine. Subscribe to `OnStatusChanged` to track it:

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

`OnStatusChanged(AlmediaStatus status)` fires on every transition out of `NotInitialized` and between terminal states. Use it both for one-shot setup that should happen once the SDK has resolved (gate on `status != NotInitialized` and a guard flag) and for live UI that mirrors the current state - the built-in `LinkButton` subscribes to it and shows or hides itself as `Eligible` enters or leaves.

For components that mount after `Initialize` has already completed, read `AlmediaLinkSDK.CurrentStatus` directly to recover the latest status, then subscribe to `OnStatusChanged` for further transitions - see the "late-joining components" rule under the bootstrap section above.

---

## Show the Link Button

The simplest integration is dropping one of the four prebuilt button prefabs into a Canvas.

1. From the package, drag one of these into the scene's Canvas:
   - `Packages/com.almedia.link/Runtime/Resources/Prefabs/LinkButtonA.prefab`
   - `LinkButtonB.prefab`
   - `LinkButtonC.prefab`
   - `LinkButtonD.prefab`
2. Position it as desired.
3. Press Play. The button stays hidden until the SDK status becomes `Eligible`. It hides itself again if the player completes linking, gets blocked, or becomes ineligible.

Tapping the button opens the `LinkPopup`. The popup handles the rest of the linking flow.

For full control, ignore the prefabs and call `AlmediaLinkSDK.StartLinking()` from a custom button - see the next section.

---

## The linking flow

```
Player taps LinkButton
        │  (LinkButton opens the LinkPopup internally; no host code involved)
        ▼
LinkPopup appears
        │
        ▼
Player taps CTA
        │  (LinkPopup's CTA button calls AlmediaLinkSDK.StartLinking)
        ▼
Linking page opens (Freecash)
        │
        ▼
Player completes linking on linking page
        │
        ▼
Status → Linked, OnLinkCompleted(linkedAt) fires
```

The bundled flow is fully wired: `LinkButton` opens `LinkPopup`, and the popup's CTA invokes `StartLinking()`. Host code only needs to drop a `LinkButton` prefab into a Canvas. To replace the popup with custom UI, see the next section.

### Trigger linking programmatically

`AlmediaLinkSDK.StartLinking(placement)` accepts a `PlacementType` that tags the linking attempt for analytics. Behavior is identical across placements; the linking flow opens the same way regardless.

Default placement (popup modal):

```csharp
AlmediaLinkSDK.StartLinking();
```

Linking initiated from a rewards or store UI:

```csharp
AlmediaLinkSDK.StartLinking(PlacementType.RewardHub);
```

Linking initiated from an in-game banner ad slot:

```csharp
AlmediaLinkSDK.StartLinking(PlacementType.Banner);
```

### `OnLinkCompleted`

`OnLinkCompleted` fires **only when the player completes linking during the current session**. Players who were already linked at `Initialize` time receive `AlmediaStatus.Linked` via `OnStatusChanged` instead. This event therefore distinguishes "they just linked, right now" from "they were already linked at launch".

Use it for fresh-link UX: a celebration toast, a one-time analytics event, unlocking a tutorial step - anything that should fire only on the transition itself. The argument is the backend's ISO-8601 timestamp of the linking event.

```csharp
AlmediaLinkSDK.OnLinkCompleted += linkedAt =>
{
    ShowThanksForLinkingToast();
    Analytics.Track("link_succeeded", new { linkedAt });
};
```

For a basic "is the player linked right now?" check - for example, to enable a menu item or update UI affordances - use `OnStatusChanged` and react when status reaches `Linked`. That fires for both fresh links and already-linked sessions.

If linking fails or the player closes the linking page, `OnErrorOccurred` may fire with `AlmediaErrorCode.LinkingFailed`. Status stays `Eligible` so the player can try again.

---

## Reward notifications

Once the player is linked, the SDK polls the Almedia backend for reward notifications.

### Polling

Polling starts automatically once the player's status reaches `Linked`. The native layer drives the loop; no manual start from host code is required. The loop runs at the configured interval (`AlmediaLinkConfig.NotificationsPollingIntervalSec` at runtime, or `AlmediaLinkSettings.NotificationPollIntervalSeconds` on the asset; default 30s, minimum 5s) on the foreground only. The native plugin pauses polling when the app backgrounds and resumes when it returns to foreground.

Host code only interacts with polling for opt-in pause and resume:

`StopNotificationPolling()` halts the loop. Common use: stop while showing a cinematic or full-screen ad, restart after.

`StartNotificationPolling()` resumes the loop after a `StopNotificationPolling()` call.

`FetchNotifications()` performs a one-shot fetch outside the loop. Useful from a "Refresh" button or after a known reward-granting event.

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

Icons come from the `NotificationIconMap` asset, looked up by the notification's `Type` - see [Customize the UI](#customize-the-ui).

### Custom UI

Disable the default UI in **Almedia → Settings → SDK Configuration → Enable Default Notification UI**, then handle notifications directly:

```csharp
AlmediaLinkSDK.OnNotificationsReceived += notifications => {
    foreach (var n in notifications)
        MyToastSystem.Show(n.Title, n.Message, iconForType(n.Type));
};
```

`AlmediaNotification` is a plain data class with `Id`, `Title`, `Message`, `Timestamp` (raw ISO 8601 string from the backend), `ReceivedAt` (parsed UTC `DateTimeOffset?`, ready for "5 minutes ago" math), and `Type` (an opaque category string such as `"reward"` or `"status"`).

For relative-time labels in your own UI, use `ReceivedAt` directly:

```csharp
if (n.ReceivedAt.HasValue)
{
    var elapsed = System.DateTimeOffset.UtcNow - n.ReceivedAt.Value;
    label.text = elapsed.TotalMinutes < 1 ? "now" : $"{(int)elapsed.TotalMinutes}min ago";
}
```

---

## iOS - App Tracking Transparency

Almedia attribution benefits from access to IDFA, which on iOS requires the player to grant tracking through Apple's ATT system dialog.

### Pre-prompt and system dialog

When `CanRunConsentFlow` is enabled (via `AlmediaLinkConfig.CanRunConsentFlow=true` or **Almedia → Settings → Enable Consent Flow**), the native layer requests the SDK's **ATT pre-prompt** screen mid-init. The pre-prompt is a Unity UI prefab; it explains *why* tracking matters for rewards before Apple's system dialog appears. Tapping **Continue** triggers the OS dialog. Closing the pre-prompt without continuing counts as a dismissal. The native layer throttles re-prompts internally; host code does not configure the throttle.

### `Info.plist`

When `CanRunConsentFlow` is on, iOS requires a non-empty `NSUserTrackingUsageDescription` string in the final `Info.plist`. The SDK provides a post-build hook that:

- Runs late in the Xcode-project post-process (callback order 100).
- Adds `NSUserTrackingUsageDescription` with a default string ("Tracking lets us credit your rewards correctly and faster.") **only if** the host app has not already set the key.
- Logs to the Unity console when it adds the key, and when it leaves an existing value untouched.

To use custom copy, set `NSUserTrackingUsageDescription` directly via **Player Settings → iOS → Other Settings → Custom Info.plist entries** (or any other plugin). The SDK detects the existing value and does not overwrite it.

When `CanRunConsentFlow` is off, the SDK does not modify `Info.plist`.

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

**Host projects do not configure any of this.** The SDK ships these in a Unity `.androidlib` subproject at `Packages/com.almedia.link/Plugins/Android/AlmediaLink.androidlib/`. Unity links the subproject into the generated Gradle project automatically; the dependencies resolve as part of the standard Android build.

This works with and without EDM4U:

- **Without EDM4U:** the `.androidlib` is the sole source. no Gradle template edits.
- **With EDM4U:** EDM4U also resolves `Packages/com.almedia.link/Editor/AlmediaLinkDependencies.xml` and injects the same Maven coordinates. Gradle dedupes by coordinate, so there is one copy of each library in the final APK.

### Build-tools / compile SDK

The `.androidlib`'s `build.gradle` reads `unity.compileSdkVersion` and `unity.buildToolsVersion` Gradle properties when present (Unity 6 / 2023.3 LTS+ injects them), so it always matches the SDK components Unity bundled. On Unity 2022.3, which doesn't inject these properties, it falls back to `compileSdk 34` and `buildToolsVersion 34.0.0` — the values that ship with current 2022.3 patches.

### `minSdkVersion`

The `.androidlib` declares `minSdk 27`. Host apps with a lower **Minimum API Level** (Player Settings → Android → Other Settings) will fail at manifest merge with a clear error. The native AAR genuinely requires 27+, so this is enforced rather than absorbed.

---

## Logging

The SDK's runtime logging is **a no-op by default**. Runtime logs route through the static event `AlmediaLinkSDK.OnLog`; when nothing is subscribed, they are discarded and nothing reaches the Unity Console. To see them, wire up the event explicitly:

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

`AlmediaLogLevel` values, in ascending severity: `Verbose`, `Debug`, `Info`, `Warning`, `Error`.

Logs from the native plugins (iOS and Android) are forwarded into the same event by the bridge, so a single stream covers the entire runtime SDK. Route it into an existing pipeline (Sentry, custom analytics, etc.) by handling the event.

**Editor-only logs are an exception.** The settings bootstrap and the iOS post-build hook write directly to the Unity Console; they are design-time feedback and bypass `OnLog` entirely.

---

## Error handling

Subscribe to `OnErrorOccurred` to surface SDK failures:

```csharp
AlmediaLinkSDK.OnErrorOccurred += err =>
{
    Debug.LogError($"[Almedia] {err.Code}: {err.Message}");
};
```

| `AlmediaErrorCode`     | Source           | Typical meaning |
|------------------------|------------------|-----------------|
| `InvalidConfiguration` | Local or native  | Missing integration key, malformed config. Fired synchronously from `Initialize` when keys are absent. |
| `NetworkFailure`       | Native           | The native HTTP request did not reach the backend (offline, DNS, etc.). |
| `ServerError`          | Native           | The backend returned 5xx. |
| `RateLimited`          | Native           | The backend returned 429. Retry later. |
| `Disabled`             | Native           | Integration killswitch is on for this app. The SDK does not run until the backend re-enables it. |
| `LinkingFailed`        | Native           | The linking flow ended without success. The player can retry. |
| `InvalidState`         | Native           | A method was called in a state it does not support (for example, `StartLinking` while already `Linked`). |
| `Unexpected`           | Native           | Catch-all for unhandled native exceptions. |
| `Unknown`              | Native           | The native side sent an error code the C# layer does not recognize; usually a version mismatch. |

Errors are non-fatal. The SDK keeps running and may recover on the next status update or fetch.

---

## Crash symbolication

The native plugins ship with symbols so Almedia frames in crash reports deobfuscate inside the host's crash reporter (Crashlytics, Sentry, etc.).

### iOS

dSYMs are bundled inside `AlmediaLinkSDK.xcframework`. Xcode picks them up automatically and folds them into the `.xcarchive`. An existing symbol-upload pipeline ships them to Apple or the crash reporter with no extra step.

### Android

Two `mapping.txt` files are attached to each GitHub Release, one per `.aar`:

- `AlmediaLink-android-sdk-mapping-<version>.txt` - for `AlmediaLinkSDK.aar`
- `AlmediaLink-android-bridge-mapping-<version>.txt` - for `AlmediaLinkBridge.aar`

Download both and upload to the crash reporter alongside the host app's own mapping file.

---

## Editor and play mode

`AlmediaLinkSDK.Initialize` works inside the Unity Editor by falling back to a deterministic mock bridge that simulates the native layer. The mock fires `OnStatusChanged` / `OnNotificationsReceived` on a timer, so the UI can be exercised without a device build.

Static state and event subscribers are cleared on domain reload (`RuntimeInitializeOnLoadMethod` with `SubsystemRegistration` timing). This means:

- Manual unsubscription in `OnDestroy` is not required to avoid leaking handlers *across play-mode entry and exit* - domain reload clears them. **Within a single play session you must still unsubscribe**, though: a `MonoBehaviour` that subscribes to an `AlmediaLinkSDK` event and is then destroyed (scene unload, `Destroy(gameObject)`) stays registered. The next callback fires on the destroyed object and throws `MissingReferenceException`, which the bridge swallows to `OnLog` - it does **not** surface through `OnErrorOccurred`. Unsubscribe in `OnDisable`/`OnDestroy` for any component shorter-lived than the SDK.
- Even with domain reload disabled in **Edit → Project Settings → Editor → Enter Play Mode Options**, subscriptions should still happen in `Awake`, because the SDK explicitly resets its own state on the same lifecycle hook.

### Driving non-happy paths

EditorMock auto-simulate covers the happy path only: `Eligible` after `Initialize`, then `Linked` + `OnLinkCompleted` after `StartLinking`, then three mock notifications when `FetchNotifications` is called. To exercise the rest of the surface - `NotAvailable` / `Blocked` / `Disabled` statuses, every `AlmediaErrorCode`, empty or oversized notification batches, the ATT pre-prompt, native log forwarding - reach for `AlmediaLinkEditorMock`. It lets host editor code and play-mode tests drive the SDK into any state in one line, so UI branches keyed off those signals become Editor-testable without a device build.

The public surface lives in `AlmediaLink.Editor.Testing` and consists of seven static methods:

- `EmitStatus(AlmediaStatus)` - delivers a status transition; `AlmediaLinkSDK.CurrentStatus` and `OnStatusChanged` reflect it immediately.
- `EmitError(AlmediaErrorCode, string)` - delivers an error; `OnErrorOccurred` fires with the matching code and message.
- `EmitLinkCompleted()` - fires `OnLinkCompleted` with the current UTC timestamp.
- `EmitNotifications(params MockNotification[])` - fires `OnNotificationsReceived`. Pass no arguments for an empty batch; pass many to test scrolling / paging.
- `EmitShowATTPrePrompt()` - triggers the ATT pre-prompt UI flow as if the native iOS layer had requested it.
- `EmitNativeLog(AlmediaLogLevel, string)` - delivers a forwarded log line through the same path the iOS/Android plugins use.
- `CancelPending()` - stops any pending auto-simulate coroutine started before manual mode flipped.

**Manual mode.** The mock has two modes: auto-simulate (default; canned coroutines drive the happy path) and manual (every `Initialize` / `StartLinking` / `FetchNotifications` / `ContinueWithATT` / `SkipATT` becomes a no-op so the test owns the scenario). The first call to *any* `AlmediaLinkEditorMock` method flips manual mode for the rest of the play session and cancels any in-flight canned coroutine, so an `EmitStatus(Blocked)` issued right after `AlmediaLinkSDK.Initialize` cannot be clobbered by the delayed `Eligible`. Manual mode resets on domain reload (entering Play mode, recompiling) - there is no public toggle to leave it manually.

**Pre-init contract.** Calling any `Emit*` method before `AlmediaLinkSDK.Initialize(...)` throws `InvalidOperationException`. The throw is deliberate: it surfaces test ordering bugs at test-author time instead of swallowing them as a silent no-op.

**Stripping.** `AlmediaLinkEditorMock` lives in the `AlmediaLink.Editor` assembly definition with `includePlatforms:["Editor"]`. It is not compiled for iOS or Android player targets at the assembly level - host code that references it must be wrapped in `#if UNITY_EDITOR`, and the references will fail to compile on a player target rather than crash at runtime.

A minimal play-mode test that verifies UI hides on `Blocked`:

```csharp
#if UNITY_EDITOR
using System.Collections;
using AlmediaLink;
using AlmediaLink.Editor.Testing;
using AlmediaLink.Models;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class RewardsHudPlayModeTests
{
    [UnityTest]
    public IEnumerator RewardsHud_HidesWhenSdkBlocked()
    {
        var hud = Object.Instantiate(Resources.Load<GameObject>("RewardsHud"));

        AlmediaLinkSDK.Initialize(new AlmediaLinkConfig
        {
            IosIntegrationKey = "test", AndroidIntegrationKey = "test"
        });
        AlmediaLinkEditorMock.EmitStatus(AlmediaStatus.Blocked);

        yield return null;

        Assert.IsFalse(hud.activeSelf, "RewardsHud should hide on Blocked status");
    }
}
#endif
```

The package README has a [shorter pointer to the same surface](../README.md#editor-testing-non-happy-paths); the API reference covers each method in the [Editor testing](./api-reference.md#editor-testing) section.

---

## Troubleshooting

**The Almedia menu does not appear after install.**
Wait for the package import to finish. If the menu is still missing, check the Console for compile errors from the SDK assembly; the most common cause is a host project on Unity 2021 or older. Upgrade to Unity 2022.3 LTS.

**`AlmediaLinkSettings.asset` is missing.**
The SDK copies the default asset once after import. If that did not happen, force it by opening **Almedia → Settings** from the menu bar; the window creates the asset if it is not present.

**Android build fails with "duplicate class" or D8 dexing errors.**
A conflicting version of an AndroidX or Kotlin library is likely present. Most often this means another plugin pulled a newer `androidx.datastore:datastore-preferences` past the SDK's 1.0.0 pin — inspect the generated Gradle output (Library/Bee/Android) and check the resolved version. If a stale `// >>> almedia-link deps` block from an older SDK version is still in `Assets/Plugins/Android/mainTemplate.gradle`, delete it (the SDK no longer manages that block — deps come from `AlmediaLink.androidlib`). If EDM4U is in use, force a re-resolve via **Assets → External Dependency Manager → Android Resolver → Resolve**.

**iOS build is missing `NSUserTrackingUsageDescription`.**
The post-build hook only runs when `CanRunConsentFlow` is enabled. When ATT is required for other reasons but the Almedia consent flow is disabled, add the key directly in **Player Settings → iOS → Other Settings → Custom Info.plist entries**.

**Status never leaves `NotInitialized`.**
Confirm `OnErrorOccurred` is not firing first with `InvalidConfiguration`. If both are silent, subscribe to `OnLog` and look for `Initializing SDK` / `Status changed: …` lines. When the log shows `Status changed: NotInitialized` but nothing else, the native HTTP request has not returned. The most common causes are an offline device or an integration key from a different environment.

**Notifications never arrive after the player links.**
Confirm the player's status is `Linked`. Polling only runs for linked accounts. In the Editor, the mock bridge emits sample notifications on a timer; on device, a real linked Freecash account with issued rewards is required.

**The notification card appears too low or overlaps the bottom HUD.**
The `NotificationCard` prefab has a Bottom Padding field in the Inspector that controls how far above the bottom edge the card rests. To shift it permanently, create a Prefab Variant (see [Customize the UI - Level 2](#level-2--prefab-variants)) and adjust the value or the RectTransform anchors.

For anything not covered here, see the [API reference](./api-reference.md) or contact your Almedia integration manager.
