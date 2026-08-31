# Changelog

## [Unreleased]

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
