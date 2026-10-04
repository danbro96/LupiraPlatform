# Lupira.Depz

Non-gating `/depz` dependency report: background prober, cache, `X-Probe-Key` gate, metrics.

```csharp
builder.Services.AddLupiraDepz(o => { builder.Configuration.GetSection(DepzOptions.SectionName).Bind(o); o.ServiceName = "lupira-cal-api"; o.MetricPrefix = "cal"; });
builder.Services.AddLupiraDepzConfigurationTargets(new ConfiguredTarget { Name = "lupira-geo-api", Section = "Geo", ProbePath = "pingz" });
app.MapDepz();
```
