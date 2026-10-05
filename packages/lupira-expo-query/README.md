# @danbro96/lupira-expo-query

React Query is the only read path in the Lupira Expo apps. Offline aggregates read the SQLite mirror through `mirrorQuery` (never stale, runs offline, refetched when the sync engine reports a change); online-only reads use `onlineQuery` (paused offline, fresh for 5 minutes, one retry) and persist across restarts.

- `queryClient`: `createAppQueryClient({ persistRoots, buster, maxAgeMs? })` → `{ queryClient, persistOptions }`.
- `online`: `connectOnlineManager()` once at startup; `useOnline()` for screens.
- `mirrorQuery(key, read)`, `onlineQuery(key, fetch)`: `useQuery` options.
- `invalidateOnChange(queryClient, derivedRoots?)`: pass as the sync engine's `onChange`.

```tsx
export const { queryClient, persistOptions } = createAppQueryClient({ persistRoots: ['photos'], buster: APP_VERSION });
connectOnlineManager();

<PersistQueryClientProvider client={queryClient} persistOptions={persistOptions}>…</PersistQueryClientProvider>

const items = useQuery(mirrorQuery(['cal.item', month], () => itemsInMonth(db, month)));
const photos = useQuery(onlineQuery(['photos', 'list'], () => listPhotos()));
```
