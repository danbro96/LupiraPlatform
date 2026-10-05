import type { Db, Tx } from '@danbro96/lupira-expo-sqlite/types';
import { counts } from './outboxStore.ts';
import type { StatusWriter } from './status.ts';
import type { AggregateModule, ChangeEvent, ChangeOrigin } from './types.ts';

/** What every engine path runs against once the database is open and migrated. */
export interface Kernel {
  db: Db;
  modules: AggregateModule[];
  now: () => number;
  onChange: (event: ChangeEvent) => void;
  status: StatusWriter;
}

export class ChangeSet {
  private readonly ids = new Map<string, Set<string>>();

  add(aggregate: string, id: string): void {
    const set = this.ids.get(aggregate) ?? new Set<string>();
    set.add(id);
    this.ids.set(aggregate, set);
  }

  events(origin: ChangeOrigin): ChangeEvent[] {
    return [...this.ids].map(([aggregate, ids]) => ({ aggregate, ids: [...ids], origin }));
  }
}

export function moduleFor(kernel: Kernel, aggregate: string): AggregateModule {
  const module = kernel.modules.find((m) => m.aggregate === aggregate);
  if (!module) throw new Error(`no sync module for aggregate '${aggregate}'`);
  return module;
}

/** One exclusive transaction; its change events go out only after it commits. */
export async function commit<T>(kernel: Kernel, origin: ChangeOrigin, fn: (tx: Tx, changes: ChangeSet) => Promise<T>): Promise<T> {
  const changes = new ChangeSet();
  const result = await kernel.db.exclusive((tx) => fn(tx, changes));
  for (const event of changes.events(origin)) kernel.onChange(event);
  return result;
}

export async function refreshCounts(kernel: Kernel): Promise<void> {
  kernel.status.set(await counts(kernel.db));
}
