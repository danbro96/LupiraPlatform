# Changelog

## 0.1.0
- `AddLupiraHealth()`: `self` liveness check; `AddReadyCheck<T>(name, timeout?)` adds a ready-tagged check (3 s default timeout).
- `MapLupiraHealth()`: anonymous `/livez` and `/readyz`, excluded from HTTP metrics; per-check JSON report outside Production.
- `MapLupiraPing(policies)`: authenticated `/pingz` echoing subject, audiences and email as `PingDto`.
