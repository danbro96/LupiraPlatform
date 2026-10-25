import { TransformRequestManager } from '@maplibre/maplibre-react-native';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { installMapAuthHeader } from './mapAuthHeader.ts';

vi.mock('@maplibre/maplibre-react-native', () => ({
  TransformRequestManager: { addHeader: vi.fn(), removeHeader: vi.fn() },
}));

const addHeader = vi.mocked(TransformRequestManager.addHeader);
const removeHeader = vi.mocked(TransformRequestManager.removeHeader);

function authSource(token: string | null, apiUrl = 'https://maps-api.lupira.com/') {
  const state = { token, apiUrl };
  const listeners = new Set<() => void>();
  return {
    state,
    listeners,
    set(next: Partial<typeof state>) {
      Object.assign(state, next);
      listeners.forEach((l) => l());
    },
    source: {
      subscribeToken: (listener: () => void) => {
        listeners.add(listener);
        return () => listeners.delete(listener);
      },
      getToken: () => state.token,
      apiUrl: () => state.apiUrl,
    },
  };
}

beforeEach(() => {
  addHeader.mockClear();
  removeHeader.mockClear();
});

describe('installMapAuthHeader', () => {
  it('adds the bearer scoped to the API origin', () => {
    installMapAuthHeader(authSource('t1').source);
    expect(addHeader).toHaveBeenCalledExactlyOnceWith({
      id: 'lupira-auth',
      name: 'Authorization',
      value: 'Bearer t1',
      match: '^https://maps-api\\.lupira\\.com/',
    });
    const match = new RegExp(addHeader.mock.calls[0]![0].match as string);
    expect(match.test('https://maps-api.lupira.com/tiles/1')).toBe(true);
    expect(match.test('https://maps-apiXlupira.com/tiles/1')).toBe(false);
  });

  it('removes the header when the token goes away', () => {
    const auth = authSource('t1');
    installMapAuthHeader(auth.source);
    auth.set({ token: null });
    expect(removeHeader).toHaveBeenCalledExactlyOnceWith('lupira-auth');
  });

  it('updates the header on token rotation', () => {
    const auth = authSource('t1');
    installMapAuthHeader(auth.source);
    auth.set({ token: 't2' });
    expect(addHeader).toHaveBeenCalledTimes(2);
    expect(addHeader).toHaveBeenLastCalledWith(expect.objectContaining({ value: 'Bearer t2' }));
  });

  it('skips native calls when nothing changed', () => {
    const auth = authSource('t1');
    installMapAuthHeader(auth.source);
    auth.set({});
    auth.set({ apiUrl: 'https://maps-api.lupira.com' });
    expect(addHeader).toHaveBeenCalledTimes(1);

    auth.set({ token: null });
    auth.set({ apiUrl: 'https://other.example' });
    expect(removeHeader).toHaveBeenCalledTimes(1);
  });

  it('re-scopes the header when the API origin changes', () => {
    const auth = authSource('t1');
    installMapAuthHeader(auth.source);
    auth.set({ apiUrl: 'https://cal-api.lupira.com' });
    expect(addHeader).toHaveBeenLastCalledWith(expect.objectContaining({ match: '^https://cal-api\\.lupira\\.com/' }));
  });

  it('unsubscribes and removes the header on uninstall', () => {
    const auth = authSource('t1');
    const uninstall = installMapAuthHeader(auth.source);
    uninstall();
    expect(auth.listeners.size).toBe(0);
    expect(removeHeader).toHaveBeenCalledExactlyOnceWith('lupira-auth');
  });
});
