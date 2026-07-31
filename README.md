# Almedia Link SDK for Unity

The Almedia Link SDK connects your Unity game to Freecash Link, letting players earn real cash rewards for playing, which lifts engagement, retention, and LTV. It's a drop-in integration with zero third-party dependencies on the Unity side.

Mechanically, the SDK lets players connect a Freecash account from inside a host app, checks whether the service is available in their region, and surfaces reward notifications as they arrive. The linking flow runs in a secure native browser; the rest (popups, buttons, notification cards) is Unity UI that blends with the host game.

---

## How it's built

The SDK has three layers:

- **C# facade** (`AlmediaLinkSDK` static API) - the only surface host code touches.
- **UI prefabs** - link button, popup, notification card, and activity overlay, all themable from **Almedia > Settings** or replaceable as Prefab Variants.
- **Native plugins** - one bridge per platform. iOS ships a single `.xcframework`; Android ships two AARs (`AlmediaLinkSDK.aar` for the SDK and `AlmediaLinkBridge.aar` for the Unity glue). Zero third-party runtime dependencies on the Unity side.

---

## Platform Support

| Platform | Minimum version | Tested on    |
|----------|------------------|--------------|
| Unity    | 2022.3 LTS       | 2022.3.62f2  |
| iOS      | 16.0             | 17.x, 18.x, 26.x |
| Android  | API 25           | API 27-36    |

The native plugins pull standard AndroidX and Kotlin libraries - see the [integration guide](./Documentation~/integration-guide.md#android--gradle-dependencies).

---

## Installation

In Unity: **Window > Package Manager > + > Add package from Git URL...** and paste:

```
https://github.com/almedia-tm/almedia-link-sdk.git
```

Append `#vX.Y.Z` to pin a specific release, e.g. `...almedia-link-sdk.git#v1.1.1`.

After install, open **Almedia > Settings** and fill in the iOS and Android integration keys.

---

## Quick Start

```csharp
using UnityEngine;
using AlmediaLink;
using AlmediaLink.Models;

public class AlmediaBootstrap : MonoBehaviour
{
    void Awake()
    {
        AlmediaLinkSDK.Initialize(new AlmediaLinkConfig
        {
            // Android needs at least one of: Gaid, Asid, Oaid, AdjustDeviceId, AppsFlyerId.
            // IDs that don't apply to the current platform are ignored.
            Idfa = "YOUR_IDFA",
            Gaid = "YOUR_GAID",
        });
    }
}
```

> The snippet above is the minimum bootstrap. For the full event surface see [Initialize the SDK](./Documentation~/integration-guide.md#initialize-the-sdk) in the integration guide.

All SDK events fire on the Unity main thread. See the [integration guide](./Documentation~/integration-guide.md#threading) for details.

On Android the SDK initializes only when the config carries at least one device/advertising ID - any of `Gaid`, `Asid`, `Oaid`, `AdjustDeviceId`, or `AppsFlyerId`. Use the same config to supply `AccountId` and the other identifiers when available.

Drop one of the `LinkButton` prefabs (`LinkButtonA`, `LinkButtonB`, `LinkButtonC`, or `LinkButtonD`) into a Canvas. It auto-shows when the player is eligible and not yet linked, and hides itself afterwards.

---

## Customization

Three levels of depth, from least to most invasive. Full detail in [Customize the UI](./Documentation~/integration-guide.md#customize-the-ui).

1. **Text and colors** - edit the strings and primary colors in **Almedia > Settings**. The built-in prefabs read the settings asset at runtime, so this works on a read-only UPM install.
2. **Prefab Variants** - create a Prefab Variant of any bundled UI prefab (`LinkPopup`, `NotificationCard`, `ActivityOverlay`) and assign it under **Almedia > Settings > Prefab Overrides** for full re-skin / layout control.
3. **Bring your own UI** - disable the default notification UI in settings and subscribe to `OnNotificationsReceived`, `OnStatusChanged`, and `OnLinkCompleted` to render your own visuals. The SDK still drives status, polling, and linking.

---

## Editor testing

In the Unity Editor and play-mode tests, drive the SDK into any state via `AlmediaLinkEditorMock`:

```csharp
#if UNITY_EDITOR
using AlmediaLink;
using AlmediaLink.Editor.Testing;
using AlmediaLink.Models;

AlmediaLinkSDK.Initialize(new AlmediaLinkConfig { /* test keys */ });
AlmediaLinkEditorMock.EmitStatus(AlmediaStatus.Blocked);
AlmediaLinkEditorMock.EmitError(AlmediaErrorCode.NetworkFailure, "Mock: backend unreachable");
AlmediaLinkEditorMock.EmitNotifications(); // empty fetch
#endif
```

Surface: `EmitStatus`, `EmitError`, `EmitLinkCompleted`, `EmitNotifications`, `EmitScreenPresented`, `EmitScreenDismissed`, `EmitNativeLog`, `CancelPending`.

- **Editor-only.** Lives in the `AlmediaLink.Editor` assembly (`includePlatforms:["Editor"]`); not present in iOS/Android player builds. Wrap host references in `#if UNITY_EDITOR` so your own code still compiles for device targets.
- **Manual mode.** The first call to any `AlmediaLinkEditorMock` method puts the bridge into manual mode for the rest of the play session — the auto-simulate coroutines that fire `Eligible`/`linked` for the happy path stop firing, so they can't race against your emits. Manual mode resets on domain reload.
- **Calling before `AlmediaLinkSDK.Initialize` throws** `InvalidOperationException`.

## Documentation

- **[Integration Guide](./Documentation~/integration-guide.md)** - install, configure, lifecycle, iOS ATT, Android dependencies, UI customization, troubleshooting.
- **[API Reference](./Documentation~/api-reference.md)** - every public type, method, event, and settings field.
- **[Changelog](./CHANGELOG.md)** - version history.

---

## Crash Symbols

Both native SDKs ship with symbol files so frames inside Almedia code deobfuscate in the host's crash reporter:

- **iOS dSYMs** - bundled inside `AlmediaLinkSDK.xcframework`. Xcode folds them into the app's `.xcarchive` automatically; an existing symbol-upload pipeline picks them up without any extra step.
- **Android mapping files** - two assets published on each GitHub Release, one per `.aar`:
  - `AlmediaLink-android-sdk-mapping-<version>.txt`
  - `AlmediaLink-android-bridge-mapping-<version>.txt`

  Download both and upload them to the crash reporter alongside the host app's own mapping file. They are not bundled in the package by design.

---

## License

See [LICENSE.md](./LICENSE.md).
