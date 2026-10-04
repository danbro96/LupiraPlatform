import { describe, expect, it } from 'vitest';
import { apiRequest, setApiTransport } from './transport.ts';

describe('apiRequest', () => {
  it('routes through the installed transport', async () => {
    setApiTransport(async <T,>(url: string, init?: RequestInit) => ({ url, method: init?.method }) as T);
    await expect(apiRequest('/cal/items', { method: 'POST' })).resolves.toEqual({ url: '/cal/items', method: 'POST' });
  });
});
