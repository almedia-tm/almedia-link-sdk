# Changelog

## [Unreleased]

## [1.1.2-preview.1] - 2026-07-31

### Added
- OS support floor. On devices below iOS 16 / Android API 25 the SDK disables itself cleanly: `Initialize()` completes with status `NotAvailable`, a single warning reported through `OnLog` states the device OS version and the required one, and every further SDK call is inert - no C# code path touches the native libraries. Devices at or above the floor are unchanged, as is Editor play mode.

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
