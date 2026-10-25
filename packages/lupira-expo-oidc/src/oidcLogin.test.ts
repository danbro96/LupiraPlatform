import type * as AuthSession from 'expo-auth-session';
import { describe, expect, it, vi } from 'vitest';
import type { TokenSet } from './oidc.ts';
import { initialOidcLoginState, oidcLoginReducer, oidcLoginStatus, runSignIn, type OidcLoginEvent } from './oidcLogin.ts';

const discovery: AuthSession.DiscoveryDocument = { authorizationEndpoint: 'https://auth.test/authorize', tokenEndpoint: 'https://auth.test/token' };
const replay = (...events: OidcLoginEvent[]) => events.reduce(oidcLoginReducer, initialOidcLoginState);

describe('oidcLoginStatus', () => {
  it('loads until both discovery and the auth request are ready', () => {
    expect(oidcLoginStatus(initialOidcLoginState, false)).toBe('loading');
    expect(oidcLoginStatus(replay({ type: 'discovered', discovery }), false)).toBe('loading');
    expect(oidcLoginStatus(replay({ type: 'discovered', discovery }), true)).toBe('ready');
  });

  it('is unavailable with the reason after a failed discovery, and loading again on retry', () => {
    const failed = replay({ type: 'discoveryFailed', error: 'discovery 503: ' });
    expect(oidcLoginStatus(failed, false)).toBe('unavailable');
    expect(failed.error).toBe('discovery 503: ');

    const retried = oidcLoginReducer(failed, { type: 'retry' });
    expect(oidcLoginStatus(retried, false)).toBe('loading');
    expect(retried).toMatchObject({ attempt: 1, error: null });
  });

  it('is busy during a sign-in and ready with its error afterwards', () => {
    const busy = replay({ type: 'discovered', discovery }, { type: 'signInStarted' });
    expect(oidcLoginStatus(busy, true)).toBe('busy');
    const failed = oidcLoginReducer(busy, { type: 'signInEnded', error: 'token 400: invalid_grant' });
    expect(oidcLoginStatus(failed, true)).toBe('ready');
    expect(failed.error).toBe('token 400: invalid_grant');
    expect(oidcLoginReducer(failed, { type: 'signInStarted' }).error).toBeNull();
  });
});

describe('runSignIn', () => {
  const tokens: TokenSet = { accessToken: 'a', refreshToken: 'r', expiresIn: 300 };
  const success = { type: 'success', errorCode: null, params: { code: 'c' }, authentication: null, url: 'app://cb?code=c' } as AuthSession.AuthSessionResult;

  function steps(result: AuthSession.AuthSessionResult | Error) {
    return {
      prompt: vi.fn(() => (result instanceof Error ? Promise.reject(result) : Promise.resolve(result))),
      exchange: vi.fn((_code: string) => Promise.resolve(tokens)),
      onTokens: vi.fn((_t: TokenSet) => Promise.resolve()),
      log: vi.fn(),
    };
  }

  it('exchanges the code and hands over the tokens', async () => {
    const s = steps(success);
    expect(await runSignIn(s)).toBeNull();
    expect(s.exchange).toHaveBeenCalledWith('c');
    expect(s.onTokens).toHaveBeenCalledWith(tokens);
  });

  it('stays quiet when the user backs out', async () => {
    const s = steps({ type: 'dismiss' });
    expect(await runSignIn(s)).toBeNull();
    expect(s.exchange).not.toHaveBeenCalled();
  });

  it('surfaces a provider error, a failed exchange and a throwing prompt', async () => {
    const providerError = { ...success, type: 'error', error: new Error('access_denied') } as AuthSession.AuthSessionResult;
    expect(await runSignIn(steps(providerError))).toBe('access_denied');

    const s = steps(success);
    s.exchange.mockRejectedValue(new Error('token 400: invalid_grant'));
    expect(await runSignIn(s)).toBe('token 400: invalid_grant');
    expect(s.onTokens).not.toHaveBeenCalled();

    expect(await runSignIn(steps(new Error('no browser')))).toBe('no browser');
  });
});
