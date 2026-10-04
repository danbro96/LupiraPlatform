using System.Net;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Lupira.Hosting.OpenApi.UnitTests;

public sealed class EndpointTests
{
    [Fact]
    public async Task Root_redirects_to_scalar()
    {
        await using var app = await TestApp.StartAsync();

        var res = await app.GetTestClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Equal("/scalar", res.Headers.Location?.ToString());
    }

    [Fact]
    public async Task RootRedirect_off_maps_no_root()
    {
        await using var app = await TestApp.StartAsync(endpoints: o => o.RootRedirect = false);

        var res = await app.GetTestClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Scalar_page_carries_the_title()
    {
        await using var app = await TestApp.StartAsync(endpoints: o => o.Title = "Widget API");

        var html = await app.GetTestClient().GetStringAsync("/scalar/");

        Assert.Contains("<title>Widget API</title>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Document_and_ui_are_anonymous_under_a_fallback_policy()
    {
        await using var app = await TestApp.StartAsync(fallbackPolicy: true);
        var client = app.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/openapi/v1.json")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/scalar/")).StatusCode);
    }

    [Fact]
    public async Task AllowAnonymous_off_leaves_document_to_the_fallback_policy()
    {
        await using var app = await TestApp.StartAsync(endpoints: o => o.AllowAnonymous = false, fallbackPolicy: true);
        var client = app.GetTestClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/openapi/v1.json")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/scalar/")).StatusCode);
    }
}
