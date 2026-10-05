import { describe, expect, it } from 'vitest';
import { ApiError, errorText, isNetworkError, problemMessage } from './apiError.ts';

describe('problemMessage', () => {
  it('prefers a problem document’s detail, then its title', () => {
    expect(problemMessage('{"title":"Bad request","detail":"End is before start"}', 'x')).toBe('End is before start');
    expect(problemMessage('{"title":"Conflict"}', 'x')).toBe('Conflict');
  });

  it('keeps a plain body, and falls back when there is none', () => {
    expect(problemMessage('upstream timeout', 'x')).toBe('upstream timeout');
    expect(problemMessage('', 'HTTP 502')).toBe('HTTP 502');
  });
});

describe('ApiError.fromBody', () => {
  it('parses title and traceId from a problem document', () => {
    const e = ApiError.fromBody(422, '{"title":"Validation failed","detail":"Name is required","traceId":"00-abc-01"}', 'x');
    expect(e).toMatchObject({ status: 422, message: 'Name is required', title: 'Validation failed', traceId: '00-abc-01' });
  });

  it('leaves title and traceId unset for a non-problem body', () => {
    for (const body of ['upstream timeout', '', '"just a string"', '{"title":42}']) {
      const e = ApiError.fromBody(502, body, 'HTTP 502');
      expect(e.title).toBeUndefined();
      expect(e.traceId).toBeUndefined();
    }
    expect(ApiError.fromBody(502, '', 'HTTP 502').message).toBe('HTTP 502');
  });
});

describe('errorText', () => {
  it('shows an error’s message without its class name', () => {
    expect(errorText(new ApiError(400, 'End is before start'))).toBe('End is before start');
    expect(errorText('boom')).toBe('boom');
  });
});

describe('isNetworkError', () => {
  it('is status 0 only', () => {
    expect(isNetworkError(new ApiError(0, 'x'))).toBe(true);
    expect(isNetworkError(new ApiError(503, 'x'))).toBe(false);
    expect(isNetworkError(new Error('x'))).toBe(false);
  });
});
