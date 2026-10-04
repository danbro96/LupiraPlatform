using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lupira.Bff.Proxy.UnitTests;

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    public const string ShareHeader = "X-Test-Share";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>();
        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.Ordinal))
        {
            var token = authorization["Bearer ".Length..];
            if (token == "bad") return Task.FromResult(AuthenticateResult.Fail("bad token"));
            claims.Add(new Claim("email", token));
        }

        if (Request.Headers[ShareHeader].ToString() is { Length: > 0 } share)
            claims.Add(new Claim("share-token", share));

        if (claims.Count == 0) return Task.FromResult(AuthenticateResult.NoResult());

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
