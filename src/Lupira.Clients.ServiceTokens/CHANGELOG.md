# Changelog

## 0.1.0
- `TokenEndpointClient` (named client `oauth-token`): `ClientCredentialsAsync`, RFC 8693 `ExchangeAsync`, `RefreshAsync` (rotated refresh token on `IssuedToken`). Error bodies map to `TokenErrorKind` via `TokenErrorKindWire`; failures throw `TokenEndpointException` (kind, status, description).
- `TokenCache`: process-wide, single-flight per key, 30 s skew, `notAfter` cap, `ExchangeKey` / `ClientCredentialsKey`, `Evict`.
- `IConfidentialClient`, `IOutboundHopOptions` (audience, client credentials, `DevUser` / `DevServiceId` / `DevScopes` fallback), bindable `OutboundHopOptions`, `TokenExchangeOptions` (`Auth:Exchange`).
- `ServiceTokenProvider.ApplyAsync`: client-credentials bearer, else `X-Dev-Service`, else `X-Dev-User` (+ `X-Dev-Scopes`); per-request `DevUserOverride`.
- `ServiceTokenHandler` + `AddLupiraServiceToken(hop)` / `AddLupiraServiceToken<TOptions>()` on an `IHttpClientBuilder`; `AddLupiraTokenEndpoint()`.
