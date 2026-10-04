# Lupira.Testing.Mcp

Shared xunit cases for a Lupira MCP API. Subclass in the IntegrationTests project; add app-specific `[Fact]`s alongside.

```csharp
[Collection("integration")]
public sealed class McpToolArgumentsTests(TasksApiTestFactory factory) : McpStrictArgumentsTests
{
    protected override HttpClient CreateAuthenticatedClient() => factory.ApiClient("alice@x.test");
    protected override string DeclaredToolName => "list_my_lists";
    public override Task InitializeAsync() => factory.ResetAsync();
}

[Collection("integration")]
public sealed class McpAuthDiscoveryTests(TasksApiTestFactory factory) : McpResourceMetadataTests
{
    protected override HttpClient CreateAnonymousClient() => factory.AnonymousClient();
    protected override string Issuer => factory.Authority!;
    protected override string RestProbePath => "/lists";
    public override Task InitializeAsync() => factory.ResetAsync();
}
```

Overridable: `DeclaredToolArguments`, `McpProbeMethod` (e.g. `HttpMethod.Post`), `TunnelledPaths` (drop `/mcp` where it isn't LAN-only).
