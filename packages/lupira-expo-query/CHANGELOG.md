# Changelog

## 0.1.1

- `queryClient`: queries whose data is not plain JSON (a `Map`, `Set`, `Date` or class instance anywhere in it) are no longer persisted; JSON would restore them as a different shape. `isPlainJson(value)` exported.

## 0.1.0

- `queryClient`: `createAppQueryClient({ persistRoots, buster, maxAgeMs? })` → `{ queryClient, persistOptions }`; successful queries under the allowlisted roots persist to `expo-sqlite/kv-store`, `gcTime` follows `maxAge` (7 days by default).
- `focus`: `connectFocusManager()` refetches stale queries when the app returns to the foreground.
- `online`: `connectOnlineManager()` (NetInfo internet reachability, then link state), `isOnline(state)`, `useOnline()`.
- `mirrorQuery(key, read)` and `onlineQuery(key, fetch)`: the two query option builders; `onlineQuery` retries once, never on a 4xx.
- `invalidateOnChange(queryClient, derivedRoots?)`: a sync engine `onChange` handler.
