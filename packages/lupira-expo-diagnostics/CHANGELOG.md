# Changelog

## 0.2.2
- Accepts `@danbro96/lupira-expo-paper` ^0.6.0.
- Peer `expo-constants` relaxed to >=57.0.19, so apps on that patch keep their native fingerprint.

## 0.2.1
- Accepts `@danbro96/lupira-expo-paper` ^0.5.0.

## 0.2.0
- `buildInfo`: `APP_NAME` and `APP_VERSION` from the Expo config, beside `UPDATE_LABEL`.
- `VersionLine`: the About line (name, marketing version, OTA label) every Settings screen ends with; moved here from `lupira-expo-paper`.
- `initSentry(dsn, options?)`: the shared Sentry setup (off without a DSN, no default PII, environment, OTA tags).
- Peer `lupira-expo-paper` ^0.3.0 || ^0.4.0; new peer `expo-constants`.

## 0.1.3
- Accepts `@danbro96/lupira-expo-paper` ^0.3.0.

## 0.1.2
- Accepts `@danbro96/lupira-expo-paper` ^0.2.0.

## 0.1.1
- Depends on `@danbro96/lupira-tokens-core` ^0.2.0.

## 0.1.0

- `log`: redacting `logDebug` buffer with Sentry breadcrumbs, `useDebugLog`, `clearDebugLog`, `redact`.
- `DebugLogScreen`: on-device view and share of the buffer.
- `DeveloperScreen`: backend presets, custom backend, diagnostics links and sync state, all passed as props.
- `buildInfo`: `UPDATE_ID`, `UPDATE_CHANNEL`, `UPDATE_LABEL`; `useAutoUpdate`: OTA check on foreground, reload when pending.
