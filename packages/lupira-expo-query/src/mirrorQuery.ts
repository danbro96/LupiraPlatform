import { queryOptions, type QueryKey } from '@tanstack/react-query';

/** A read over the local SQLite mirror: runs offline, never goes stale on its own, and refetches only when the sync
 *  engine invalidates its root. */
export function mirrorQuery<T, Key extends QueryKey>(queryKey: Key, read: () => Promise<T>) {
  return queryOptions({ queryKey, queryFn: read, staleTime: Infinity, networkMode: 'always', retry: false });
}
