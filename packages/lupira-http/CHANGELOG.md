# Changelog

## 0.1.0

- `apiError`: `ApiError`, `isNetworkError`, `problemMessage`, `errorText`, `NETWORK_ERROR`.
- `transport`: `ApiTransport`, `setApiTransport`, `apiRequest` (the orval mutator seam).
- `retryPolicy`: `MAX_RETRIES`, `isTransientStatus`, `isRetriableRequest`, `retryDelayMs`.
- `authPort`: `AuthPort`, `setAuthPort`, `authPort`.
- `mutator`: `createBearerMutator({ auth, baseUrl?, timeoutMs, retry?, envelope?, decorate? })`, `ApiEnvelope`, `ApiMutator`.
