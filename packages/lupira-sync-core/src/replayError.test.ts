import { describe, expect, it } from 'vitest';
import { classifyReplayError, classifyReplayStatus, describeFailure } from './replayError.ts';

class HttpError extends Error {
  readonly status: number;
  readonly traceId?: string;

  constructor(status: number, message = `HTTP ${status}`, traceId?: string) {
    super(message);
    this.status = status;
    this.traceId = traceId;
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

  it('carries the server detail and trace id of an HTTP failure', () => {
    expect(classifyReplayError(new HttpError(400, 'Title is required', '00-abc-01'))).toEqual({
      outcome: 'park',
      stop: false,
      reason: 'rejected (400)',
      detail: 'Title is required',
      traceId: '00-abc-01',
    });
    expect(classifyReplayError(new HttpError(409, ''))).not.toHaveProperty('detail');
    expect(classifyReplayError(new HttpError(409))).not.toHaveProperty('detail');
    expect(classifyReplayError(new HttpError(401, 'Unauthorized', '00-abc-01'))).toEqual({
      outcome: 'pause',
      stop: true,
      reason: 'signed out',
      traceId: '00-abc-01',
    });
  });

  it('adds no detail to a client error', () => {
    expect(classifyReplayError(new TypeError('boom'))).not.toHaveProperty('detail');
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

describe('describeFailure', () => {
  it('appends the detail to the reason', () => {
    expect(describeFailure({ outcome: 'park', stop: false, reason: 'rejected (400)', detail: 'Title is required' })).toBe('rejected (400): Title is required');
  });

  it('falls back to the reason alone', () => {
    expect(describeFailure(classifyReplayStatus(401))).toBe('signed out');
  });
});
