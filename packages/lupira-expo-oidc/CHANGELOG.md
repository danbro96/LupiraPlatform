# Changelog

## 0.3.0

- `authStore`: `createAuthStore({ keyPrefix, defaultApiUrl, defaultAuthMode, oidc, ... })` → a zustand store with `load`, `setBackend`, `setSession`, `clearSession`, `refreshIfNeeded`, `isAuthenticated`, `onSignIn`; registers itself as the lupira-http AuthPort.
- `authStore`: `onAccountChange(prevSub, nextSub)` is awaited before a sign-in as a different account than the last one (persisted as `<prefix>.lastSub`, kept across sign-out); `beforeSignOut(reason)`, `onDefinitiveFailure`, `onLoad`, typed `extend(set, get)` for app state and actions; `toAuthPort(getState)` for apps with a wider port.
- `oidc`: `hasAudience(token, audience)`.
- New peers: `@danbro96/lupira-http`, `zustand`.

## 0.2.0

- `createTokenRefresher`: optional `onDefinitiveFailure(e)`, called before `signOut()` when the token endpoint rejects the refresh token for good.

## 0.1.0

- `oidc`: `createOidcClient({ issuer, clientId, timeoutMs, log? })` → `getDiscovery`, `exchangeAuthCode`, `refreshTokens`; `RefreshError`, `TokenSet`, `decodeJwt`.
- `tokenSession`: `secureSessionStore(prefix)`, `createTokenRefresher(...)` (coalesced, rotation-safe refresh).
- `crypto`: side-effect polyfill for `crypto.getRandomValues` / `randomUUID` on Hermes.
