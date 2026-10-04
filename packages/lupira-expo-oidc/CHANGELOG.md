# Changelog

## 0.2.0

- `createTokenRefresher`: optional `onDefinitiveFailure(e)`, called before `signOut()` when the token endpoint rejects the refresh token for good.

## 0.1.0

- `oidc`: `createOidcClient({ issuer, clientId, timeoutMs, log? })` → `getDiscovery`, `exchangeAuthCode`, `refreshTokens`; `RefreshError`, `TokenSet`, `decodeJwt`.
- `tokenSession`: `secureSessionStore(prefix)`, `createTokenRefresher(...)` (coalesced, rotation-safe refresh).
- `crypto`: side-effect polyfill for `crypto.getRandomValues` / `randomUUID` on Hermes.
