using Xunit;

namespace Lupira.Testing.Mcp.UnitTests;

public sealed class StrictArgumentsConformanceTests(ConformanceHost host) : McpStrictArgumentsTests, IClassFixture<ConformanceHost>
{
    protected override string DeclaredToolName => "echo";

    protected override IReadOnlyDictionary<string, object?> DeclaredToolArguments =>
        new Dictionary<string, object?> { ["text"] = "hi" };

    protected override HttpClient CreateAuthenticatedClient()
    {
        var client = host.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "alice@x.test");
        return client;
    }

    [Fact]
    public async Task Error_text_names_the_missing_argument()
    {
        await using var mcp = await ConnectAsync();
        var result = await mcp.CallToolAsync("echo", new Dictionary<string, object?>());

        Assert.Equal("Invalid arguments for 'echo': missing required text. Accepts: text (required).", ErrorText(result));
    }
}
