# @danbro96/lupira-http

Fetch-only HTTP plumbing shared by the Lupira web and mobile clients. No dependencies.
Generated clients call `apiRequest` from `@danbro96/lupira-http/transport`; each app installs its transport once with `setApiTransport`.
Mobile: `setApiTransport(createBearerMutator({ auth: authPort, timeoutMs: 10_000 }))`, with the auth store calling `setAuthPort` at module load.
