# @danbro96/lupira-expo-oidc

OIDC plumbing and the auth store for the Lupira Expo apps; the login screen and config stay in the app.
Import `@danbro96/lupira-expo-oidc/crypto` first in `index.ts`.

## Auth store

`createAuthStore` owns the session: SecureStore persistence under `keyPrefix`, rotation-safe refresh, backend switching and the lupira-http AuthPort registration.

```ts
import { createAuthStore } from '@danbro96/lupira-expo-oidc/authStore';
import { hasAudience } from '@danbro96/lupira-expo-oidc/oidc';

export const useAuth = createAuthStore({
  keyPrefix: 'lupira.photos',
  defaultApiUrl: DEFAULT_API_URL,
  defaultAuthMode: DEFAULT_AUTH_MODE,
  oidc,
  log: logDebug,
  onAccountChange: async () => {
    queryClient.clear();
    await uploadQueue.wipe();
  },
  beforeSignOut: (reason) => {
    if (reason === 'expired') toast('Session expired — please sign in again.');
  },
});

export const useGeoReady = () => useAuth((s) => s.authMode === 'dev' || hasAudience(s.token, 'lupira-geo'));
```

- `setSession(tokens)` derives `user` from the access token (`email` → `preferred_username` → `sub`).
- `onAccountChange(prevSub, nextSub)` runs before the state flips, so `onSignIn` subscribers never see the previous account's data; `prevSub` is `null` on the first sign-in of an install.
- `extend(set, get)` adds typed app state and actions; `onLoad` hydrates them before `loaded` flips. Apps with a wider port register `{ ...toAuthPort(useAuth.getState), ...extras }`.
- `load()` refreshes a stored token that is due before `loaded` flips, waiting at most 3 s (then it opens with the stored token and the refresh lands in the background); a transient failure keeps it, a rejected refresh token signs out.
- `onSignOut(cb)` fires after every `clearSession` (sign-out, expiry, backend switch).
- `startAutoRefresh()` (call once at startup) refreshes on foregrounding and `REFRESH_MARGIN_MS` before expiry; returns the stop.

## Login screen

```tsx
WebBrowser.maybeCompleteAuthSession();

const { status, error, retry, signIn } = useOidcLogin({
  oidc,
  scopes: OIDC_SCOPES,
  scheme: OIDC_SCHEME,
  path: OIDC_REDIRECT_PATH,
  onTokens: (tokens) => useAuth.getState().setSession(tokens),
  log: logDebug,
});
```

- `status`: `loading` (discovery or the auth request pending) → `ready`; `unavailable` when discovery failed (`error` holds why, `retry()` refetches); `busy` during `signIn()`.
- A sign-in the user backs out of leaves `error` null.

## Lower level

`secureSessionStore(prefix)` persists `<prefix>.token|refreshToken|expiresAt`; `createTokenRefresher` owns single-flight, rotation-safe refresh; `createOidcClient` does discovery, code exchange and refresh.
