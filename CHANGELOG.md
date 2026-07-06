# Changelog

## [Unreleased]

## [1.0.1] - 2026-07-06

### Added
- `AlmediaLinkConfig.Idfv` - iOS Identifier for Vendor, forwarded to native as `idfv`. Runtime-only with no settings fallback, like the other advertising identifiers. A supplied value overrides the device-issued one; when omitted, the iOS SDK collects it automatically via `UIDevice.current.identifierForVendor`. Unlike `Idfa`, it is not gated behind ATT. iOS-only - ignored on Android. The QA test panel gained a matching IDFV input. **Requires the matching `fc-link-sdk-ios` native binary - the field is silently ignored by older bundled plugins.**

### Changed
- `AlmediaLinkSDK.Initialize` is now idempotent. A repeat call with the same effective configuration is a no-op that preserves `CurrentStatus` instead of resetting it to `NotInitialized` and re-dispatching. Previously, because the native layer dedupes a same-config init without re-emitting a status callback, a redundant `Initialize(sameConfig)` left the Unity side permanently reporting `NotInitialized`. A call with a different configuration still tears down the session and re-initializes.
- `StartLinking`, `FetchNotifications`, and `StartNotificationPolling` now no-op with a warning when called before the SDK is ready (before the first `OnStatusChanged` fires), rather than only checking that the native bridge object exists. `StopNotificationPolling` and the ATT/tracking callbacks are unchanged - the latter legitimately fire during initialization.

## [1.0.0] - 2026-06-23

Initial release pending public availability.
