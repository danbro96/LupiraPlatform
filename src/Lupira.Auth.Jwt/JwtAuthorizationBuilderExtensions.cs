using Lupira.Auth.DevUser;
using Microsoft.AspNetCore.Authorization;

namespace Lupira.Auth.Jwt;

public static class JwtAuthorizationBuilderExtensions
{
    public const string ApiPolicy = "ApiPolicy";
    public const string InternalPolicy = "InternalPolicy";
    public const string InternalReadScope = "internal:read";
    public const string DavBackendPolicy = "DavBackendPolicy";

    public static AuthorizationBuilder AddLupiraApiPolicy(this AuthorizationBuilder authorization, string[] schemes, string name = ApiPolicy) =>
        authorization.AddPolicy(name, p => p.AddAuthenticationSchemes(schemes).RequireAuthenticatedUser());

    /// <summary>Granted only to service clients — user tokens authenticate but never carry the scope.</summary>
    public static AuthorizationBuilder AddLupiraInternalScopePolicy(
        this AuthorizationBuilder authorization, string[] schemes, string name = InternalPolicy, string scope = InternalReadScope) =>
        authorization.AddPolicy(name, p => p.AddAuthenticationSchemes(schemes).RequireAuthenticatedUser()
            .RequireAssertion(ctx => ctx.User.FindAll("scope")
                .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Contains(scope)));

    /// <summary>
    /// A gateway's service identity: a valid token for this API (aud) minted by the gateway's client (<c>azp</c> ==
    /// <paramref name="clientId"/>). Dev-header auth passes in Development so tests can drive the seam directly.
    /// </summary>
    public static AuthorizationBuilder AddLupiraGatewayAzpPolicy(
        this AuthorizationBuilder authorization,
        string[] schemes,
        string? clientId,
        string name = DavBackendPolicy,
        string devScheme = DevAuthenticationBuilderExtensions.DefaultScheme) =>
        authorization.AddPolicy(name, p => p.AddAuthenticationSchemes(schemes).RequireAuthenticatedUser()
            .RequireAssertion(ctx =>
                ctx.User.Identity?.AuthenticationType == devScheme
                || (clientId is not null && ctx.User.HasClaim("azp", clientId))));
}
