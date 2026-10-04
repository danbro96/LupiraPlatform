using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Lupira.Bff.Auth.UnitTests;

public sealed class AuthOptionsTests
{
    [Fact]
    public void Session_cookie_must_carry_the_host_prefix()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });

        var error = Assert.Throws<InvalidOperationException>(() => builder.AddLupiraBffAuth(o =>
        {
            AuthHost.Cal(o);
            o.CookieName = "lupira-cal";
        }));
        Assert.Contains("__Host-", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Guest_cookie_must_carry_the_host_prefix()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });

        Assert.Throws<InvalidOperationException>(() => builder.AddLupiraBffAuth(o =>
        {
            AuthHost.Cal(o);
            o.Guest = new LupiraGuestSessionOptions { CookieName = "guest", RequiredClaim = "share-token" };
        }));
    }

    [Fact]
    public void Bearer_only_production_without_an_authority_fails_fast()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });

        Assert.Throws<InvalidOperationException>(() => builder.AddLupiraBffAuth(o =>
        {
            o.EnableBearer = true;
            o.Audience = "lupira-assistant";
        }));
    }

    [Fact]
    public async Task Fallback_policy_gates_unannotated_endpoints_on_the_required_group()
    {
        await using var host = await AuthHost.StartAsync(
            "assistant",
            "Production",
            o =>
            {
                o.EnableBearer = true;
                o.BearerAuthority = AuthHost.Issuer;
                o.Audience = "lupira-family";
                o.RequireAuthenticatedFallback = true;
                o.RequiredGroup = "family";
            },
            app => app.MapGet("/test/plain", () => TypedResults.Ok()));

        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client().GetAsync("/test/plain")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client(AuthHost.MintToken("lupira-family")).GetAsync("/test/plain")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client(AuthHost.MintToken("lupira-family", groups: ["family"])).GetAsync("/test/plain")).StatusCode);
    }
}
