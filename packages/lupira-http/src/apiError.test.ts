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
