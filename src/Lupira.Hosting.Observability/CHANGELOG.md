# Changelog

## 0.1.1
- Build-time OpenAPI document generation (`GetDocument.Insider`) registers no telemetry and never fails on a missing endpoint.

## 0.1.0
- `AddLupiraTelemetry(serviceName, configure?)`: OTLP traces, metrics and logs; outside Development a missing `OTEL_EXPORTER_OTLP_ENDPOINT` fails startup. Development without an endpoint exports nothing, or to the console when opted in.
- `Lupira.*` and `<ApplicationName>.*` meters and activity sources always registered; extras via `LupiraTelemetryOptions.Meters` / `Sources`.
- Server spans dropped for `/livez`, `/readyz`, `/pingz`, `/depz` (+ `FilteredPaths`); exceptions recorded on spans.
