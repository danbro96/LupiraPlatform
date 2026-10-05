# Changelog

## 0.1.0
- `EventLogExtensions` on `IQuerySession`: `HeadSequenceAsync` (latest settled global event sequence, 0 when none) and `ChangedStreamsAsync` (`<TAggregate>` or by stream type alias): streams with a settled event after `since`, one query over `mt_events` joined to `mt_streams`, ordered by latest event, `limit + 1` look-ahead, capped at the settled head.
- Settle fence: only events older than `settleLag` by the database clock count (`DefaultSettleLag` 5 s, optional `settleLag` overloads, `TimeSpan.Zero` = none), so a lower sequence committed after a higher one is not skipped.
- `ChangedStreams`: `Ids`, `NextSequence`, `HasMore`.
