# @danbro96/lupira-http

Fetch-only HTTP plumbing shared by the Lupira web and mobile clients. No dependencies.
Generated clients call `apiRequest` from `@danbro96/lupira-http/transport`; each app installs its transport once with `setApiTransport`.
Mobile: `setApiTransport(createBearerMutator({ auth: authPort, timeoutMs: 10_000 }))`, with the auth store calling `setAuthPort` at module load.
`createBearerMutator` resolves to the parsed body and throws `ApiError` (`status`, `title`, `traceId`) otherwise. Transient failures are retried for GET/HEAD only; writes are left to the caller's outbox. A 401 forces one token refresh and replays the request for any method.
