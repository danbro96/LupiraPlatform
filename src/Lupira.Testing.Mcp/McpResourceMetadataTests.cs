using System.Net;
using System.Text.Json;
using Xunit;

namespace Lupira.Testing.Mcp;

/// <summary>MCP auth discovery (RFC 9728): anonymous metadata names the issuer, and a 401 on /mcp points at it.</summary>
public abstract class McpResourceMetadataTests : IAsyncLifetime
{
    private const string MetadataPath = "/.well-known/oauth-protected-resource";

    protected abstract HttpClient CreateAnonymousClient();

    /// <summary>The authority the host was given, e.g. <c>https://auth.test/application/o/lupira-cal/</c>.</summary>
    protected abstract string Issuer { get; }

    /// <summary>An authenticated REST route; its 401 must not advertise the MCP metadata.</summary>
    protected virtual string RestProbePath => "/me";

    protected virtual HttpMethod McpProbeMethod => HttpMethod.Get;

    /// <summary>Paths a Cloudflare-tunnelled request (<c>CF-Ray</c>) must get 404 on.</summary>
    protected virtual IReadOnlyList<string> TunnelledPaths => [MetadataPath, MetadataPath + "/mcp", "/mcp"];

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(MetadataPath)]
    [InlineData(MetadataPath + "/mcp")]
    public async Task Metadata_is_anonymous_and_names_the_issuer(string path)
    {
        var anon = CreateAnonymousClient();
        var resp = await anon.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(new Uri(anon.BaseAddress!, "/mcp").ToString(), doc.RootElement.GetProperty("resource").GetString());
        var servers = doc.RootElement.GetProperty("authorization_servers").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Equal([Issuer], servers);
        Assert.Contains("offline_access",
            doc.RootElement.GetProperty("scopes_supported").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task Unauthenticated_mcp_401_advertises_the_resource_metadata()
    {
        var anon = CreateAnonymousClient();
        using var req = new HttpRequestMessage(McpProbeMethod, "/mcp");
        var resp = await anon.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);

        var challenge = Assert.Single(resp.Headers.WwwAuthenticate).ToString();
        Assert.Contains($"resource_metadata=\"{new Uri(anon.BaseAddress!, MetadataPath + "/mcp")}\"", challenge);
    }

    [Fact]
    public async Task Tunnelled_requests_get_404()
    {
        var anon = CreateAnonymousClient();
        foreach (var path in TunnelledPaths)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, path);
            req.Headers.Add("CF-Ray", "test-ray");
            var resp = await anon.SendAsync(req);
            Assert.True(resp.StatusCode == HttpStatusCode.NotFound, $"{path}: {resp.StatusCode}");
        }
    }

    [Fact]
    public async Task Rest_401_does_not_advertise_mcp_metadata()
    {
        var anon = CreateAnonymousClient();
        var resp = await anon.GetAsync(RestProbePath);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        Assert.DoesNotContain(resp.Headers.WwwAuthenticate.Select(h => h.ToString()),
            c => c.Contains("resource_metadata"));
    }
}
