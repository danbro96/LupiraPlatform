# Lupira.Sync.Marten

Change detection for `/sync/*` feeds from Marten's event history: a record changed since sequence N if its stream has an event after N. No document carries a change stamp, so every append mode works.

```csharp
var head = await session.HeadSequenceAsync(ct);
var changes = await session.ChangedStreamsAsync<CalendarItem>(since, limit, ct);
// changes.Ids, changes.NextSequence, changes.HasMore
```

| Member | Use |
|---|---|
| `HeadSequenceAsync([settleLag], ct)` | Latest settled global event sequence, 0 when none. A full sync's final cursor. |
| `ChangedStreamsAsync<TAggregate>(since, limit, [settleLag], ct)` | Up to `limit` streams of `TAggregate` with a settled event after `since`, each once, ordered by their latest event. |
| `ChangedStreamsAsync(since, streamType, limit, [settleLag], ct)` | Same, by Marten's stream type alias (`mt_streams.type`). |

`NextSequence` = latest event of the last returned stream (`since` when none), the next delta's `since`; never past the settled head. `HasMore` = more streams changed after it.

**Settle fence.** Sequences are taken at insert but become visible at commit, so a lower sequence can commit after a higher one. Only events with `mt_events.timestamp <= now() - settleLag` count (`DefaultSettleLag` = 5 s; `TimeSpan.Zero` = no fence), compared on the database clock. A late commit is caught as long as its writer commits within the lag of the event's timestamp:

- Quick mode, new stream (`StartStream`): Marten stamps the app clock at `SaveChanges`, in the same batch as the commit. App clock behind the database shortens the effective lag by the skew.
- Quick mode, existing stream (`mt_quick_append_events`): the database stamps `now() at time zone 'utc'`, the transaction start, written into a `timestamptz` in the session time zone. The database session time zone must be UTC, otherwise these stamps land hours in the past and the fence does not hold for them.
- A transaction held open around `SaveChanges` (`SessionOptions.ForTransaction`, an explicit `BeginTransactionAsync`) must still commit within the lag of its stamps.

Constraints:

- Guid stream identity.
- Streams must be started with `StartStream<TAggregate>`: that call records the stream type; a stream begun by a bare `Append` has none and is never returned.
- Reads `mt_events` / `mt_streams` in the store's event schema.
- Full-sync paging: page snapshots by id in SQL (`ORDER BY id`, `id > after`); never sort or compare Guids in .NET, whose order differs from Postgres `uuid`.

Cursor, feed query and page contract: `Lupira.Sync`.
