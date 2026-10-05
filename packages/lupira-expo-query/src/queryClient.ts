import { createAsyncStoragePersister } from '@tanstack/query-async-storage-persister';
import { defaultShouldDehydrateQuery, QueryClient } from '@tanstack/react-query';
import type { PersistQueryClientProviderProps } from '@tanstack/react-query-persist-client';
import Storage from 'expo-sqlite/kv-store';

const WEEK_MS = 7 * 24 * 60 * 60_000;

export interface AppQueryClientOptions {
  /** Query-key roots kept across restarts; every other query lives in memory only. */
  persistRoots: string[];
  /** The app version: a new build starts from an empty persisted cache. */
  buster: string;
  maxAgeMs?: number;
}

export interface AppQueryClient {
  queryClient: QueryClient;
  /** For `<PersistQueryClientProvider client={queryClient} persistOptions={persistOptions}>`. */
  persistOptions: PersistQueryClientProviderProps['persistOptions'];
}

export function createAppQueryClient({ persistRoots, buster, maxAgeMs = WEEK_MS }: AppQueryClientOptions): AppQueryClient {
  const roots = new Set(persistRoots);
  return {
    // gcTime must outlive maxAge, or the persister writes back a cache that has already been collected.
    queryClient: new QueryClient({ defaultOptions: { queries: { gcTime: maxAgeMs } } }),
    persistOptions: {
      persister: createAsyncStoragePersister({ storage: Storage, key: 'lupira.query-cache' }),
      maxAge: maxAgeMs,
      buster,
      dehydrateOptions: {
        shouldDehydrateQuery: (query) => defaultShouldDehydrateQuery(query) && roots.has(String(query.queryKey[0])),
      },
    },
  };
}
