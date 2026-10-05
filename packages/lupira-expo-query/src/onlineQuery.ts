import { queryOptions, type QueryKey } from '@tanstack/react-query';

const FIVE_MINUTES_MS = 5 * 60_000;

/** A client error (4xx) answers the same on a second try; only transient failures earn one retry. */
export function retryTransient(failureCount: number, error: unknown): boolean {
  const status = (error as { status?: unknown } | null)?.status;
  return failureCount < 1 && !(typeof status === 'number' && status >= 400 && status < 500);
}

/** A read straight from the server: paused while offline, served from cache for five minutes. */
export function onlineQuery<T, Key extends QueryKey>(queryKey: Key, fetch: () => Promise<T>) {
  return queryOptions({ queryKey, queryFn: fetch, staleTime: FIVE_MINUTES_MS, networkMode: 'online', retry: retryTransient });
}
