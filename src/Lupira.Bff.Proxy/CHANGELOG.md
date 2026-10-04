# Changelog

## 0.2.0
- Route guards from the upstream OpenAPI specs (`RouteGuardOptions.Specs`, cluster → document): templated parameters gain a type constraint from their schema (`uuid` → `guid`, `int32` → `int`, other `integer` → `long`, `boolean` → `bool`), and every unlisted upstream operation that outranks a listed template on the same verb answers 404 (`MapRouteFences()`, part of `MapLupiraBffProxy()`). `/items/{id}` no longer forwards `/items/thin`.
- Route keys, methods, clusters and policies are unchanged; constraints are applied by a YARP config filter at load.
- A cluster without a spec keeps unconstrained templates and logs a startup warning.

## 0.1.0
- `ExposedSurface`: loads the application assembly's embedded `exposed.json` — per-cluster `prefix` / `announcePrefix`, named groups with `policy`, `prefixed`, `catchAll` (GET-only), `documented`, `credential` and `pathMap`; exposes `ApiPrefixes` and `DeviceKeyPrefixes`.
- `ProxyRoutes`: one exact template per path with verbs pinned, as `ReverseProxy:Routes:*` configuration; collision guard on every key, verb in unprefixed keys, optional `X-Forwarded-Prefix`.
- `AddLupiraBffProxy()`, `MapLupiraBffProxy()` / `MapApiPrefixFence()`, `UseLupiraBffDeviceKeyGate()`, `TryPrintLupiraBffRoutes(args)`.
- Upstream credential transform and `SessionTokenHandler` (bearer passthrough, Development `X-Dev-User` replace, Duende session token); guest path refill from claims.
