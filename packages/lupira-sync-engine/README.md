# @danbro96/lupira-sync-engine

The offline kernel shared by the Lupira mobile apps. It owns four SQLite tables (`docs`, `outbox`, `cursors`, `meta`) and the push/pull loop; each app supplies one `AggregateModule` per aggregate: its feed, reducer, replay, hold key and index tables.

Every write changes an input (the server doc or the queued ops) and recomputes `local = reduce(server, ...ops)`, then rewrites the module's index rows and reports `{ aggregate, ids, origin }` through `onChange`.

- `engine`: `createSyncEngine(options)` → `sync()`, `push()`, `ready()` (await before reads), `enqueue(ops, { holdMs? })` (held and backed-off ops replay on their own once due), `discard(commandId)`, `retry(commandId)`, `reindex(aggregate)`, `wipe()`, `doc(aggregate, id)`, `docs(aggregate)`, `parked()`, `status`.
- Reads: the `docs` table is a stable read surface. `local` holds the current `{ doc, guards }` JSON (null = deleted locally). A module's index tables hold only the columns its queries filter or sort on; queries join `docs ON docs.aggregate = '<aggregate>' AND docs.id = <index>.id AND docs.local IS NOT NULL` for the doc itself.
- `types`: the module contract. `bannerState`: the sync banner. `status`: the status snapshot shape.
- `expo/triggers`: `defineSyncTask(name, engine, prepare?)` at module scope (`prepare` loads the auth session in the headless context), `startSyncTriggers(engine, { backgroundTaskName, registerBackgroundTask: !__DEV__ })` once the app mounts. Needs `react-native`, `@react-native-community/netinfo`, `expo-background-task`, `expo-task-manager` and `@danbro96/lupira-http`.

The kernel owns `PRAGMA user_version`; module tables come from `IndexSpec.ddl` and are rebuilt when `version` changes.

```ts
export const engine = createSyncEngine({
  openDb: expoDb('cal.db'),
  modules: [itemModule, contactModule, calendarModule],
  cacheVersion: 1,
  onChange: invalidateOnChange(queryClient, { contact: ['occurrences'] }),
});
defineSyncTask('lupira-cal-sync', engine, () => useAuth.getState().load());

await engine.enqueue({ commandId, occurredAt, aggregate: 'cal.item', aggregateId: id, kind: 'item.revise', core });
```
