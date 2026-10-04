using System.Security.Claims;
using Lupira.Auth.Service.AspNetCore;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Lupira.Auth.Service.UnitTests;

public sealed class CurrentServiceTests
{
    private static CurrentService For(params Claim[] claims) =>
        new(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) } });

    [Fact]
    public void The_service_id_claim_wins_over_token_client_claims()
    {
        Assert.Equal("dev", For(new Claim("sub", "s"), new Claim(ServiceAuthHandler.ServiceIdClaim, "dev"), new Claim("azp", "a")).ServiceId);
    }

    [Fact]
    public void A_client_credentials_token_falls_back_azp_then_client_id_then_sub()
    {
        Assert.Equal("a", For(new Claim("sub", "s"), new Claim("client_id", "c"), new Claim("azp", "a")).ServiceId);
        Assert.Equal("c", For(new Claim("sub", "s"), new Claim("client_id", "c")).ServiceId);
        Assert.Equal("s", For(new Claim("sub", "s")).ServiceId);
    }

    [Fact]
    public void RequireCaller_throws_without_a_service_identity()
    {
        Assert.Throws<InvalidOperationException>(() => For().RequireCaller());
    }
}
