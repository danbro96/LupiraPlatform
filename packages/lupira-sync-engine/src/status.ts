export type SyncPhase = 'idle' | 'push' | 'pull';

/** Docs applied so far for the aggregate being pulled; feeds page without a total. */
export interface SyncProgress {
  aggregate: string;
  count: number;
}

export interface SyncStatus {
  phase: SyncPhase;
  progress: SyncProgress | null;
  pending: number;
  parked: number;
  serverReachable: boolean;
  lastSyncAt: number | null;
  lastError: string | null;
}

/** `useSyncExternalStore(status.subscribe, status.getSnapshot)`. */
export interface StatusStore {
  getSnapshot(): SyncStatus;
  subscribe(listener: () => void): () => void;
}

export interface StatusWriter extends StatusStore {
  set(patch: Partial<SyncStatus>): void;
}

export const INITIAL_STATUS: SyncStatus = {
  phase: 'idle',
  progress: null,
  pending: 0,
  parked: 0,
  serverReachable: true,
  lastSyncAt: null,
  lastError: null,
};

export function createStatusStore(): StatusWriter {
  let snapshot = INITIAL_STATUS;
  const listeners = new Set<() => void>();
  return {
    getSnapshot: () => snapshot,
    subscribe(listener) {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    set(patch) {
      snapshot = { ...snapshot, ...patch };
      for (const listener of listeners) listener();
    },
  };
}
