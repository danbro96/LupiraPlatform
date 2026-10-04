using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lupira.Auth.DevUser;

/// <summary>
/// Non-production auth: authenticates every request as the configured local user (<c>Dev:User</c>, groups from
/// <c>Dev:Groups</c>) so the member surface is usable without an Authentik round-trip.
/// </summary>
public sealed class DevConfigUserAuthHandler(
    IOptionsMonitor<DevConfigUserOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<DevConfigUserOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var email = configuration[DevConfigUserOptions.UserKey] ?? Options.DefaultUser;
        var groups = configuration.GetSection(DevConfigUserOptions.GroupsKey).Get<string[]>() ?? [.. Options.DefaultGroups];
        var principal = DevClaims.Principal(Scheme.Name, email, groups, [new("name", email)]);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
