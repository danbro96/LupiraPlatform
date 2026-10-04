# Changelog

## 0.1.0
- `ExposedSurface`: loads the application assembly's embedded `exposed.json` — per-cluster `prefix` / `announcePrefix`, named groups with `policy`, `prefixed`, `catchAll` (GET-only), `documented`, `credential` and `pathMap`; exposes `ApiPrefixes` and `DeviceKeyPrefixes`.
- `ProxyRoutes`: one exact template per path with verbs pinned, as `ReverseProxy:Routes:*` configuration; collision guard on every key, verb in unprefixed keys, optional `X-Forwarded-Prefix`.
- `AddLupiraBffProxy()`, `MapLupiraBffProxy()` / `MapApiPrefixFence()`, `UseLupiraBffDeviceKeyGate()`, `TryPrintLupiraBffRoutes(args)`.
- Upstream credential transform and `SessionTokenHandler` (bearer passthrough, Development `X-Dev-User` replace, Duende session token); guest path refill from claims.
