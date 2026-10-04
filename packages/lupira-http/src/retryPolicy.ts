/** Retries after the initial attempt — up to 3 tries total. */
export const MAX_RETRIES = 2;

const BASE_DELAY_MS = 300;
const MAX_DELAY_MS = 10_000;

/** Transient = worth retrying in-request: transport failure (0), throttling (429), or a server error. */
export function isTransientStatus(status: number): boolean {
  return status === 0 || status === 429 || status >= 500;
}

/** Reads are always replayable; writes only when the caller supplied an Idempotency-Key (the server dedups). */
export function isRetriableRequest(method: string | undefined, hasIdempotencyKey: boolean): boolean {
  const m = (method ?? 'GET').toUpperCase();
  return m === 'GET' || m === 'HEAD' || hasIdempotencyKey;
}

/** Honors a numeric Retry-After (seconds, capped); else exponential backoff with jitter. `attempt` is 0-based. */
export function retryDelayMs(attempt: number, retryAfter?: string | null, rand: () => number = Math.random): number {
  if (retryAfter) {
    const seconds = Number(retryAfter);
    if (Number.isFinite(seconds) && seconds >= 0) return Math.min(seconds * 1000, MAX_DELAY_MS);
  }
  return Math.min(BASE_DELAY_MS * 2 ** attempt + Math.floor(rand() * BASE_DELAY_MS), MAX_DELAY_MS);
}
