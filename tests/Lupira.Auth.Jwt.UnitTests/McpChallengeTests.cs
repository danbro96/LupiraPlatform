using System.Net;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Lupira.Auth.Jwt.UnitTests;

public sealed class McpChallengeTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Unauthenticated_mcp_401_advertises_the_resource_metadata(bool devOrJwt)
    {
        await using var app = await JwtTestHost.StartAsync(o => o.DevOrJwtDefault = devOrJwt, apiPolicy: !devOrJwt);

        var resp = await app.GetTestClient().GetAsync("/mcp");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        var challenge = Assert.Single(resp.Headers.WwwAuthenticate).ToString();
        Assert.Equal("Bearer resource_metadata=\"http://localhost/.well-known/oauth-protected-resource/mcp\"", challenge);
    }

    [Fact]
    public async Task Rest_401_does_not_advertise_mcp_metadata()
    {
        await using var app = await JwtTestHost.StartAsync();

        var resp = await app.GetTestClient().GetAsync("/me");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        Assert.DoesNotContain(resp.Headers.WwwAuthenticate.Select(h => h.ToString()), c => c.Contains("resource_metadata"));
    }

    [Fact]
    public async Task Without_the_mcp_challenge_mcp_gets_the_plain_bearer_challenge()
    {
        await using var app = await JwtTestHost.StartAsync(o => o.McpChallenge = false);

        var resp = await app.GetTestClient().GetAsync("/mcp");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        Assert.Equal(["Bearer"], resp.Headers.WwwAuthenticate.Select(h => h.ToString()));
    }
}
