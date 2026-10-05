import type { SyncStatus } from './status.ts';

export interface BannerInput extends Pick<SyncStatus, 'phase' | 'progress' | 'pending' | 'parked' | 'serverReachable' | 'lastError'> {
  /** The device's own connectivity, from the app. */
  online: boolean;
}

export type BannerKind = 'offline' | 'syncing' | 'unreachable' | 'parked' | 'error';

export interface BannerState {
  kind: BannerKind;
  text: string;
  /** Show only an activity line, no strip: a routine sync is not worth a row of the screen. */
  quiet: boolean;
}

/** Past this many docs a sync is a bulk one (first or full), long enough that its count is worth a strip. */
export const BULK_SYNC_COUNT = 100;

/** Priority: device offline → sync running → server unreachable → parked changes → last error. `labels` names each
 *  aggregate's docs in the progress line ("events"); the aggregate name is the fallback. */
export function bannerState(s: BannerInput, labels: Record<string, string> = {}): BannerState | null {
  if (!s.online) return { kind: 'offline', text: withQueued('Offline', s.pending), quiet: false };
  if (s.phase !== 'idle') {
    const p = s.progress;
    return {
      kind: 'syncing',
      text: p && p.count > 0 ? `Syncing — ${p.count} ${labels[p.aggregate] ?? p.aggregate}…` : 'Syncing…',
      quiet: !p || p.count < BULK_SYNC_COUNT,
    };
  }
  if (!s.serverReachable) return { kind: 'unreachable', text: withQueued("Can't reach server", s.pending), quiet: false };
  if (s.parked > 0) {
    return { kind: 'parked', text: `${changes(s.parked)} need${s.parked === 1 ? 's' : ''} attention`, quiet: false };
  }
  if (s.lastError) return { kind: 'error', text: 'Sync problem — tap for details', quiet: false };
  return null;
}

const withQueued = (label: string, pending: number): string =>
  pending > 0 ? `${label} — ${changes(pending)} queued` : label;

const changes = (n: number): string => `${n} change${n === 1 ? '' : 's'}`;
