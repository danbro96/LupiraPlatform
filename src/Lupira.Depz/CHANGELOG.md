# Changelog

## 0.1.1
- `/depz` no longer claims an endpoint name, so a host may name its own operations freely (e.g. `GetDependencies`).

## 0.1.0
- `AddLupiraDepz(Action<DepzOptions>)`: background poller (runs only with a probe key), report cache, `depz-probe` HTTP client, `<prefix>.dependency.probe.duration` / `<prefix>.dependency.up` metrics on a configurable meter.
- `MapDepz()`: anonymous `/depz` gated by `X-Probe-Key` (401 on mismatch or blank key).
- `IProbeCredential` with `ClientCredentialsProbeCredential` (client-credentials bearer, cached; `X-Dev-User` fallback) and `StaticHeaderProbeCredential` (Bearer / X-API-Key / Basic / any header).
- Target sources: `AddLupiraDepzTargets` (static list), `AddLupiraDepzConfigurationTargets` (reads each edge from the client's own config section).
