# Lupira.Hosting.Observability

OpenTelemetry → OTLP (traces, metrics, logs), a no-op unless `OTEL_EXPORTER_OTLP_ENDPOINT` is set. Probe spans are dropped; `Lupira.*` and `<ApplicationName>.*` meters/sources need no registration.

```csharp
builder.AddLupiraTelemetry("lupira-cal-api", o => o.Sources.Add("Marten"));
```
