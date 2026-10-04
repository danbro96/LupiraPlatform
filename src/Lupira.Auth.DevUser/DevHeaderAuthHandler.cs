using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lupira.Auth.DevUser;

/// <summary>
/// DEVELOPMENT-ONLY auth: authenticates as the member named in the <c>X-Dev-User</c> header (an email), so the
/// API can be exercised locally without Authentik. Optional <c>X-Dev-Groups</c> (comma-separated),
/// <c>X-Dev-Scopes</c> and <c>X-Dev-Client</c> add <c>groups</c>, <c>scope</c> and <c>azp</c>. Register it only
/// when the environment is Development.
/// </summary>
public sealed class DevHeaderAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string HeaderName = "X-Dev-User";
    public const string GroupsHeaderName = "X-Dev-Groups";
    public const string ScopesHeaderName = "X-Dev-Scopes";
    public const string ClientHeaderName = "X-Dev-Client";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // No dev header → let authorization challenge (401), same as a missing bearer.
        if (!Request.Headers.TryGetValue(HeaderName, out var emailRaw))
            return Task.FromResult(AuthenticateResult.NoResult());

        var email = emailRaw.ToString().Trim().ToLowerInvariant();
        if (email.Length == 0)
            return Task.FromResult(AuthenticateResult.Fail($"'{HeaderName}' header is present but empty."));

        var groups = Request.Headers[GroupsHeaderName].ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var extra = new List<Claim>();
        if (Header(ScopesHeaderName) is { } scopes)
            extra.Add(new Claim("scope", scopes));
        if (Header(ClientHeaderName) is { } client)
            extra.Add(new Claim("azp", client));

        var principal = DevClaims.Principal(Scheme.Name, email, groups, extra);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }

    private string? Header(string name)
    {
        var value = Request.Headers[name].ToString().Trim();
        return value.Length == 0 ? null : value;
    }
}
