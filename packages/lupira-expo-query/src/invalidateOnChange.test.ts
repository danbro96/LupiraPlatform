import { QueryClient } from '@tanstack/react-query';
import { describe, expect, it } from 'vitest';
import { invalidateOnChange } from './invalidateOnChange.ts';

describe('invalidateOnChange', () => {
  const setup = () => {
    const queryClient = new QueryClient();
    for (const key of [['contact', 'a'], ['occurrences', '2026-08'], ['cal.item', 'x'], ['photos']]) queryClient.setQueryData(key, 1);
    const stale = () => queryClient.getQueryCache().getAll().filter((q) => q.state.isInvalidated).map((q) => q.queryKey[0]);
    return { queryClient, stale };
  };

  it('invalidates the changed aggregate\'s root', () => {
    const { queryClient, stale } = setup();
    invalidateOnChange(queryClient)({ aggregate: 'cal.item' });
    expect(stale()).toEqual(['cal.item']);
  });

  it('also invalidates the roots derived from it', () => {
    const { queryClient, stale } = setup();
    invalidateOnChange(queryClient, { contact: ['occurrences'] })({ aggregate: 'contact' });
    expect(stale().sort()).toEqual(['contact', 'occurrences']);
  });
});
