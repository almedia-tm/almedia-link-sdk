# Changelog

## [1.3.0] - 2026-09-30

### Introducing: Almedia SDK
The SDK is now also published as `com.almedia.sdk`, shown as Almedia SDK in the Package Manager, from `https://github.com/almedia-tm/almedia-sdk.git`. This package, `com.almedia.link`, ships the same 1.3.0. Future releases are published as `com.almedia.sdk` only.

To move, replace the `com.almedia.link` line in `Packages/manifest.json` with `"com.almedia.sdk": "https://github.com/almedia-tm/almedia-sdk.git"`. Never install both packages. Every asset keeps its GUID, so scenes, Prefab Variants and settings references resolve as before.

On the first editor load after the upgrade, the settings asset moves from `Assets/AlmediaLink/Resources/AlmediaLinkSettings.asset` to `Assets/Almedia/Resources/AlmediaSettings.asset`, and the empty `Assets/AlmediaLink` folder is removed. The SDK finds the moved asset by itself. If your own code loads the asset with `Resources.Load("AlmediaLinkSettings")`, change that call to `AlmediaLinkSettings.Load()`. An asset that cannot be moved stays where it is and keeps working.

The SDK's API is now `Almedia`, in the `AlmediaSDK` namespace and assembly, with `AlmediaConfig` for configuration. Every public type starts with `Almedia`, so none collides with a type in your game or in another SDK.
- `Almedia.Status` holds the player's status as one object, and `Almedia.OnStatusChanged` fires on every change. `Linked` carries `CanShowRewardHub` and `CanShowOffer`. `NotAvailable` carries the reason. See [Status and lifecycle](./Documentation~/integration-guide.md#status-and-lifecycle).
- Error codes, screens, log levels and the other value sets are classes, not enums. Compare them with `==`.
- `Almedia` has no `Engage()`. Use `ShowLink()` and `ShowRewardHub()`.
- `AlmediaEditorMock` drives `Almedia` in the editor.

Existing integrations need no code changes. Code that uses `AlmediaLinkSDK`, `AlmediaLinkConfig` and `AlmediaLink.Models` compiles and behaves as before, and assembly definitions that reference `AlmediaLink` still resolve. The [migration guide](./Documentation~/migration-guide.md) shows how to move code to `Almedia`, and the [AlmediaLink API reference](./Documentation~/almedialink-api-reference.md) documents the existing API. Everything under Added works with both APIs.

### Added
- Player progress. `Almedia.Progress` holds a linked player's progress in your game: balance, lifetime earnings, pending, completed and expired tasks, and username. It is `null` when there is no progress to show. `OnProgressUpdated` fires on every change. `OnTaskCompleted` and `OnBalanceChanged` report completed tasks and balance changes. Render your UI from `Progress` and use the events for celebrations. The events can repeat, so deduplicate on their `Id`. The editor mocks can emit all three. See [Progress and rewards](./Documentation~/integration-guide.md#progress-and-rewards).
  - Every amount in `Progress`, `OnTaskCompleted` and `OnBalanceChanged` is an `AlmediaRewardPoints`: coins, plus `AlmediaMoney` in the player's currency and in USD. `AlmediaMoney.Amount` is an exact `decimal`, and the SDK does not format it. The supported player currencies are USD, EUR, GBP, CAD, AUD, PLN, CHF, KRW, JPY and SEK. Any other currency shows as its USD value.
