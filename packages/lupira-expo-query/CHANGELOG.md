# Changelog

## 0.1.0

- `queryClient`: `createAppQueryClient({ persistRoots, buster, maxAgeMs? })` → `{ queryClient, persistOptions }`; successful queries under the allowlisted roots persist to `expo-sqlite/kv-store`, `gcTime` follows `maxAge` (7 days by default).
- `online`: `connectOnlineManager()` (NetInfo internet reachability, then link state), `isOnline(state)`, `useOnline()`.
- `mirrorQuery(key, read)` and `onlineQuery(key, fetch)`: the two query option builders.
- `invalidateOnChange(queryClient, derivedRoots?)`: a sync engine `onChange` handler.
