import type { QueryKey } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';

vi.mock('expo-sqlite/kv-store', () => ({
  default: { getItem: async () => null, setItem: async () => undefined, removeItem: async () => undefined },
}));

import { createAppQueryClient } from './queryClient.ts';

describe('createAppQueryClient', () => {
  const { queryClient, persistOptions } = createAppQueryClient({ persistRoots: ['places', 'photos'], buster: '1.2.3' });
  const persisted = (key: QueryKey) => {
    const query = queryClient.getQueryCache().find({ queryKey: key, exact: true })!;
    return persistOptions.dehydrateOptions!.shouldDehydrateQuery!(query);
  };

  it('persists only successful queries under an allowlisted root', () => {
    queryClient.setQueryData(['places', 'near'], [1]);
    queryClient.setQueryData(['photos', 'list', 2], [2]);
    queryClient.setQueryData(['search', 'x'], [3]);
    queryClient.getQueryCache().build(queryClient, { queryKey: ['places', 'pending'] });

    expect(persisted(['places', 'near'])).toBe(true);
    expect(persisted(['photos', 'list', 2])).toBe(true);
    expect(persisted(['search', 'x'])).toBe(false);
    expect(persisted(['places', 'pending'])).toBe(false);
  });

  it('keeps queries in memory at least as long as the persisted copy lives', () => {
    expect(persistOptions).toMatchObject({ maxAge: 7 * 24 * 60 * 60_000, buster: '1.2.3' });
    expect(queryClient.getDefaultOptions().queries?.gcTime).toBe(persistOptions.maxAge);
    const short = createAppQueryClient({ persistRoots: [], buster: '1', maxAgeMs: 60_000 });
    expect(short.queryClient.getDefaultOptions().queries?.gcTime).toBe(60_000);
    expect(short.persistOptions.maxAge).toBe(60_000);
  });
});
