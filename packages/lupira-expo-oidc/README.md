# @danbro96/lupira-expo-oidc

OIDC plumbing for the Lupira Expo apps; the zustand store, login screen and config stay in the app.
Import `@danbro96/lupira-expo-oidc/crypto` first in `index.ts`.
The stores use `tokenSession`: `secureSessionStore(prefix)` persists `<prefix>.token|refreshToken|expiresAt`, and `createTokenRefresher` owns single-flight, rotation-safe refresh.
