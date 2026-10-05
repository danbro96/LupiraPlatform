import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const fetchDiscoveryAsync = vi.fn();
vi.mock('expo-auth-session', () => ({ fetchDiscoveryAsync: (issuer: string) => fetchDiscoveryAsync(issuer) }));

import { RefreshError, createOidcClient, decodeJwt, hasAudience } from './oidc.ts';

let fetchMock: ReturnType<typeof vi.fn>;
const log = vi.fn();
const client = () => createOidcClient({ issuer: 'https://auth.test/app', clientId: 'app', timeoutMs: 5000, log });

beforeEach(() => {
  fetchDiscoveryAsync.mockReset().mockResolvedValue({ tokenEndpoint: 'https://auth.test/token' });
  fetchMock = vi.fn();
  vi.stubGlobal('fetch', fetchMock);
  log.mockClear();
});

afterEach(() => vi.unstubAllGlobals());

const reply = (status: number, body: string) => fetchMock.mockResolvedValueOnce(new Response(body, { status }));
const form = (call: number) => new URLSearchParams(fetchMock.mock.calls[call][1].body as string);

describe('exchangeAuthCode', () => {
  it('posts the PKCE form to the discovered endpoint and maps the token set', async () => {
    reply(200, '{"access_token":"a","refresh_token":"r","id_token":"i","expires_in":300}');
    const t = await client().exchangeAuthCode({ code: 'c', redirectUri: 'app://cb', codeVerifier: 'v' });
    expect(t).toEqual({ accessToken: 'a', refreshToken: 'r', idToken: 'i', expiresIn: 300 });
    expect(fetchMock.mock.calls[0][0]).toBe('https://auth.test/token');
    expect(Object.fromEntries(form(0))).toEqual({ grant_type: 'authorization_code', code: 'c', redirect_uri: 'app://cb', client_id: 'app', code_verifier: 'v' });
    expect(fetchDiscoveryAsync).toHaveBeenCalledWith('https://auth.test/app');
  });

  it('surfaces a failing status with its body and never logs a successful body', async () => {
    reply(200, '{"access_token":"secret"}');
    await client().exchangeAuthCode({ code: 'c', redirectUri: 'app://cb', tokenEndpoint: 'https://auth.test/t2' });
    expect(log.mock.calls.flat().join(' ')).not.toContain('secret');
    reply(400, 'invalid_grant');
    await expect(client().exchangeAuthCode({ code: 'c', redirectUri: 'app://cb' })).rejects.toThrow('token 400: invalid_grant');
  });
});

describe('refreshTokens', () => {
  it('rotates through the refresh grant', async () => {
    reply(200, '{"access_token":"a2","refresh_token":"r2"}');
    expect(await client().refreshTokens('r1')).toMatchObject({ accessToken: 'a2', refreshToken: 'r2' });
    expect(form(0).get('grant_type')).toBe('refresh_token');
  });

  it('classifies 400/401 as definitive and everything else as transient', async () => {
    const c = client();
    reply(401, 'invalid_grant');
    await expect(c.refreshTokens('r')).rejects.toMatchObject({ definitive: true });
    reply(503, '');
    await expect(c.refreshTokens('r')).rejects.toMatchObject({ definitive: false });
    fetchMock.mockRejectedValueOnce(new TypeError('offline'));
    await expect(c.refreshTokens('r')).rejects.toMatchObject({ definitive: false });
  });

  it('does not cache a failed discovery', async () => {
    const c = client();
    fetchDiscoveryAsync.mockRejectedValueOnce(new Error('blip'));
    const err = await c.refreshTokens('r').catch((e: unknown) => e);
    expect(err).toBeInstanceOf(RefreshError);
    reply(200, '{"access_token":"a"}');
    expect(await c.refreshTokens('r')).toMatchObject({ accessToken: 'a' });
    expect(fetchDiscoveryAsync).toHaveBeenCalledTimes(2);
  });
});

describe('decodeJwt', () => {
  it('reads the claims of a url-safe, unpadded payload', () => {
    const bytes = new TextEncoder().encode(JSON.stringify({ email: 'ö@test', sub: 's' }));
    const payload = btoa(String.fromCharCode(...bytes)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
    expect(decodeJwt(`h.${payload}.sig`)).toEqual({ email: 'ö@test', sub: 's' });
  });

  it('gives nothing for garbage', () => {
    expect(decodeJwt('nope')).toEqual({});
    expect(decodeJwt('h.!!!.s')).toEqual({});
  });
});

describe('hasAudience', () => {
  const jwt = (claims: Record<string, unknown>) => `h.${Buffer.from(JSON.stringify(claims)).toString('base64url')}.s`;

  it('matches an audience array or a single audience string', () => {
    expect(hasAudience(jwt({ aud: ['lupira-photos-mobile', 'lupira-geo'] }), 'lupira-geo')).toBe(true);
    expect(hasAudience(jwt({ aud: 'lupira-geo' }), 'lupira-geo')).toBe(true);
  });

  it('is false without the audience, without a token, or for garbage', () => {
    expect(hasAudience(jwt({ aud: ['lupira-photo'] }), 'lupira-geo')).toBe(false);
    expect(hasAudience(jwt({}), 'lupira-geo')).toBe(false);
    expect(hasAudience(null, 'lupira-geo')).toBe(false);
    expect(hasAudience('not-a-jwt', 'lupira-geo')).toBe(false);
  });
});
