/** Structural twins of `lupira-sync-engine`'s `BannerState`, `SyncStatus` and `ParkedOp`, so the kit renders
 *  them without depending on the engine, and an app without sync can feed the banner too. */
export type StatusBannerKind = 'offline' | 'syncing' | 'unreachable' | 'parked' | 'error';

export interface StatusBannerState {
  kind: StatusBannerKind;
  text: string;
  quiet: boolean;
}

export interface SyncViewStatus {
  phase: string;
  progress: { aggregate: string; count: number } | null;
  pending: number;
  serverReachable: boolean;
  lastSyncAt: number | null;
  lastError: string | null;
}

export interface ParkedChangeOp {
  commandId: string;
  occurredAt: string;
  kind: string;
}

export interface ParkedChange<Op extends ParkedChangeOp = ParkedChangeOp> {
  op: Op;
  attempts: number;
  lastError: string | null;
}

/** The banner for an app whose only status is the device's connectivity. */
export function offlineBannerState(online: boolean): StatusBannerState | null {
  return online ? null : { kind: 'offline', text: 'Offline', quiet: false };
}

export function syncStatusLine(status: Pick<SyncViewStatus, 'phase' | 'serverReachable' | 'lastSyncAt'>, online = true): string {
  const state = !online ? 'Offline' : status.phase !== 'idle' ? 'Syncing…' : status.serverReachable ? 'Server reachable' : "Can't reach server";
  return status.lastSyncAt ? `${state} · last sync ${new Date(status.lastSyncAt).toLocaleTimeString()}` : state;
}

export const changeCount = (n: number): string => `${n} change${n === 1 ? '' : 's'}`;
