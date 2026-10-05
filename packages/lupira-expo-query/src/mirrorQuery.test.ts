import { onlineManager, QueryClient } from '@tanstack/react-query';
import { afterEach, describe, expect, it } from 'vitest';
import { mirrorQuery } from './mirrorQuery.ts';

afterEach(() => onlineManager.setOnline(true));

describe('mirrorQuery', () => {
  it('never goes stale, never retries and runs regardless of the network', () => {
    expect(mirrorQuery(['cal.item', 'a'], async () => 1)).toMatchObject({
      queryKey: ['cal.item', 'a'], staleTime: Infinity, networkMode: 'always', retry: false,
    });
  });

  it('reads while offline', async () => {
    onlineManager.setOnline(false);
    expect(await new QueryClient().fetchQuery(mirrorQuery(['cal.item'], async () => ['row']))).toEqual(['row']);
  });
});