- Tasks whose reward decreases over time. `AlmediaTask.RewardDropsAt` is the time of the next decrease, for a countdown. `AlmediaCompletedTask.ActualReward` is the reward paid at completion. `AlmediaTaskKind` lists the known task kinds. Show an unknown kind as `Main`.
- `ShowLink()` shows the link popup to a player who can link, and the popup's button starts linking. It does nothing for other players. The popup comes from the new **Link Popup** slot in **Almedia → Settings → Default UI Prefabs**. New projects get the bundled popup there. An existing settings asset keeps the slot empty, so the build does not grow. Until you assign a popup, `ShowLink()` uses the deprecated `LinkPopupOverride` or starts linking directly. A popup in the slot is included in every build.
- `DisabledFeatures` in the config, and **Disabled Features** in **Almedia → Settings**. Declare the parts of the experience your game hides: linking, the reward hub, the offer screen or notifications. The backend then stops offering them, and our reporting can tell a deliberate rollout from a broken integration. The settings apply to every player, and code can add to them for the current player. A player whose linking you hide reads `NotAvailable` with the reason `Disabled`.
- `TrafficSource` and `Meta1` to `Meta4` in the config. They add your acquisition source, for example `applovin_int`, and up to four values of your own to the linking URL, for your S2S reporting. Each value is limited to 250 UTF-8 bytes.
- Players who have the Freecash app can link in the app instead of a web screen, so they do not log in again (iOS and Android). The backend decides where this applies. Control returns to the game at once, and the status updates when the game comes back to the foreground. As with linking in the browser, no screen events fire.
- Where Almedia enables it for your integration, linking, the reward hub and offers work on devices without an advertising ID. The SDK then creates an install ID and stores it on the device.
- When the Adjust or AppsFlyer SDK is in your app and Almedia enables it for your integration, the SDK reads their device ID itself. You no longer need to pass `AdjustDeviceId` or `AppsFlyerId`. A value you pass still wins.
- A reward earned in the reward hub or offer screen can now reach the game right away, without waiting for the next poll.
- The bundled UI prefabs log a warning through `OnLog` when they are shown in a scene without an active `EventSystem`. They need one to receive taps.

### Changed
- The backend can now set how often the SDK polls for messages, and hold a request for up to a minute until a message arrives. Your polling interval applies when the backend sets none. A `FetchNotifications()` result can therefore take up to about a minute.
- `OnErrorOccurred` no longer fires when the connection drops during the SDK's own background work: notification polling, event delivery and status refreshes. The SDK retries that work and logs the failure at `Warning`. Failures of calls your game makes, such as `Initialize` or opening a screen, still fire it. `Error` logs and `OnErrorOccurred` are now safe to send to a crash reporter.
- An event handler that throws no longer stops the other handlers of that event, or later SDK callbacks. The exception is logged through `OnLog` at `Error`.
- A link popup that fails to open, for example a Prefab Variant with a missing reference, is removed and logged at `Error`. It no longer throws out of the `LinkButton` tap or the `ShowLink()` call.

### Fixed
- Fixed the link popup staying open after the player could no longer link, for example after linking on another device.
- Fixed lost callbacks when a GameObject in your game had the same name as the SDK's callback receiver. The SDK then never left `NotInitialized`. The receiver is now named `[Almedia SDK] Bridge`, which is reserved, and `Initialize` logs an error naming any other object with that name.
- Fixed an SDK screen on iOS breaking when your game showed its own full-screen view over it, such as a login or paywall. The SDK no longer closes your view, and its screen stays open underneath with a working close button.
- Fixed crashes on Android when a link in an SDK web screen could not be opened, and when the game moved on while the SDK still had a request in flight.
- Fixed frame hitches on Android when the player opens the SDK's UI, and the SDK staying unavailable for a session after a failed file write.
- Fixed delays on Android with a weak connection, when returning to the game after linking in the browser and when opening an SDK screen.
- Fixed a rare case on Android where the game missed a status change, including `OnLinkCompleted` after linking in the browser.
- Fixed Android collecting the advertising ID after the player opted out of ad tracking, when saving the opt-out failed.
- Fixed buttons in the SDK's web screens doing nothing on Android in apps minified without Android's default ProGuard file.

## [1.2.1] - 2026-09-17

### Fixed
- Fixed the Android guard that stops native callbacks reaching Unity once the player begins quitting. The call that switches it on asked for an instance method where the compiled native one is static, so it failed - logging `NoSuchMethodError: no non-static method with name='notifyPlayerQuitting'` during shutdown - and the guard stayed off, leaving callbacks able to reach a player that was already tearing down. The call is corrected, and a new test pins every Unity-to-native call to the shape of the method it targets, so a mismatch fails the build instead of shipping.
- Fixed an Android build failure - `Duplicate class a.a found in modules AlmediaLinkSDK.aar and ...` - when the app also ships another obfuscated library. R8 used to minify the SDK's internal class names into top-level packages `a`, `b`, `c` and so on, which any other SDK obfuscated the same way also claims. Internal classes now live under `co.almedia.internal` (and `co.almedia.bridge.internal`), so the names cannot collide. Nothing about the public API changes.

