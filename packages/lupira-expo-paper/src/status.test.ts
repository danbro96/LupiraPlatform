import { describe, expect, it } from 'vitest';
import { changeCount, offlineBannerState, syncStatusLine } from './status.ts';

describe('offlineBannerState', () => {
  it('is null online', () => {
    expect(offlineBannerState(true)).toBeNull();
  });

  it('is an offline strip offline', () => {
    expect(offlineBannerState(false)).toEqual({ kind: 'offline', text: 'Offline', quiet: false });
  });
});

describe('syncStatusLine', () => {
  const idle = { phase: 'idle', serverReachable: true, lastSyncAt: null };

  it('puts a running sync first', () => {
    expect(syncStatusLine({ ...idle, phase: 'pull', serverReachable: false }, false)).toBe('Syncing…');
  });

  it('puts the device being offline before the server', () => {
    expect(syncStatusLine({ ...idle, serverReachable: false }, false)).toBe('Offline');
  });

  it('names an unreachable server', () => {
    expect(syncStatusLine({ ...idle, serverReachable: false })).toBe("Can't reach server");
  });

  it('appends the last sync time', () => {
    expect(syncStatusLine({ ...idle, lastSyncAt: Date.now() })).toMatch(/^Server reachable · last sync /);
  });
});

describe('changeCount', () => {
  it('pluralises', () => {
    expect([changeCount(1), changeCount(3)]).toEqual(['1 change', '3 changes']);
  });
});
