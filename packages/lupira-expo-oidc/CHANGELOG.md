# Changelog

## 0.4.1

- `load()` waits at most 3 s for a due token refresh; past that the app opens with the stored token and the refresh lands in the background.

## 0.4.0

- `oidc`: `getDiscovery` fetches `<issuer>/.well-known/openid-configuration` itself, bounded by `timeoutMs`; a failing status, empty body or non-JSON body rejects with the new `DiscoveryError` (status in the message). Trailing slashes on `issuer` are ignored. `OidcClient.clientId`.
- `useOidcLogin({ oidc, scopes, scheme, path, onTokens, log? })` → `{ status, error, retry, signIn, redirectUri }`: discovery, PKCE prompt and code exchange for a login screen. Pure state and sign-in steps in `oidcLogin`.
- `authStore`: `load()` refreshes a due stored token before `loaded` flips; `onSignOut(cb)` fires after every `clearSession` and is on the AuthPort; `startAutoRefresh()` refreshes on foregrounding and just before expiry, returns the stop.
- `tokenSession`: `REFRESH_MARGIN_MS`.
- Peers: `@danbro96/lupira-http` `^0.3.0`; new `react`, `react-native`. Ships precompiled by React Compiler.

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