## [1.2.0] - 2026-08-31

### Added
- In-game reward grants. `AlmediaLinkSDK.OnInGameRewardGrantRequested` fires when the backend instructs the game to grant in-game rewards, carrying an `AlmediaInGameRewardGrant`: one event per grant, bundling one or more `AlmediaInGameReward` line items (`Amount` is a `double`, `Code` is the reward code agreed with Almedia). Delivery is at-least-once, so deduplicate on `Id` when a repeat credit matters. The integration guide covers the contract and the dedup pattern.
- `AlmediaNotification.Display` - the presentation hint, `"popup"` (banner card) or `"tray"` (quiet list entry). The set is open; treat unknown values as `"popup"`. Never null or empty.
- `AlmediaNotification.IconUrl` - optional absolute URL of the notification's icon, `null` when the backend sent none. Exposed for host-rendered notification UI; the bundled UI does not render it in this release.
- `AlmediaLinkEditorMock.EmitInGameRewardGrant(...)` delivers a mock grant through the real bridge path; the overload with an explicit id, called twice, reproduces an at-least-once redelivery for testing host-side dedup. `MockNotification` gained `Display` and `IconUrl`.
- Zero-setup initialization. A `LinkButton` prefab initializes the SDK by itself when no host code has called `Initialize` by the end of the button's first frame, using the values from **Almedia → Settings**. Install, set keys, drag a prefab is now a complete integration. A host `Initialize` in that first frame runs first and the prefab stands down. A later host call with a different configuration re-initializes and takes over. The new **Auto-Initialize From Prefabs** setting gates the prefab path. Fresh installs start with it on.
- **Note for SDK upgrades**: auto-initialization is off by default and you need to enable it explicitly if you want it (**Almedia → Settings → Auto-Initialize From Prefabs**). We recommend it (and deleting your custom initialization) unless you use `AccountId`.
- `AlmediaLinkSDK.NotAvailableReason` reports why the service is unavailable, non-null exactly while `CurrentStatus` is `NotAvailable`.
- `AlmediaLinkSDK.ScreenAvailability` (`CanShowRewardHub`, `CanShowOffer`) and `OnScreenAvailabilityChanged` report which screens can be presented right now. A linked player can lose the reward hub or gain an offer between syncs with no status transition; gate your own entry points on this instead of caching a flag that goes stale.

### Changed
- `AlmediaNotification.Type` is now `[Obsolete]`; use `Display`. Both carry the same value, the presentation hint `"popup"` or `"tray"`. Code comparing it against category strings such as `"reward"` or `"promo"` keeps compiling with a deprecation warning and never matches.
- **BREAKING.** `NotificationIconMap` is removed, along with `Assets/AlmediaLink/Resources/NotificationIconMap.asset` and the icon lookup in the bundled UI. The bundled card and activity overlay are visually unchanged: they show the icon authored on the `NotificationRow` prefab. Delete the leftover asset from your project; to change the icon, make a Prefab Variant of `NotificationRow`.
- `NotificationRowView.Populate(AlmediaNotification, Sprite)` is now `Populate(AlmediaNotification)`. The row no longer takes an icon from the caller.
- `MockNotification`'s fourth constructor parameter is now named `display` (was `type`). Positional calls are unaffected; a named `type:` argument must be renamed.
- The bundled `LinkButton` now hides for a linked player whose reward hub is unavailable, rather than showing a rewards button that opens nothing, and reappears if the hub returns. That case reports `promo_load` with `Hidden`. Eligible and fully-linked players are unaffected.
- `AlmediaLinkEditorMock.EmitStatus` takes optional `reason`, `canShowRewardHub`, and `canShowOffer`. Existing one-argument calls are unchanged. Omitted availability defaults to `true` for `Linked` and `false` for every other status.

## [1.1.4] - 2026-08-18

### Fixed
- Fixed a crash on Android when the player quits. Callbacks that arrived from native while Unity was tearing the player down called `UnitySendMessage` on an engine whose message queue was already gone, killing the process. The SDK now stops delivering callbacks to Unity the moment player teardown starts, and resumes on the next `Initialize()`.

## [1.1.3] - 2026-08-05

