using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace Lupira.Testing.Mcp;

/// <summary>Every tool refuses an argument its schema doesn't declare, before it runs (so this has no side effects).
/// The rules themselves are tested in LupiraPlatform (Lupira.Mcp).</summary>
public abstract class McpStrictArgumentsTests : IAsyncLifetime
{
    /// <summary>A client the host authenticates on <c>/mcp</c>; disposed with the MCP session.</summary>
    protected abstract HttpClient CreateAuthenticatedClient();

    /// <summary>A side-effect-free tool called with <see cref="DeclaredToolArguments"/>; null when every tool has side
    /// effects or needs a live upstream.</summary>
    protected abstract string? DeclaredToolName { get; }

    protected virtual IReadOnlyDictionary<string, object?> DeclaredToolArguments => new Dictionary<string, object?>();

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Every_tool_rejects_an_undeclared_argument()
    {
        await using var mcp = await ConnectAsync();
        var tools = await mcp.ListToolsAsync();
        Assert.NotEmpty(tools);
        foreach (var tool in tools)
        {
            var result = await mcp.CallToolAsync(tool.Name, new Dictionary<string, object?> { ["__undeclared"] = 1 });

            Assert.StartsWith($"Invalid arguments for '{tool.Name}': unknown __undeclared", ErrorText(result, tool.Name));
        }
    }

    [Fact]
    public async Task Declared_arguments_reach_the_tool()
    {
        if (DeclaredToolName is not { } name) return;

        await using var mcp = await ConnectAsync();
        var result = await mcp.CallToolAsync(name, DeclaredToolArguments);

        Assert.NotEqual(true, result.IsError);
    }

    protected async Task<McpClient> ConnectAsync()
    {
        var http = CreateAuthenticatedClient();
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(http.BaseAddress!, "/mcp"), TransportMode = HttpTransportMode.StreamableHttp },
            http, ownsHttpClient: true);
        return await McpClient.CreateAsync(transport);
    }

    /// <summary>The single text block of a result that must be an error.</summary>
    protected static string ErrorText(CallToolResult result, string? because = null)
    {
        Assert.True(result.IsError, because);
        return Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
    }
}
