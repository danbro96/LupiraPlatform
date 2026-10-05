import type { Tx } from '@danbro96/lupira-expo-sqlite/types';

export interface OpBase {
  commandId: string;
  occurredAt: string;
  aggregate: string;
  aggregateId: string;
  kind: string;
}

export interface DocState<Doc = unknown, Guards = unknown> {
  doc: Doc;
  guards: Guards;
}

/** One page of a server sync feed (`SyncPage<T>` on the wire). `reset` = a full sync starts at this page. */
export interface FeedPage<Change> {
  cursor: string;
  hasMore: boolean;
  reset: boolean;
  changed: Change[];
  deleted: string[];
}

export interface Feed<Change = unknown, Doc = unknown, Guards = unknown> {
  fetch(since: string | null): Promise<FeedPage<Change>>;
  fromWire(change: Change): { id: string; state: DocState<Doc, Guards> };
}

/** Module-owned query tables derived from each doc's local state. Bump `version` to rebuild them. */
export interface IndexSpec<Doc = unknown, Guards = unknown> {
  version: number;
  tables: string[];
  ddl: string;
  write(tx: Tx, id: string, state: DocState<Doc, Guards> | null): Promise<void>;
}

/** One aggregate type. Read-only aggregates leave out `reduce` and `replay`. */
export interface AggregateModule<Doc = unknown, Guards = unknown, Op extends OpBase = OpBase, Change = unknown> {
  aggregate: string;
  feed: Feed<Change, Doc, Guards>;
  reduce?(state: DocState<Doc, Guards> | null, op: Op): DocState<Doc, Guards> | null;
  replay?(op: Op): Promise<void>;
  /** Ops sharing a hold key replay strictly in order; defaults to the aggregate id. */
  holdKeyOf?(op: Op): string;
  index?: IndexSpec<Doc, Guards>;
}

export type ChangeOrigin = 'local' | 'pull';

export interface ChangeEvent {
  aggregate: string;
  ids: string[];
  origin: ChangeOrigin;
}

export interface ParkedOp {
  op: OpBase;
  attempts: number;
  lastStatus: number | null;
  lastError: string | null;
}
