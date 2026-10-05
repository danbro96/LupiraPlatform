# Changelog

## 0.2.0

- **Breaking** `mutator`: `createBearerMutator` always resolves to the parsed body (204 → `undefined`); the `envelope` option and `ApiEnvelope` are removed.
- **Breaking** `mutator`: transient failures are retried in-request for GET/HEAD only; writes are never retried, with or without an `Idempotency-Key`.
- `mutator`: a 401 on any method with a bearer forces one refresh and replays when the token rotated.
- **Breaking** `retryPolicy`: `isRetriableRequest(method)` drops the `hasIdempotencyKey` parameter.
- `apiError`: `ApiError` gains `title` and `traceId` (from problem+json) and `ApiError.fromBody(status, body, fallback)`.

## 0.1.0

- `apiError`: `ApiError`, `isNetworkError`, `problemMessage`, `errorText`, `NETWORK_ERROR`.
- `transport`: `ApiTransport`, `setApiTransport`, `apiRequest` (the orval mutator seam).
- `retryPolicy`: `MAX_RETRIES`, `isTransientStatus`, `isRetriableRequest`, `retryDelayMs`.
- `authPort`: `AuthPort`, `setAuthPort`, `authPort`.
- `mutator`: `createBearerMutator({ auth, baseUrl?, timeoutMs, retry?, envelope?, decorate? })`, `ApiEnvelope`, `ApiMutator`.
