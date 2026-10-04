import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError, NETWORK_ERROR } from '@danbro96/lupira-http/apiError';
import { apiRequest } from '@danbro96/lupira-http/transport';
import { cookieTransport, installCookieTransport } from './cookieTransport.ts';

let assign: ReturnType<typeof vi.fn>;
let fetchMock: ReturnType<typeof vi.fn>;

beforeEach(() => {
  assign = vi.fn();
  fetchMock = vi.fn();
  vi.stubGlobal('window', { location: { pathname: '/items', search: '?item=1', assign } });
  vi.stubGlobal('fetch', fetchMock);
});

afterEach(() => vi.unstubAllGlobals());

const respond = (status: number, body: string | null, contentType = 'application/json') =>
  fetchMock.mockResolvedValueOnce(new Response(body, { status, headers: { 'content-type': contentType } }));

describe('cookieTransport', () => {
  it('sends the session cookie and returns the parsed body', async () => {
    respond(200, '{"ok":true}');
    installCookieTransport({ baseUrl: '/api' });
    await expect(apiRequest('/lists')).resolves.toEqual({ ok: true });
    expect(fetchMock).toHaveBeenCalledWith('/api/lists', expect.objectContaining({ credentials: 'include' }));
  });

  it('bounces a 401 to the BFF sign-in, returning to the current path', async () => {
    respond(401, '');
    await expect(cookieTransport()('/me')).rejects.toMatchObject({ status: 401, message: 'Not authenticated' });
    expect(assign).toHaveBeenCalledWith('/auth/login?returnUrl=%2Fitems%3Fitem%3D1');
  });

  it('stays put on a 401 when redirecting is off', async () => {
    respond(401, '{"title":"Share link expired"}');
    await expect(cookieTransport({ redirectOn401: () => false })('/shared')).rejects.toMatchObject({ status: 401, message: 'Share link expired' });
    expect(assign).not.toHaveBeenCalled();
  });

  it('surfaces a problem document’s detail', async () => {
    respond(409, '{"title":"Conflict","detail":"Already a member"}', 'application/problem+json');
    await expect(cookieTransport()('/x')).rejects.toMatchObject({ status: 409, message: 'Already a member' });
  });

  it('refuses the SPA shell in place of data', async () => {
    respond(200, '<!doctype html>', 'text/html');
    await expect(cookieTransport()('/gone')).rejects.toThrow('received the app shell');
  });

  it('returns undefined for 204 and maps a transport failure to status 0', async () => {
    respond(204, null);
    await expect(cookieTransport()('/x')).resolves.toBeUndefined();
    fetchMock.mockRejectedValueOnce(new TypeError('offline'));
    const err = await cookieTransport()('/x').catch((e: unknown) => e);
    expect(err).toBeInstanceOf(ApiError);
    expect(err).toMatchObject({ status: 0, message: NETWORK_ERROR });
  });
});
