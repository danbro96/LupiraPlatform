import { describe, expect, it } from 'vitest';
import { BULK_SYNC_COUNT, bannerState, type BannerInput } from './bannerState.ts';

const healthy: BannerInput = {
  online: true,
  phase: 'idle',
  progress: null,
  pending: 0,
  parked: 0,
  serverReachable: true,
  lastError: null,
};
const labels = { 'cal.item': 'events' };
const state = (over: Partial<BannerInput> = {}) => bannerState({ ...healthy, ...over }, labels);

describe('bannerState', () => {
  it('says nothing when there is nothing to report', () => {
    expect(state()).toBeNull();
  });

  it('reports the aggregate and count while syncing, falling back to the aggregate name', () => {
    expect(state({ phase: 'pull', progress: { aggregate: 'cal.item', count: 12 } })?.text).toBe('Syncing — 12 events…');
    expect(state({ phase: 'pull', progress: { aggregate: 'contact', count: 3 } })?.text).toBe('Syncing — 3 contact…');
    expect(state({ phase: 'push' })?.text).toBe('Syncing…');
    expect(state({ phase: 'pull', progress: { aggregate: 'cal.item', count: 0 } })?.text).toBe('Syncing…');
  });

  it('keeps a routine sync quiet and gives a bulk one its strip', () => {
    expect(state({ phase: 'push' })?.quiet).toBe(true);
    expect(state({ phase: 'pull', progress: { aggregate: 'cal.item', count: BULK_SYNC_COUNT - 1 } })?.quiet).toBe(true);
    expect(state({ phase: 'pull', progress: { aggregate: 'cal.item', count: BULK_SYNC_COUNT } })?.quiet).toBe(false);
  });

  it('counts queued changes while offline or unreachable, and pluralises', () => {
    expect(state({ online: false })?.text).toBe('Offline');
    expect(state({ online: false, pending: 1 })?.text).toBe('Offline — 1 change queued');
    expect(state({ serverReachable: false })).toEqual({ kind: 'unreachable', text: "Can't reach server", quiet: false });
    expect(state({ serverReachable: false, pending: 3 })?.text).toBe("Can't reach server — 3 changes queued");
  });

  it('surfaces parked changes, then a lingering error', () => {
    expect(state({ parked: 1 })).toMatchObject({ kind: 'parked', text: '1 change needs attention' });
    expect(state({ parked: 2 })?.text).toBe('2 changes need attention');
    expect(state({ lastError: 'boom' })).toMatchObject({ kind: 'error', text: 'Sync problem — tap for details' });
  });

  it('ranks offline over syncing, syncing over unreachable, unreachable over parked, parked over error', () => {
    expect(state({ online: false, phase: 'pull' })?.kind).toBe('offline');
    expect(state({ phase: 'pull', serverReachable: false })?.kind).toBe('syncing');
    expect(state({ serverReachable: false, parked: 1 })?.kind).toBe('unreachable');
    expect(state({ parked: 1, lastError: 'boom' })?.kind).toBe('parked');
  });
});
