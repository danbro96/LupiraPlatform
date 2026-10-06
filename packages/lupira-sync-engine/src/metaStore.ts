import type { Tx } from '@danbro96/lupira-expo-sqlite/types';

export const CACHE_VERSION_KEY = 'cacheVersion';

export const indexVersionKey = (aggregate: string): string => `index.${aggregate}.version`;

export async function getMeta(tx: Tx, key: string): Promise<string | null> {
  return (await tx.first<{ value: string }>('SELECT value FROM meta WHERE key = ?', [key]))?.value ?? null;
}

export async function setMeta(tx: Tx, key: string, value: string): Promise<void> {
  await tx.run('INSERT INTO meta (key, value) VALUES (?, ?) ON CONFLICT (key) DO UPDATE SET value = excluded.value', [key, value]);
}

export async function deleteMeta(tx: Tx, key: string): Promise<void> {
  await tx.run('DELETE FROM meta WHERE key = ?', [key]);
}
