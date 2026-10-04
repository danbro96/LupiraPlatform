# Changelog

## 0.1.0
- `AddLupiraMcp()` (Streamable HTTP + strict tool-argument filter, returns the builder) and `MapLupiraMcp(path, policy)`.
- `StrictToolArguments`: rejects tool calls whose argument names don't match the input schema, naming what each level accepts.
- `OpResult.Require()` / `OpResult<T>.Require()`: value or `McpException`.
- `McpResourceMetadata`: RFC 9728 protected-resource metadata endpoints and `ResourceMetadataUrl`.
