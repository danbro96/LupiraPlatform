# Changelog

## 0.2.0

- `web()` and `mobile()` carry the `@danbro96/*` import policy (tokens, domain and sync-core everywhere; http, feedback, diagnostics, oidc and sqlite from data up; paper kit from ui; web session and web kits by layer) and allow npm and node-core imports, so an app config is the preset call alone.
- `mobile({ layers })` adds layers outside the standard stack (`{ name, pattern, imports, importedBy }`); `web({ domain: true })` adds the pure `domain` layer.
- `mobile()` forbids `@danbro96/lupira-expo-sqlite/node` outside tests; the `feedback`/`debug`/`polyfills` layers are gone.

## 0.1.0

- `pure()`: production code may import only its own package plus `allowModules`; tests exempt; optional React hook and Compiler rules.
- `web()`: downward-only data → state → ui with a `config` leaf.
- `mobile()`: downward-only domain → data → sync → state → ui with `feedback`/`debug`/`config` leaves and the generated client as its own element.
