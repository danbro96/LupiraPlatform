using System.Security.Claims;
using System.Text.Encodings.Web;
using Lupira.Bff.Proxy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lupira.Bff.Auth;

/// <summary>
/// Non-production auth: authenticates every request as the configured local user (<c>Dev:User</c>) so
/// the member surface is usable without an Authentik round-trip. Member API calls are forwarded with an
/// <c>X-Dev-User</c> header, which the upstreams' dev handlers accept.
/// </summary>
public sealed class DevAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration,
    LupiraBffAuthOptions auth)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Dev";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var email = DevUser.From(configuration);
        var claims = new List<Claim>
        {
            new("sub", "dev|" + email),
            new("email", email),
            new("name", email),
        };

        var groups = configuration.GetSection("Dev:Groups").Get<string[]>() ?? [.. auth.DevGroups];
        foreach (var g in groups)
            claims.Add(new Claim("groups", g));

        // Satisfy an optional group gate locally so dev isn't locked out when a required group is set.
        if (!string.IsNullOrWhiteSpace(auth.RequiredGroup) && !groups.Contains(auth.RequiredGroup))
            claims.Add(new Claim("groups", auth.RequiredGroup));

        var identity = new ClaimsIdentity(claims, SchemeName, nameType: "email", roleType: "groups");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
