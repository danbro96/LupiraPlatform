import { onlineManager } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';

type State = { isConnected: boolean | null; isInternetReachable: boolean | null };
const net = vi.hoisted(() => ({ listener: null as ((s: State) => void) | null }));
vi.mock('@react-native-community/netinfo', () => ({
  default: {
    addEventListener: (cb: (s: State) => void) => {
      net.listener = cb;
      return () => undefined;
    },
  },
}));

import { connectOnlineManager, isOnline } from './online.ts';

describe('isOnline', () => {
  it('trusts internet reachability, then the link, then assumes online', () => {
    expect(isOnline({ isConnected: true, isInternetReachable: false })).toBe(false);
    expect(isOnline({ isConnected: false, isInternetReachable: null })).toBe(false);
    expect(isOnline({ isConnected: true, isInternetReachable: null })).toBe(true);
    expect(isOnline({ isConnected: null, isInternetReachable: null })).toBe(true);
  });
});

describe('connectOnlineManager', () => {
  it('drives React Query\'s online state from NetInfo', () => {
    connectOnlineManager();

    net.listener!({ isConnected: true, isInternetReachable: false });
    expect(onlineManager.isOnline()).toBe(false);
    net.listener!({ isConnected: true, isInternetReachable: true });
    expect(onlineManager.isOnline()).toBe(true);
  });
});
