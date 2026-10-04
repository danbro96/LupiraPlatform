using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Lupira.Hosting.LanEdge.UnitTests;

public sealed class LanOnlyExposureTests
{
    private static async Task<WebApplication> StartAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.UseLanOnlySurfaces("/mcp", "/internal");
        app.MapGet("/mcp", () => TypedResults.Ok());
        app.MapGet("/internal/counts", () => TypedResults.Ok());
        app.MapGet("/mcpx", () => TypedResults.Ok());
        app.MapGet("/items", () => TypedResults.Ok());
        await app.StartAsync();
        return app;
    }

    private static async Task<HttpStatusCode> GetAsync(WebApplication app, string path, string? cloudflareHeader)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (cloudflareHeader is not null) request.Headers.Add(cloudflareHeader, "x");
        return (await app.GetTestClient().SendAsync(request)).StatusCode;
    }

    [Theory]
    [InlineData("/mcp", "CF-Ray")]
    [InlineData("/internal/counts", "CF-Connecting-IP")]
    public async Task A_tunnelled_hit_on_a_lan_only_prefix_is_404(string path, string header)
    {
        await using var app = await StartAsync();

        Assert.Equal(HttpStatusCode.NotFound, await GetAsync(app, path, header));
    }

    [Theory]
    [InlineData("/mcp", null)]
    [InlineData("/internal/counts", null)]
    [InlineData("/mcpx", "CF-Ray")]
    [InlineData("/items", "CF-Ray")]
    public async Task Direct_hits_and_other_paths_pass(string path, string? header)
    {
        await using var app = await StartAsync();

        Assert.Equal(HttpStatusCode.OK, await GetAsync(app, path, header));
    }
}
