# Changelog

## 0.2.0

- `replayError`: `ReplayDecision` gains `detail` (the error message, except on 401 or the bare `HTTP <status>` fallback) and `traceId`, set by `classifyReplayError` for HTTP failures.
- `replayError`: `describeFailure(decision)` → `reason: detail`, or `reason` alone.

## 0.1.0

- `lww`: `wins`, `compareInstant` (full ISO precision), `compareCommandId`, `SectionGuard`; parity vectors with the server rule.
- `replayError`: `classifyReplayError`, `classifyReplayStatus`, `ReplayDecision` (401 pauses; 0, 429 and 5xx retry; other 4xx and client errors park).
- `backoff`: `PARK_AFTER_ATTEMPTS`, `nextAttemptDelayMs`.
