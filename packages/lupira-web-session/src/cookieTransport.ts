import { ApiError, NETWORK_ERROR, problemMessage } from '@danbro96/lupira-http/apiError';
import { setApiTransport, type ApiTransport } from '@danbro96/lupira-http/transport';
import { login } from './session.ts';

export interface CookieTransportOptions {
  baseUrl?: string;
  redirectOn401?: () => boolean;
}

/** The SPA's transport for every generated request. Auth rides the BFF's HttpOnly cookie session
 *  (same-origin), so we send credentials and never a bearer. A 401 means the session expired →
 *  bounce to the BFF sign-in, returning here afterwards. */
export function cookieTransport({ baseUrl = '', redirectOn401 = () => true }: CookieTransportOptions = {}): ApiTransport {
  return async <T>(url: string, init?: RequestInit): Promise<T> => {
    let res: Response;
    try {
      res = await fetch(`${baseUrl}${url}`, { credentials: 'include', ...init });
    } catch {
      throw new ApiError(0, NETWORK_ERROR);
    }
    if (!res.ok) {
      const text = await res.text().catch(() => '');
      if (res.status === 401 && redirectOn401()) login();
      throw new ApiError(res.status, problemMessage(text, res.status === 401 ? 'Not authenticated' : res.statusText || `HTTP ${res.status}`));
    }
    if (res.status === 204) return undefined as T;
    // A 200 of HTML means the SPA fallback answered a dead route; parsing it fails obscurely.
    if ((res.headers.get('content-type') ?? '').includes('text/html')) {
      throw new ApiError(res.status, `Expected data from ${url} but received the app shell.`);
    }
    return (await res.json()) as T;
  };
}

/** Installed once from main.tsx, before anything can issue a request. */
export function installCookieTransport(options?: CookieTransportOptions): void {
  setApiTransport(cookieTransport(options));
}
