# Changelog

## 0.3.0

- **Breaking** `authPort`: `AuthPort` gains `onSignOut(cb)`.
- `mutator`: `createBearerMutator` takes the bearer from `refresh(false)`, so a token near expiry is refreshed before the request.

## 0.2.1

- `apiError`: `ApiError` gains `type` (the problem+json `type` URI), set by `ApiError.fromBody`, so a caller can branch on a typed failure.

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
