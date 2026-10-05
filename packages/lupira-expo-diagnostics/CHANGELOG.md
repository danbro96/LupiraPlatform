# Changelog

## 0.1.2
- Accepts `@danbro96/lupira-expo-paper` ^0.2.0.

## 0.1.1
- Depends on `@danbro96/lupira-tokens-core` ^0.2.0.

## 0.1.0

- `log`: redacting `logDebug` buffer with Sentry breadcrumbs, `useDebugLog`, `clearDebugLog`, `redact`.
- `DebugLogScreen`: on-device view and share of the buffer.
- `DeveloperScreen`: backend presets, custom backend, diagnostics links and sync state, all passed as props.
- `buildInfo`: `UPDATE_ID`, `UPDATE_CHANNEL`, `UPDATE_LABEL`; `useAutoUpdate`: OTA check on foreground, reload when pending.
