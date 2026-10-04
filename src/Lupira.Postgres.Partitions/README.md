# Lupira.Postgres.Partitions

Time-range partitions created on demand for every period an ingest batch touches (no DEFAULT catch-all), pre-created ahead of use, and dropped once entirely older than a retention cutoff.

```csharp
builder.Services.AddSingleton(new PartitionManager("telemetry"));
await partitions.EnsureAsync(conn, tx, "location_point", PartitionInterval.Weekly, ts, ct);
```
