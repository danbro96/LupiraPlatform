import { describe, expect, it } from 'vitest';
import { classifyReplayError, classifyReplayStatus } from './replayError.ts';

class HttpError extends Error {
  readonly status: number;

  constructor(status: number) {
    super(`HTTP ${status}`);
    this.status = status;
  }
}

describe('classifyReplayError', () => {
  it('pauses the drain on 401 (session gone — the mutator already spent its re-auth)', () => {
    expect(classifyReplayError(new HttpError(401))).toMatchObject({ outcome: 'pause', stop: true });
  });

  it('retries transient failures including 429', () => {
    for (const status of [0, 429, 500, 503])
      expect(classifyReplayError(new HttpError(status))).toMatchObject({ outcome: 'retry', stop: true });
  });

  it('parks semantic conflicts and lets the queue continue', () => {
    for (const status of [400, 403, 404, 409, 422])
      expect(classifyReplayError(new HttpError(status))).toMatchObject({ outcome: 'park', stop: false });
  });

  it('parks client bugs (they fail identically forever)', () => {
    expect(classifyReplayError(new TypeError('boom'))).toMatchObject({ outcome: 'park', stop: false });
    expect(classifyReplayError('weird string throw')).toMatchObject({ outcome: 'park', stop: false });
  });

  it('only reads status from an Error', () => {
    expect(classifyReplayError({ status: 503 })).toMatchObject({ outcome: 'park', stop: false });
  });
});

describe('classifyReplayStatus', () => {
  it('names the status in the reason', () => {
    expect(classifyReplayStatus(401).reason).toBe('signed out');
    expect(classifyReplayStatus(503).reason).toBe('transient (503)');
    expect(classifyReplayStatus(409).reason).toBe('rejected (409)');
  });
});
