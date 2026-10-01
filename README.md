# Almedia SDK for Unity

The Almedia SDK connects your Unity game to Freecash Link, letting players earn real cash rewards for playing, which lifts engagement, retention, and LTV. It's a drop-in integration with zero third-party dependencies on the Unity side.

Mechanically, the SDK lets players connect a Freecash account from inside a host app, checks whether the service is available in their region, and surfaces reward notifications as they arrive. It also exposes the player's progress (balance, earnings and tasks) for the game to render. The linking flow runs in a secure native browser; the rest (popups, buttons, notification cards) is Unity UI that blends with the host game.

> [!NOTE]
> **Moving to the Almedia SDK (`com.almedia.sdk`)**
>
> 1.3.0 is the last release of `com.almedia.link`. Later releases ship only as `com.almedia.sdk`. To move, replace the `com.almedia.link` line in `Packages/manifest.json` with `"com.almedia.sdk": "https://github.com/almedia-tm/almedia-sdk.git"`. Never install both packages. Your code keeps working, and the settings asset moves to its new folder on the first editor load. The [migration guide](./Documentation~/migration-guide.md) covers the upgrade and moving your code to `Almedia`.

---

## How it's built

The SDK has three layers:

- **C# API** (`Almedia`, in the `AlmediaSDK` namespace) - the only surface host code touches.
- **UI prefabs** - link button, popup, notification card, and activity overlay, all themable from **Almedia > Settings** or replaceable as Prefab Variants.
- **Native plugins** - one bridge per platform. iOS ships a single `.xcframework`; Android ships two AARs (`AlmediaSDK.aar` for the SDK and `AlmediaSDKBridge.aar` for the Unity glue). Zero third-party runtime dependencies on the Unity side.

---

## Platform Support

| Platform | Minimum version | Tested on    |
|----------|------------------|--------------|
| Unity    | 2022.3 LTS       | 2022.3.62f2, 6000.4.9f1  |
| iOS      | 13.x             | 16.x, 17.x, 18.x, 26.x |
| Android  | API 23           | API 25-37    |

