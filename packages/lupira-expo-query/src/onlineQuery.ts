import { queryOptions, type QueryKey } from '@tanstack/react-query';

const FIVE_MINUTES_MS = 5 * 60_000;

/** A read straight from the server: paused while offline, served from cache for five minutes. */
export function onlineQuery<T, Key extends QueryKey>(queryKey: Key, fetch: () => Promise<T>) {
  return queryOptions({ queryKey, queryFn: fetch, staleTime: FIVE_MINUTES_MS, networkMode: 'online', retry: 1 });
}
