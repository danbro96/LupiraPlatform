# Lupira.Hosting.Health

`/livez`, `/readyz` (ready-tagged checks) and `/pingz` (auth-seam claims echo). Detailed JSON outside Production.

```csharp
builder.Services.AddLupiraHealth().AddReadyCheck<DatabaseReadyCheck>("postgres");
app.MapLupiraHealth();
app.MapLupiraPing("ApiPolicy");
```
