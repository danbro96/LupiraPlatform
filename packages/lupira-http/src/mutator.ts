import { ApiError, NETWORK_ERROR } from './apiError.ts';
import type { AuthPort } from './authPort.ts';
import { MAX_RETRIES, isRetriableRequest, isTransientStatus, retryDelayMs } from './retryPolicy.ts';

export type ApiMutator = <T>(url: string, init?: RequestInit) => Promise<T>;

export interface BearerMutatorOptions {
  auth: AuthPort | (() => AuthPort);
  baseUrl?: string | (() => string);
  timeoutMs: number;
  retry?: false | { maxRetries?: number };
  decorate?: (headers: Headers) => void;
}

/** Orval custom mutator. Resolves to the parsed body (204 → undefined) and throws ApiError otherwise. Injects
 *  the bearer (none while the port has no token), bounds every attempt with a timeout, retries transient failures
 *  of reads per retryPolicy, and on a 401 for any method forces ONE coalesced refresh — if that actually rotated
 *  the token, the retry budget resets and the call replays (the server rejected it before executing). */
export function createBearerMutator(options: BearerMutatorOptions): ApiMutator {
  const { timeoutMs, retry, decorate } = options;
  const maxRetries = retry === false ? 0 : (retry?.maxRetries ?? MAX_RETRIES);
  const portOf = typeof options.auth === 'function' ? options.auth : () => options.auth as AuthPort;

  return async <T>(url: string, init?: RequestInit): Promise<T> => {
    const auth = portOf();
    const base = typeof options.baseUrl === 'function' ? options.baseUrl() : (options.baseUrl ?? auth.getApiUrl());
    if (!base) throw new ApiError(0, 'API base URL is not configured.');
    const fullUrl = base.replace(/\/$/, '') + url;

    const headers = new Headers(init?.headers);
    if (!headers.has('Accept')) headers.set('Accept', 'application/json');
    // RN on Android runs fetch through OkHttp, which installs a response cache; `RequestInit.cache` is not honoured there.
    if (!headers.has('Cache-Control')) headers.set('Cache-Control', 'no-store');
    if (init?.body && !(typeof FormData !== 'undefined' && init.body instanceof FormData) && !headers.has('Content-Type'))
      headers.set('Content-Type', 'application/json');

    const retriable = isRetriableRequest(init?.method);
    let token = auth.getToken();
    let triedReauth = false;

    for (let attempt = 0; ; attempt++) {
      if (token) headers.set('Authorization', `Bearer ${token}`);
      else headers.delete('Authorization');
      decorate?.(headers);

      let res: Response;
      try {
        res = await fetchWithTimeout(fullUrl, { ...init, headers }, timeoutMs);
      } catch {
        if (retriable && attempt < maxRetries) {
          await delay(retryDelayMs(attempt));
          continue;
        }
        throw new ApiError(0, NETWORK_ERROR);
      }

      if (res.ok) return (res.status === 204 ? undefined : parseBody(await res.text())) as T;

      if (retriable && isTransientStatus(res.status) && attempt < maxRetries) {
        await delay(retryDelayMs(attempt, res.headers.get('Retry-After')));
        continue;
      }

      // One forced re-auth per call: the store decides transient-vs-definitive; a genuinely fresh token
      // (≠ what we sent) earns a full new retry budget.
      if (res.status === 401 && !triedReauth && token) {
        triedReauth = true;
        const fresh = await auth.refresh(true, token);
        if (fresh && fresh !== token) {
          token = fresh;
          attempt = -1;
          continue;
        }
      }

      const body = await res.text().catch(() => '');
      throw ApiError.fromBody(res.status, body, res.statusText || `HTTP ${res.status}`);
    }
  };
}

function parseBody(text: string): unknown {
  if (!text) return undefined;
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

async function fetchWithTimeout(url: string, init: RequestInit, timeoutMs: number): Promise<Response> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  try {
    return await fetch(url, { ...init, signal: controller.signal });
  } finally {
    clearTimeout(timer);
  }
}

const delay = (ms: number) => new Promise<void>((resolve) => setTimeout(resolve, ms));
