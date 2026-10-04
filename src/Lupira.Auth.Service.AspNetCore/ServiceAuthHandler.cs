using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lupira.Auth.Service.AspNetCore;

/// <summary>
/// Authenticates the internal LAN hops. A service caller relays a principal's <i>id</i> in the request body; it
/// never acts as itself against user records.
///
/// <para>
/// A Development <c>X-Dev-Service</c> header (the service id) makes the intake exercisable + tested
/// without Authentik. Production plugs in Authentik client-credentials via the separate "ServiceJwt"
/// scheme, so the <c>Service</c> authorization policy is unchanged.
/// </para>
/// </summary>
public sealed class ServiceAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Service";
    public const string DevHeaderName = "X-Dev-Service";
    public const string ServiceIdClaim = "service_id";

    private readonly IHostEnvironment _env;

    public ServiceAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHostEnvironment env)
        : base(options, logger, encoder)
    {
        _env = env;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // The dev header is honoured ONLY in Development — in prod the "ServiceJwt" scheme (Authentik
        // client-credentials) authenticates the Service policy and this handler is a no-op.
        if (!_env.IsDevelopment())
            return Task.FromResult(AuthenticateResult.NoResult());

        if (Request.Headers.TryGetValue(DevHeaderName, out var raw))
        {
            var serviceId = raw.ToString().Trim();
            if (string.IsNullOrEmpty(serviceId))
                return Task.FromResult(AuthenticateResult.Fail($"'{DevHeaderName}' header is present but empty."));

            var identity = new ClaimsIdentity([new Claim(ServiceIdClaim, serviceId)], SchemeName);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }
}
