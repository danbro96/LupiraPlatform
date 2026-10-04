# Lupira.Postgres.Health

`DatabaseReadyCheck`: `select 1` through the Marten store; Healthy "Postgres reachable.", Unhealthy "Postgres unreachable." with the exception.

```csharp
builder.Services.AddLupiraHealth().AddReadyCheck<DatabaseReadyCheck>("postgres");
```
