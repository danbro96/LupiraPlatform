using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Lupira.Auth.Jwt.UnitTests;

public sealed class DevOrJwtTests
{
    [Fact]
    public async Task The_default_policy_accepts_the_dev_header_in_Development()
    {
        await using var app = await JwtTestHost.StartAsync(o => o.DevOrJwtDefault = true, apiPolicy: false);
        var request = new HttpRequestMessage(HttpMethod.Get, "/me");
        request.Headers.Add("X-Dev-User", "Alice@Example.SE");

        var resp = await app.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("alice@example.se", await resp.Content.ReadFromJsonAsync<string>());
    }

    [Fact]
    public async Task The_api_policy_accepts_the_dev_header_in_Development()
    {
        await using var app = await JwtTestHost.StartAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/me");
        request.Headers.Add("X-Dev-User", "bob@example.se");

        var resp = await app.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
