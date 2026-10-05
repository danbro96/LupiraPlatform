import { describe, expect, it } from 'vitest';
import { onlineQuery, retryTransient } from './onlineQuery.ts';

describe('onlineQuery', () => {
  it('pauses offline and stays fresh for five minutes', () => {
    const options = onlineQuery(['photos', 'list'], async () => [1]);
    expect(options).toMatchObject({ queryKey: ['photos', 'list'], staleTime: 5 * 60_000, networkMode: 'online', retry: retryTransient });
  });

  it('retries a transient failure once and never a client error', () => {
    expect(retryTransient(0, { status: 503 })).toBe(true);
    expect(retryTransient(0, new Error('network'))).toBe(true);
    expect(retryTransient(1, { status: 503 })).toBe(false);
    expect(retryTransient(0, { status: 404 })).toBe(false);
  });
});
