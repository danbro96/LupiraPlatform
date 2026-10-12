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

/** The persister writes JSON: a Map, Set or Date would come back as a different shape that breaks its reader. */
export function isPlainJson(value: unknown): boolean {
  if (value === null || ['string', 'number', 'boolean', 'undefined'].includes(typeof value)) return true;
  if (Array.isArray(value)) return value.every(isPlainJson);
  if (typeof value !== 'object') return false;
  const proto = Object.getPrototypeOf(value);
  return (proto === Object.prototype || proto === null) && Object.values(value).every(isPlainJson);
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
        shouldDehydrateQuery: (query) =>
          defaultShouldDehydrateQuery(query) && roots.has(String(query.queryKey[0])) && isPlainJson(query.state.data),
      },
    },
  };
}