### Fixed
- Fixed an issue on Android where host apps compiled with `minSdk` 23 would not receive `OnLinkCompleted` after linking - and therefore dispensed no in-game rewards - leaving the SDK stuck in the `Eligible` state. The SDK's internal lifecycle observer never attached in such builds, so the post-linking sync that promotes the player to `Linked` never ran.

## [1.1.2] - 2026-08-04

### Added
- OS support floor. On devices below iOS 16 / Android API 25 the SDK disables itself cleanly: `Initialize()` completes with status `NotAvailable`, a single warning reported through `OnLog` states the device OS version and the required one, and every further SDK call is inert - no C# code path touches the native libraries. Devices at or above the floor are unchanged, as is Editor play mode.

### Changed
- The ATT consent machinery buried in 1.1.0 is now removed: the pre-prompt screen and its prefab, the settings fields behind it, and the ATT preliminary tracking event. The public surface survives as `[Obsolete]` compatibility stubs so 1.x host code keeps compiling: the `Att*` settings getters return the former default values, `ATTPrePromptController` remains as an inert stub with its original script identity (host Prefab Variants still load), and `AlmediaLinkEditorMock.EmitShowATTPrePrompt()` is a warning no-op. Unchanged, as before: ATT-gated IDFA reading, ATT-based domain switching, and the iOS post-build hook that adds a default `NSUserTrackingUsageDescription` to `Info.plist` (host-supplied values still win).
- Bundled fonts reduced from four Poppins weights to two: **Bold** and **Regular** (SemiBold and Medium removed; the package shrinks by ~1.1 MB). Labels that used SemiBold now render in Bold, and Medium in Regular. The link popup's body text now renders in Poppins-Regular instead of the host project's LiberationSans, removing the SDK's dependency on a font asset it does not ship.
- The link popup's decorative background textures (`Cards`, `CardsLightBeams`) are capped at 1024px import size, trimming built-app size with no visible change.
- The bundled UI is now pay-per-use. The prefabs moved out of `Resources/` (to `Packages/com.almedia.link/Runtime/Prefabs/`), so an integration that uses none of them ships none of the SDK's UI, art, or fonts - only the bridge glue. Compatibility shims keep 1.x integrations working:
  - **Link popup:** each `LinkButton` prefab now carries its own reference to the `LinkPopup` prefab (visible in the button's inspector); a button with the field emptied starts linking directly on tap, with a one-time warning. `AlmediaLinkSettings.LinkPopupOverride` is deprecated but **still honored** - a variant assigned there wins over the button's reference - so upgrading hosts keep their popup. New integrations assign the button's *Link Popup* field instead.
  - **Notification UI:** `NotificationCardOverride` / `ActivityOverlayOverride` are renamed `NotificationCardPrefab` / `ActivityOverlayPrefab`; deprecated read-only aliases remain, and serialized values migrate automatically. The slots are now *the* references the SDK instantiates, pre-filled with the bundled prefabs. Turning **Enable Default Notification UI** off clears the slots that point at the bundled prefabs so they stay out of the build - a prefab you assigned yourself survives the toggle; turning it back on restores the bundled defaults into empty slots.
  - **Settings theming:** `ApplyHostSettings` is now a checkbox on the popup/card/overlay prefabs themselves (default on). A one-shot editor migration unticks it on host variants that were assigned in the old override slots, preserving their 1.x appearance; newly created variants receive settings theming unless the box is unticked.
  - **BREAKING (undocumented pattern):** code that loaded the bundled prefabs itself via `Resources.Load("Prefabs/...")` now gets `null` - reference them directly from `Packages/com.almedia.link/Runtime/Prefabs/` instead.

## [1.1.1] - 2026-07-29

### Changed
- In-app screens (linking, reward hub, offer) now present transparently over the running game: it stays visible behind a screen while that screen's content loads, instead of being hidden by an opaque background.
- In-app screen stability fixes.

## [1.1.0] - 2026-07-27

### Added
- Reward progression screen. `AlmediaLinkSDK.ShowRewardHub()` opens the reward hub in a webview for a linked user. `AlmediaLinkSDK.Engage()` is a context-aware entry point that forwards to native, which routes on the player's state - starting linking when eligible, opening the reward hub when linked, and no-op-with-a-log otherwise; all routing lives in native, so Unity forwards the call regardless of the current status once ready. Both methods no-op with a warning until the SDK is ready; native additionally requires a live progression URL for the reward hub and reports its own no-op through `OnLog`.

- Offer screen. `AlmediaLinkSDK.ShowOffer()` opens an offer in a webview. No-op with a warning until the SDK is ready; native additionally requires a live offer URL, and that URL can appear and disappear between syncs - a call that worked earlier may later be a no-op there, reported through `OnLog`. Only one in-app screen can be open at a time: a call made while another screen is showing is ignored with a log rather than raising `OnErrorOccurred`.

- Unified screen lifecycle callbacks. `AlmediaLinkSDK.OnScreenPresented` fires when any SDK screen (`AlmediaScreen.Linking`, `.RewardHub`, `.Offer`) lands on top of the game - pause there - and `AlmediaLinkSDK.OnScreenDismissed` fires when it is gone - resume there - carrying an `InAppScreenResult` (`Completed`, `Cancelled`, or `Failed` with an `AlmediaError`). A screen that appears produces exactly one matched pair regardless of trigger (API method, `LinkButton` prefab, `Engage()` routing); a call that opens nothing fires neither. Presented fires when the native container commits to presenting (before page load); dismissed fires after native's sync-on-close and, for webview linking, before the outcome link callbacks. System-browser linking fires neither - only the link callbacks. The editor mock simulates the pair and `AlmediaLinkEditorMock` gained `EmitScreenPresented` / `EmitScreenDismissed`.

### Changed
- `LICENSE.md` now contains the final Almedia Link SDK License approved by Almedia's general counsel, replacing the interim placeholder. The license is an Exhibit to the Master Agreement between Almedia and the integrator; third-party notices are unchanged.
- The Link button is now two-state. It stays visible once the player links, becoming a rewards entry that opens the reward hub, instead of hiding as before; it hides only when the player can neither link nor view rewards. Each `LinkButton` prefab now holds a self-contained subtree per state (eligible / linked), each with its own background, button and labels.
- **BREAKING.** ATT consent flow buried. `AlmediaLinkConfig.CanRunConsentFlow` and the **Enable Consent Flow (iOS ATT)** setting are removed - delete any usage (it becomes a compile error). The SDK no longer shows the ATT pre-prompt screen. ATT-gated IDFA reading is unchanged: the iOS post-build hook now always adds a default `NSUserTrackingUsageDescription` to `Info.plist` on every iOS build (any host-supplied value is preserved) and runs after other SDKs' post-processors. Native ATT layers stay dormant and are removed fully in a later release. The editor mock's `AlmediaLinkEditorMock.EmitShowATTPrePrompt()` is now marked obsolete and shows no UI; it goes away with them.

## [1.0.1] - 2026-07-06

### Added
- `AlmediaLinkConfig.Idfv` - iOS Identifier for Vendor, forwarded to native as `idfv`. Runtime-only with no settings fallback, like the other advertising identifiers. A supplied value overrides the device-issued one; when omitted, the iOS SDK collects it automatically via `UIDevice.current.identifierForVendor`. Unlike `Idfa`, it is not gated behind ATT. iOS-only - ignored on Android. The QA test panel gained a matching IDFV input. **Requires the matching `fc-link-sdk-ios` native binary - the field is silently ignored by older bundled plugins.**

### Changed
- `AlmediaLinkSDK.Initialize` is now idempotent. A repeat call with the same effective configuration is a no-op that preserves `CurrentStatus` instead of resetting it to `NotInitialized` and re-dispatching. Previously, because the native layer dedupes a same-config init without re-emitting a status callback, a redundant `Initialize(sameConfig)` left the Unity side permanently reporting `NotInitialized`. A call with a different configuration still tears down the session and re-initializes.
- `StartLinking`, `FetchNotifications`, and `StartNotificationPolling` now no-op with a warning when called before the SDK is ready (before the first `OnStatusChanged` fires), rather than only checking that the native bridge object exists. `StopNotificationPolling` and the ATT/tracking callbacks are unchanged - the latter legitimately fire during initialization.

## [1.0.0] - 2026-06-23

Initial release pending public availability.
