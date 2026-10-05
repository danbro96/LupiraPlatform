import NetInfo, { type NetInfoState } from '@react-native-community/netinfo';
import { onlineManager } from '@tanstack/react-query';
import { useSyncExternalStore } from 'react';

/** Internet reachability when NetInfo knows it, else link state; unknown counts as online so startup reads don't pause. */
export function isOnline(state: Pick<NetInfoState, 'isConnected' | 'isInternetReachable'>): boolean {
  return state.isInternetReachable ?? state.isConnected ?? true;
}

/** Call once at startup: React Query then pauses online queries and mutations while the device is offline. */
export function connectOnlineManager(): void {
  onlineManager.setEventListener((setOnline) => NetInfo.addEventListener((state) => setOnline(isOnline(state))));
}

const subscribe = (onChange: () => void) => onlineManager.subscribe(onChange);

export function useOnline(): boolean {
  return useSyncExternalStore(subscribe, () => onlineManager.isOnline());
}
