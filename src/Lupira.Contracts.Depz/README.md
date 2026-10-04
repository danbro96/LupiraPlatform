# Lupira.Contracts.Depz

Wire contract of the `/depz` dependency report: `DepzReportDto`, `DependencyDto`, `DependencyStatus`. BCL only.

`DependencyStatus` always serializes by name — the collector parses it case-insensitively and maps unknown names to `Unknown`.
