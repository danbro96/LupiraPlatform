import { beforeEach, describe, expect, it, vi } from 'vitest';

const rn = vi.hoisted(() => ({ appState: null as ((s: string) => void) | null, remove: vi.fn() }));
const net = vi.hoisted(() => ({ listener: null as ((s: { isConnected: boolean | null }) => void) | null, unsubscribe: vi.fn() }));
const task = vi.hoisted(() => ({ defined: new Map<string, () => Promise<unknown>>(), register: vi.fn() }));

vi.mock('react-native', () => ({
  AppState: {
    addEventListener: (_: string, cb: (s: string) => void) => {
      rn.appState = cb;
      return { remove: rn.remove };
    },
  },
}));
vi.mock('@react-native-community/netinfo', () => ({
  default: {
    addEventListener: (cb: (s: { isConnected: boolean | null }) => void) => {
      net.listener = cb;
      return net.unsubscribe;
    },
  },
}));
vi.mock('expo-task-manager', () => ({ defineTask: (name: string, fn: () => Promise<unknown>) => task.defined.set(name, fn) }));
vi.mock('expo-background-task', () => ({ BackgroundTaskResult: { Success: 1 }, registerTaskAsync: task.register }));

import { setAuthPort } from '@danbro96/lupira-http/authPort';
import { defineSyncTask, startSyncTriggers } from './triggers.ts';

let signIn: (() => void) | null = null;
const offSignIn = vi.fn();
setAuthPort({
  getApiUrl: () => '',
  getToken: () => null,
  refresh: async () => null,
  onSignIn: (cb) => {
    signIn = cb;
    return offSignIn;
  },
});

const engine = { sync: vi.fn(async () => undefined) };

beforeEach(() => {
  engine.sync.mockClear();
  task.register.mockReset().mockResolvedValue(undefined);
});

describe('startSyncTriggers', () => {
  it('syncs now, on foreground, on reconnect and on sign-in, and registers the background task', () => {
    startSyncTriggers(engine, { backgroundTaskName: 'test-sync' });
    expect(engine.sync).toHaveBeenCalledTimes(1);
    expect(task.register).toHaveBeenCalledWith('test-sync', { minimumInterval: 15 });

    rn.appState!('background');
    net.listener!({ isConnected: false });
    expect(engine.sync).toHaveBeenCalledTimes(1);

    rn.appState!('active');
    net.listener!({ isConnected: true });
    signIn!();
    expect(engine.sync).toHaveBeenCalledTimes(4);
  });

  it('unsubscribes every foreground trigger', () => {
    startSyncTriggers(engine, { backgroundTaskName: 'test-sync' })();
    expect(rn.remove).toHaveBeenCalled();
    expect(net.unsubscribe).toHaveBeenCalled();
    expect(offSignIn).toHaveBeenCalled();
  });

  it('tolerates a device without background tasks', async () => {
    task.register.mockRejectedValue(new Error('unavailable'));
    expect(() => startSyncTriggers(engine, { backgroundTaskName: 'test-sync' })).not.toThrow();
    await Promise.resolve();
  });
});

describe('defineSyncTask', () => {
  it('runs a sync when the OS fires the task', async () => {
    defineSyncTask('test-sync', engine);
    expect(await task.defined.get('test-sync')!()).toBe(1);
    expect(engine.sync).toHaveBeenCalledTimes(1);
  });

  it('prepares before syncing', async () => {
    const order: string[] = [];
    engine.sync.mockImplementationOnce(async () => void order.push('sync'));
    defineSyncTask('test-prepared', engine, async () => void order.push('prepare'));
    await task.defined.get('test-prepared')!();
    expect(order).toEqual(['prepare', 'sync']);
  });
});
