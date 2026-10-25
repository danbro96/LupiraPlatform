import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { DiscoveryError, RefreshError, createOidcClient, decodeJwt, hasAudience } from './oidc.ts';

let fetchMock: ReturnType<typeof vi.fn>;
let discoveryReplies: (() => Promise<Response>)[];
let tokenReplies: (() => Promise<Response>)[];
const log = vi.fn();
const client = () => createOidcClient({ issuer: 'https://auth.test/app/', clientId: 'app', timeoutMs: 5000, log });
const discoveryUrl = 'https://auth.test/app/.well-known/openid-configuration';

beforeEach(() => {
  discoveryReplies = [];
  tokenReplies = [];
  fetchMock = vi.fn((url: string) => {
    if (url === discoveryUrl) return discoveryReplies.shift()?.() ?? Promise.resolve(new Response('{"token_endpoint":"https://auth.test/token"}'));
    return tokenReplies.shift()?.() ?? Promise.reject(new Error(`unexpected fetch ${url}`));
  });
  vi.stubGlobal('fetch', fetchMock);
  log.mockClear();
});

afterEach(() => vi.unstubAllGlobals());

const reply = (status: number, body: string) => tokenReplies.push(() => Promise.resolve(new Response(body, { status })));
const discoveryReply = (status: number, body: string) => discoveryReplies.push(() => Promise.resolve(new Response(body, { status })));
const tokenCalls = () => fetchMock.mock.calls.filter(([url]) => url !== discoveryUrl);
const discoveryCalls = () => fetchMock.mock.calls.filter(([url]) => url === discoveryUrl);
const form = (call: number) => new URLSearchParams(tokenCalls()[call][1].body as string);

describe('getDiscovery', () => {
  it('maps the provider metadata like expo-auth-session does', async () => {
    const metadata = {
      issuer: 'https://auth.test/app/',
      authorization_endpoint: 'https://auth.test/authorize',
      token_endpoint: 'https://auth.test/token',
      revocation_endpoint: 'https://auth.test/revoke',
      userinfo_endpoint: 'https://auth.test/userinfo',
      end_session_endpoint: 'https://auth.test/end',
      registration_endpoint: 'https://auth.test/register',
    };
    discoveryReply(200, JSON.stringify(metadata));
    expect(await client().getDiscovery()).toEqual({
      discoveryDocument: metadata,
      authorizationEndpoint: 'https://auth.test/authorize',
      tokenEndpoint: 'https://auth.test/token',
      revocationEndpoint: 'https://auth.test/revoke',
      userInfoEndpoint: 'https://auth.test/userinfo',
      endSessionEndpoint: 'https://auth.test/end',
      registrationEndpoint: 'https://auth.test/register',
    });
      });

  it.each([
    ['an empty body', 200, '', 'discovery 200: empty body'],
    ['a failing status', 503, 'down', 'discovery 503: down'],
    ['a body that is not JSON', 200, '<html>', 'discovery 200: not a JSON object'],
    ['JSON that is not an object', 200, 'null', 'discovery 200: not a JSON object'],
  ])('rejects %s with a DiscoveryError', async (_, status, body, msg) => {
    discoveryReply(status, body);
    const err = await client().getDiscovery().catch((e: unknown) => e);
    expect(err).toBeInstanceOf(DiscoveryError);
    expect((err as Error).message).toContain(msg);
  });

  it('refetches after a failure and memoises a success', async () => {
    const c = client();
    discoveryReply(200, '');
    await expect(c.getDiscovery()).rejects.toBeInstanceOf(DiscoveryError);
    expect(await c.getDiscovery()).toMatchObject({ tokenEndpoint: 'https://auth.test/token' });
    await c.getDiscovery();
    expect(discoveryCalls()).toHaveLength(2);
  });
});

describe('exchangeAuthCode', () => {
  it('posts the PKCE form to the discovered endpoint and maps the token set', async () => {
    reply(200, '{"access_token":"a","refresh_token":"r","id_token":"i","expires_in":300}');
    const t = await client().exchangeAuthCode({ code: 'c', redirectUri: 'app://cb', codeVerifier: 'v' });
    expect(t).toEqual({ accessToken: 'a', refreshToken: 'r', idToken: 'i', expiresIn: 300 });
    expect(tokenCalls()[0][0]).toBe('https://auth.test/token');
    expect(Object.fromEntries(form(0))).toEqual({ grant_type: 'authorization_code', code: 'c', redirect_uri: 'app://cb', client_id: 'app', code_verifier: 'v' });
    expect(discoveryCalls()).toHaveLength(1);
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
    tokenReplies.push(() => Promise.reject(new TypeError('offline')));
    await expect(c.refreshTokens('r')).rejects.toMatchObject({ definitive: false });
  });

  it('does not cache a failed discovery', async () => {
    const c = client();
    discoveryReplies.push(() => Promise.reject(new TypeError('blip')));
    const err = await c.refreshTokens('r').catch((e: unknown) => e);
    expect(err).toMatchObject({ definitive: false });
    expect(err).toBeInstanceOf(RefreshError);
    reply(200, '{"access_token":"a"}');
    expect(await c.refreshTokens('r')).toMatchObject({ accessToken: 'a' });
    expect(discoveryCalls()).toHaveLength(2);
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
