# Changelog

## 0.1.0
- `McpStrictArgumentsTests`: `Every_tool_rejects_an_undeclared_argument` and `Declared_arguments_reach_the_tool` (`DeclaredToolName` / `DeclaredToolArguments`; null name skips the call) over Streamable HTTP; `ConnectAsync()` and `ErrorText(result)` for app-specific cases.
- `McpResourceMetadataTests`: metadata is anonymous and names `Issuer`, `/mcp` 401 advertises `resource_metadata`, tunnelled (`CF-Ray`) requests on `TunnelledPaths` get 404, REST 401 on `RestProbePath` doesn't advertise the metadata; `McpProbeMethod` for hosts whose `/mcp` GET isn't challenged.
- Both implement `IAsyncLifetime` with virtual no-op `InitializeAsync`/`DisposeAsync` for a per-test reset.