The native plugins pull standard AndroidX and Kotlin libraries - see the [integration guide](./Documentation~/integration-guide.md#android---gradle-dependencies).

---

## Installation

In Unity: **Window > Package Manager > + > Add package from Git URL...** and paste:

```
https://github.com/almedia-tm/almedia-link-sdk.git
```

Append `#vX.Y.Z` to pin a specific release, e.g. `...almedia-link-sdk.git#v1.3.0`.

After install, open **Almedia > Settings** and fill in the iOS and Android integration keys.

---

## Quick Start

The shortest integration is three steps: install the package, set the integration keys in **Almedia → Settings**, and drag a `LinkButton` prefab onto a Canvas. The button initializes the SDK by itself using the settings values.

Initialize in code when you have runtime values to pass:

```csharp
using UnityEngine;
using AlmediaSDK;

public class AlmediaBootstrap : MonoBehaviour
{
    void Awake()
    {
        Almedia.Initialize(new AlmediaConfig
        {
            // IDs improve attribution and cover devices where automatic collection fails.
            // IDs that don't apply to the current platform are ignored.
            Idfa = "YOUR_IDFA",
            Gaid = "YOUR_GAID",
        });
    }
}
```

> The snippet above is the minimum bootstrap. For the full event surface see [Initialize the SDK](./Documentation~/integration-guide.md#initialize-the-sdk) in the integration guide.

All SDK events fire on the Unity main thread. See the [integration guide](./Documentation~/integration-guide.md#threading) for details.

The native SDK collects device identifiers on its own. Pass IDs to cover the devices where it cannot, and use the same config to supply `AccountId` and the other identifiers when available.

If your game hides part of Link from some players - an A/B test of the entry point, a staged reward hub rollout - declare it, in **Almedia → Settings → Disabled Features** or with `DisabledFeatures` on the config. The backend then does the hiding, and our reporting can tell your rollout from a broken integration. See [Disabled features](./Documentation~/integration-guide.md#disabled-features).

Drop one of the `LinkButton` prefabs (`LinkButtonA`, `LinkButtonB`, `LinkButtonC`, or `LinkButtonD`) into a Canvas. It shows itself as a linking entry while the player is eligible and as a rewards entry once they are linked, and hides in every other case - including a linked player whose reward hub the backend has withdrawn, so it never offers a screen that would not open. When nothing has called `Initialize` and **Auto-Initialize From Prefabs** is on, the button does it itself - see [Initialize the SDK](./Documentation~/integration-guide.md#initialize-the-sdk).

To start linking from your own button instead, call `Almedia.ShowLink()`. It shows the popup from **Almedia > Settings > Default UI Prefabs > Link Popup** to a player who can link, or starts linking directly while that slot is empty. It does nothing for other players. Show your button while `Almedia.Status` is `AlmediaStatus.Eligible`. See [Trigger linking programmatically](./Documentation~/integration-guide.md#trigger-linking-programmatically).

---

## Customization

Three levels of depth, from least to most invasive. Full detail in [Customize the UI](./Documentation~/integration-guide.md#customize-the-ui).

1. **Text and colors** - edit the strings and primary colors in **Almedia > Settings**. The built-in prefabs read the settings asset at runtime, so this works on a read-only UPM install.
2. **Prefab Variants** - create a Prefab Variant of any bundled UI prefab for full re-skin / layout control. Assign a popup variant on the LinkButton's **Link Popup** field. Assign the popup variant for `ShowLink()`, and card and overlay variants, under **Almedia > Settings > Default UI Prefabs**.
3. **Bring your own UI** - disable the default notification UI in settings and subscribe to `OnNotificationsReceived`, `OnStatusChanged`, and `OnLinkCompleted` to render your own visuals. The SDK still drives status, polling, and linking.

You also render a linked player's progress. `Almedia.Progress` holds the latest snapshot: balance, lifetime earnings, pending and completed tasks, and username. `OnProgressUpdated` fires when a newer snapshot arrives. `OnTaskCompleted` and `OnBalanceChanged` report events for celebrations. Money arrives as exact `decimal` values in the player's currency and in USD. The SDK does not format money. See [Progress and rewards](./Documentation~/integration-guide.md#progress-and-rewards).

---

## Editor testing

In the Unity Editor and play-mode tests, drive the SDK into any state via `AlmediaEditorMock`:

```csharp
#if UNITY_EDITOR
using AlmediaSDK;
using AlmediaSDK.Editor.Testing;

Almedia.Initialize(new AlmediaConfig { /* test keys */ });
AlmediaEditorMock.EmitStatus(new AlmediaStatus.Blocked());
AlmediaEditorMock.EmitStatus(new AlmediaStatus.NotAvailable("holdout"));   // holdout player
AlmediaEditorMock.EmitStatus(new AlmediaStatus.Linked(false, false));      // linked, nothing to open
AlmediaEditorMock.EmitError(AlmediaErrorCode.NetworkFailure, "Mock: backend unreachable");
AlmediaEditorMock.EmitNotifications(); // empty fetch
#endif
```

Surface: `EmitStatus`, `EmitError`, `EmitLinkCompleted`, `EmitNotifications`, `EmitInGameRewardGrant`, `EmitProgress`, `EmitTaskCompleted`, `EmitBalanceChanged`, `EmitScreenPresented`, `EmitScreenDismissed`, `EmitNativeLog`, `CancelPending`.

- **Editor-only.** Lives in `AlmediaSDK.Editor.Testing`, in the `AlmediaLink.Editor` assembly (`includePlatforms:["Editor"]`); not present in iOS/Android player builds. Wrap host references in `#if UNITY_EDITOR` so your own code still compiles for device targets.
- **Manual mode.** The first call to any `AlmediaEditorMock` method puts the bridge into manual mode for the rest of the play session — the auto-simulate coroutines that fire `Eligible`/`linked` for the happy path stop firing, so they can't race against your emits. Manual mode resets on domain reload.
- **Calling before `Almedia.Initialize` throws** `InvalidOperationException`.

## Documentation

- **[Integration Guide](./Documentation~/integration-guide.md)** - install, configure, lifecycle, iOS ATT, Android dependencies, UI customization, troubleshooting.
- **[API Reference](./Documentation~/api-reference.md)** - every public type, method, event, and settings field.
- **[Migration Guide](./Documentation~/migration-guide.md)** - upgrading from `com.almedia.link`, and moving code from the `AlmediaLink` API to `Almedia`.
- **[AlmediaLink API Reference](./Documentation~/almedialink-api-reference.md)** - the `AlmediaLink` API of releases before 1.3.0.
- **[Changelog](./CHANGELOG.md)** - version history.

---

## Crash Symbols

Both native SDKs ship with symbol files so frames inside Almedia code deobfuscate in the host's crash reporter:

- **iOS dSYMs** - bundled inside `AlmediaSDK.xcframework`. Xcode folds them into the app's `.xcarchive` automatically; an existing symbol-upload pipeline picks them up without any extra step.
- **Android mapping files** - two assets published on each GitHub Release, one per `.aar`:
  - `AlmediaSDK-android-sdk-mapping-<version>.txt`
  - `AlmediaSDK-android-bridge-mapping-<version>.txt`

  Download both and upload them to the crash reporter alongside the host app's own mapping file. They are not bundled in the package by design.

---

## License

See [LICENSE.md](./LICENSE.md).
