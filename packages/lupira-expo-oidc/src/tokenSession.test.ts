import { beforeEach, describe, expect, it, vi } from 'vitest';

const store = new Map<string, string>();
vi.mock('expo-secure-store', () => ({
  getItemAsync: vi.fn((k: string) => Promise.resolve(store.get(k) ?? null)),
  setItemAsync: vi.fn((k: string, v: string) => {
    store.set(k, v);
    return Promise.resolve();
  }),
  deleteItemAsync: vi.fn((k: string) => {
    store.delete(k);
    return Promise.resolve();
  }),
}));
vi.mock('expo-auth-session', () => ({ fetchDiscoveryAsync: vi.fn() }));

import { RefreshError, type TokenSet } from './oidc.ts';
import { createTokenRefresher, secureSessionStore, type StoredSession } from './tokenSession.ts';

let session: StoredSession;
const refreshTokens = vi.fn<(rt: string) => Promise<TokenSet>>();
const signOut = vi.fn(async () => {
  session = { token: null, refreshToken: null, expiresAt: 0 };
});
const onDefinitiveFailure = vi.fn();
const refresh = createTokenRefresher({
  read: () => session,
  refreshTokens,
  onDefinitiveFailure,
  apply: async (t, previous) => {
    session = { token: t.accessToken, refreshToken: t.refreshToken ?? previous, expiresAt: Date.now() + (t.expiresIn ?? 3600) * 1000 };
  },
  signOut,
});

const seed = (expiresInMs: number) => {
  session = { token: 'tok-1', refreshToken: 'rt-1', expiresAt: Date.now() + expiresInMs };
};

beforeEach(() => {
  store.clear();
  refreshTokens.mockReset();
  signOut.mockClear();
  onDefinitiveFailure.mockClear();
});

describe('createTokenRefresher', () => {
  it('stands pat on a fresh token without a forced refresh', async () => {
    seed(3_600_000);
    expect(await refresh()).toBe('tok-1');
    expect(refreshTokens).not.toHaveBeenCalled();
  });

  it('force refreshes even when fresh, and adopts the new token', async () => {
    seed(3_600_000);
    refreshTokens.mockResolvedValue({ accessToken: 'tok-2', refreshToken: 'rt-2', expiresIn: 3600 });
    expect(await refresh({ force: true, sentToken: 'tok-1' })).toBe('tok-2');
    expect(session.refreshToken).toBe('rt-2');
  });

  it('coalesces concurrent refreshes into one token-endpoint call', async () => {
    seed(10_000);
    let release!: (v: TokenSet) => void;
    refreshTokens.mockReturnValue(new Promise((r) => { release = r; }));

    const a = refresh();
    const b = refresh();
    release({ accessToken: 'tok-2', refreshToken: 'rt-2', expiresIn: 3600 });

    expect(await a).toBe('tok-2');
    expect(await b).toBe('tok-2');
    expect(refreshTokens).toHaveBeenCalledTimes(1);
  });

  it('is rotation-safe: a 401 about an already-replaced token does not rotate again', async () => {
    seed(3_600_000);
    session = { ...session, token: 'tok-2', refreshToken: 'rt-2' };
    expect(await refresh({ force: true, sentToken: 'tok-1' })).toBe('tok-2');
    expect(refreshTokens).not.toHaveBeenCalled();
  });

  it('signs out on a definitive failure', async () => {
    seed(10_000);
    refreshTokens.mockRejectedValue(new RefreshError(true, 'invalid_grant'));
    expect(await refresh({ force: true })).toBeNull();
    expect(signOut).toHaveBeenCalledTimes(1);
    expect(onDefinitiveFailure).toHaveBeenCalledWith(expect.objectContaining({ message: 'invalid_grant' }));
  });

  it('keeps the session on a transient failure and returns the same token', async () => {
    seed(10_000);
    refreshTokens.mockRejectedValue(new RefreshError(false, '503'));
    expect(await refresh({ force: true })).toBe('tok-1');
    expect(signOut).not.toHaveBeenCalled();
    expect(onDefinitiveFailure).not.toHaveBeenCalled();
  });

  it('settles a synchronous transient failure and lets the next call refresh again', async () => {
    seed(10_000);
    refreshTokens.mockImplementationOnce(() => { throw new Error('boom'); });
    expect(await refresh({ force: true })).toBe('tok-1');
    refreshTokens.mockResolvedValue({ accessToken: 'tok-2', expiresIn: 3600 });
    expect(await refresh({ force: true })).toBe('tok-2');
  });

  it('signs out on a forced refresh with no refresh token, but limps along proactively', async () => {
    session = { token: 'tok-1', refreshToken: null, expiresAt: 0 };
    expect(await refresh()).toBe('tok-1');
    expect(await refresh({ force: true })).toBeNull();
    expect(signOut).toHaveBeenCalledTimes(1);
  });

  it('keeps the previous refresh token when the endpoint rotates without issuing one', async () => {
    seed(10_000);
    refreshTokens.mockResolvedValue({ accessToken: 'tok-2', expiresIn: 3600 });
    await refresh({ force: true });
    expect(session.refreshToken).toBe('rt-1');
  });

  it('keeps the current token when the response carries no access token', async () => {
    seed(10_000);
    refreshTokens.mockResolvedValue({ accessToken: '' });
    expect(await refresh({ force: true })).toBe('tok-1');
  });

  it('sends nothing without a session', async () => {
    session = { token: null, refreshToken: null, expiresAt: 0 };
    expect(await refresh({ force: true })).toBeNull();
    expect(refreshTokens).not.toHaveBeenCalled();
  });
});

describe('secureSessionStore', () => {
  it('round-trips a session under the prefix and clears it', async () => {
    const s = secureSessionStore('lupira.calendar');
    await s.save({ token: 'tok-9', refreshToken: 'rt-9', expiresAt: 123 });
    expect(store.get('lupira.calendar.token')).toBe('tok-9');
    expect(await s.load()).toEqual({ token: 'tok-9', refreshToken: 'rt-9', expiresAt: 123 });

    await s.save({ token: 'tok-10', refreshToken: null, expiresAt: 456 });
    expect(store.has('lupira.calendar.refreshToken')).toBe(false);

    await s.clear();
    expect(await s.load()).toEqual({ token: null, refreshToken: null, expiresAt: 0 });
  });
});
