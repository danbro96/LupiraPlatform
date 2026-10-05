import { describe, expect, it } from 'vitest';
import { MAX_RETRIES, isRetriableRequest, isTransientStatus, retryDelayMs } from './retryPolicy.ts';

describe('isTransientStatus', () => {
  it('treats transport failure, throttling, and server errors as transient', () => {
    expect(isTransientStatus(0)).toBe(true);
    expect(isTransientStatus(429)).toBe(true);
    expect(isTransientStatus(500)).toBe(true);
    expect(isTransientStatus(503)).toBe(true);
  });

  it('treats other 4xx and 2xx/3xx as terminal', () => {
    for (const s of [400, 401, 403, 404, 409, 422, 200, 204, 301]) expect(isTransientStatus(s)).toBe(false);
  });
});

describe('isRetriableRequest', () => {
  it('replays reads', () => {
    expect(isRetriableRequest('GET')).toBe(true);
    expect(isRetriableRequest('head')).toBe(true);
    expect(isRetriableRequest(undefined)).toBe(true); // fetch defaults to GET
  });

  it('never replays writes', () => {
    for (const m of ['POST', 'PUT', 'PATCH', 'DELETE']) expect(isRetriableRequest(m)).toBe(false);
  });
});

describe('retryDelayMs', () => {
  it('honors a numeric Retry-After in seconds, capped', () => {
    expect(retryDelayMs(0, '2', () => 0)).toBe(2000);
    expect(retryDelayMs(0, '0', () => 0)).toBe(0);
    expect(retryDelayMs(0, '9999', () => 0)).toBe(10_000);
  });

  it('ignores a malformed Retry-After and backs off exponentially with jitter', () => {
    expect(retryDelayMs(0, 'soon', () => 0)).toBe(300);
    expect(retryDelayMs(0, 'Wed, 21 Oct 2099 07:28:00 GMT', () => 0)).toBe(300);
    expect(retryDelayMs(1, null, () => 0)).toBe(600);
    expect(retryDelayMs(2, null, () => 1)).toBe(1500);
  });

  it('adds bounded whole-millisecond jitter on top of the base delay', () => {
    expect(retryDelayMs(0, null, () => 0.5)).toBe(450);
    expect(retryDelayMs(1, undefined, () => 0.999)).toBe(899);
  });

  it('caps the backoff and keeps a small retry budget', () => {
    expect(retryDelayMs(10, null, () => 0)).toBe(10_000);
    expect(MAX_RETRIES).toBe(2);
  });
});
