/** Park-don't-wedge: what the outbox drain does with a failed replay. Pure and total so the whole decision
 *  table is unit-tested. Throttling (429) is transient, not a semantic conflict. */
export type ReplayDecision = {
  outcome: 'pause' | 'park' | 'retry';
  /** Stop the drain loop (park lets the queue continue past the corpse). */
  stop: boolean;
  reason: string;
};

/** Classifies an HTTP status; 0 = transport failure (no response). */
export function classifyReplayStatus(status: number): ReplayDecision {
  // The mutator already spent its one forced re-auth; a surviving 401 means the session is gone.
  if (status === 401) return { outcome: 'pause', stop: true, reason: 'signed out' };
  if (status === 0 || status === 429 || status >= 500)
    return { outcome: 'retry', stop: true, reason: `transient (${status})` };
  // Remaining 4xx: a semantic conflict this op can never win (404 deleted target, 400, 403, 409).
  return { outcome: 'park', stop: false, reason: `rejected (${status})` };
}

/** Classifies a thrown replay failure: an `Error` with a numeric `status` (the apps' `ApiError`) by status. */
export function classifyReplayError(e: unknown): ReplayDecision {
  const status = e instanceof Error ? (e as { status?: unknown }).status : undefined;
  if (typeof status === 'number') return classifyReplayStatus(status);
  // Non-HTTP throw = a client bug; it would fail identically forever — park it for review.
  return { outcome: 'park', stop: false, reason: `client error: ${String(e)}` };
}
