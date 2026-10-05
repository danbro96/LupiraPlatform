import { describe, expect, it } from 'vitest';
import { onlineQuery } from './onlineQuery.ts';

describe('onlineQuery', () => {
  it('pauses offline, stays fresh for five minutes and retries once', () => {
    const options = onlineQuery(['photos', 'list'], async () => [1]);
    expect(options).toMatchObject({ queryKey: ['photos', 'list'], staleTime: 5 * 60_000, networkMode: 'online', retry: 1 });
  });
});
