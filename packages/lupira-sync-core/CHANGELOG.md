# Changelog

## 0.1.0

- `lww`: `wins`, `compareInstant` (full ISO precision), `compareCommandId`, `SectionGuard`; parity vectors with the server rule.
- `replayError`: `classifyReplayError`, `classifyReplayStatus`, `ReplayDecision` (401 pauses; 0, 429 and 5xx retry; other 4xx and client errors park).
- `backoff`: `PARK_AFTER_ATTEMPTS`, `nextAttemptDelayMs`.
