# Lupira.Marten.Idempotency

`Idempotency-Key` dedup for Marten event appends. The `ProcessedCommand` row and the events commit in one `SaveChangesAsync`; the row is a plain INSERT, so a concurrent duplicate key rolls back the loser's events and the loser returns the committed state.

```csharp
opts.Schema.For<ProcessedCommand>().Identity(x => x.CommandId);
builder.Services.AddScoped<Idempotency>();
```

| Member | Use |
|---|---|
| `SeenAsync(commandId, ct)` | Ledger row for the key, or null (also null without a key). Replay the existing aggregate on a hit. |
| `CommitAsync(commandId, aggregateId, resultVersion, ct)` | Stage the row and save. False = dedup race lost. |
| `AppendDedupAsync(commandId, aggregateId, events, ct)` | Append to the stream at its current head, stage the row, save. Resulting version, or null = race lost. |
| `Record(commandId, aggregateId, resultVersion)` | Stage the row only; the caller saves. |
| `IsDuplicate(ex)` | The save failure is the dedup race being lost. |

No key = no row; the save commits without cross-request dedup.
