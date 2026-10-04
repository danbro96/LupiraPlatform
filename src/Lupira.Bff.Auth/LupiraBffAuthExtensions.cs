using Duende.AccessTokenManagement.OpenIdConnect;
using Lupira.Bff.Proxy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lupira.Bff.Auth;

/// <summary>
/// SSO gate for the member surface. Two front doors share one authorization policy: browsers run Authentik
/// OIDC (Authorization Code + PKCE) with a server-side cookie session and Duende token management (production;
/// non-production auto-authenticates a local user via <see cref="DevAuthHandler"/>), while native callers
/// present an Authentik-minted JWT bearer that is validated here and forwarded verbatim upstream.
/// </summary>
public static class LupiraBffAuthExtensions
{
    public static AuthenticationBuilder AddLupiraBffAuth(
        this WebApplicationBuilder builder, Action<LupiraBffAuthOptions> configure)
    {
        var options = new LupiraBffAuthOptions();
        configure(options);
        Overlay(options, builder.Configuration);
        Validate(options);

        var services = builder.Services;
        services.AddSingleton(options);
        services.AddHttpContextAccessor();
        services.AddSingleton(provider => new ApiPaths(
            options.ApiPrefixes
            ?? provider.GetService<ExposedSurface>()?.ApiPrefixes
            ?? throw new InvalidOperationException("Set ApiPrefixes or register the proxy's ExposedSurface (AddLupiraBffProxy).")));

        var production = builder.Environment.IsProduction();
        AuthenticationBuilder auth;
        string? interactiveScheme;
        if (production && options.EnableOidc)
        {
            auth = AddOidc(services, options);
            interactiveScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        }
        else if (production)
        {
            auth = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
            interactiveScheme = null;
        }
        else
        {
            auth = services.AddAuthentication(DevAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>(DevAuthHandler.SchemeName, null);
            interactiveScheme = DevAuthHandler.SchemeName;
        }

        if (options.Guest is { } guest) AddGuestCookie(auth, guest, production);

        // DefaultPolicy = authenticated via the interactive scheme OR a bearer — referenced by the YARP routes
        // ("Default"). The bearer scheme is added only when an authority is configured.
        var hasBearer = options.EnableBearer && AddBearer(auth, options, builder.Environment.IsDevelopment());
        string[] schemes = [.. new[] { interactiveScheme, hasBearer ? JwtBearerDefaults.AuthenticationScheme : null }.OfType<string>()];
        if (schemes.Length == 0)
            throw new InvalidOperationException("Production requires Auth:Bearer:Authority (or Auth:Oidc:Authority).");

        var authorization = services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder(schemes).RequireAuthenticatedUser().Build())
            .AddPolicy(LupiraBffAuthOptions.AdminPolicy, p => p
                .AddAuthenticationSchemes(schemes)
                .RequireAuthenticatedUser()
                .RequireAssertion(ctx => options.IsAdmin(ctx.User)));

        if (options.Guest is { } session)
        {
            // Names its scheme, so neither cookie can satisfy the other's policy.
            authorization.AddPolicy(session.PolicyName, p => p
                .AddAuthenticationSchemes(session.SchemeName)
                .RequireAuthenticatedUser()
                .RequireClaim(session.RequiredClaim));
        }

        if (options.RequireAuthenticatedFallback)
        {
            var fallback = new AuthorizationPolicyBuilder(schemes).RequireAuthenticatedUser();
            if (!string.IsNullOrWhiteSpace(options.RequiredGroup))
                fallback.RequireRole(options.RequiredGroup);
            authorization.SetFallbackPolicy(fallback.Build());
        }

        return auth;
    }

    private static void Overlay(LupiraBffAuthOptions options, IConfiguration configuration)
    {
        var oidc = configuration.GetSection("Auth:Oidc");
        var bearer = configuration.GetSection("Auth:Bearer");
        options.Authority = NonBlank(oidc["Authority"]) ?? options.Authority;
        options.ClientId = NonBlank(oidc["ClientId"]) ?? options.ClientId;
        options.ClientSecret = NonBlank(oidc["ClientSecret"]) ?? options.ClientSecret;
        if (oidc.GetSection("Scopes").Get<string[]>() is { Length: > 0 } scopes) options.Scopes = scopes;
        options.BearerAuthority = NonBlank(bearer["Authority"]) ?? options.BearerAuthority;
        options.Audience = NonBlank(bearer["Audience"]) ?? options.Audience;
        options.RequiredGroup = NonBlank(configuration["Auth:RequiredGroup"]) ?? options.RequiredGroup;
    }

    private static void Validate(LupiraBffAuthOptions options)
    {
        if (options.EnableOidc && options.CookieName?.StartsWith(LupiraBffAuthOptions.HostCookiePrefix, StringComparison.Ordinal) != true)
            throw new InvalidOperationException($"CookieName must start with {LupiraBffAuthOptions.HostCookiePrefix}.");
        if (options.Guest is { } guest && !guest.CookieName.StartsWith(LupiraBffAuthOptions.HostCookiePrefix, StringComparison.Ordinal))
            throw new InvalidOperationException($"Guest CookieName must start with {LupiraBffAuthOptions.HostCookiePrefix}.");
    }

    /// <summary>Registered in both environments: DevAuthHandler would otherwise mask the guest path locally.</summary>
    private static void AddGuestCookie(AuthenticationBuilder auth, LupiraGuestSessionOptions guest, bool secure) =>
        auth.AddCookie(guest.SchemeName, o =>
        {
            // The `__Host-` prefix requires Secure, and the dev BFF is plain http — a browser would drop
            // the cookie. Unlike the member session, dev cannot fall back to DevAuthHandler here.
            o.Cookie.Name = secure ? guest.CookieName : guest.CookieName[LupiraBffAuthOptions.HostCookiePrefix.Length..];
            o.Cookie.HttpOnly = true;
            o.Cookie.SecurePolicy = secure ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.SlidingExpiration = false;
            o.ExpireTimeSpan = guest.Lifetime;
            o.Events.OnRedirectToLogin = ctx => ApiAware(ctx, StatusCodes.Status401Unauthorized);
            o.Events.OnRedirectToAccessDenied = ctx => ApiAware(ctx, StatusCodes.Status403Forbidden);
        });

    private static bool AddBearer(AuthenticationBuilder auth, LupiraBffAuthOptions options, bool isDevelopment)
    {
        var authority = options.BearerAuthority ?? options.Authority;
        if (string.IsNullOrWhiteSpace(authority)) return false;   // bare dev config — interactive scheme only
        var audience = options.Audience
            ?? throw new InvalidOperationException("Bearer validation needs an Audience (Auth:Bearer:Audience).");

        auth.AddJwtBearer(o =>
        {
            o.Authority = authority;
            o.MapInboundClaims = false;
            o.RequireHttpsMetadata = !isDevelopment;
            o.TokenValidationParameters.ValidAudience = audience;
            o.TokenValidationParameters.NameClaimType = "email";
            o.TokenValidationParameters.RoleClaimType = "groups";
        });
        return true;
    }

    private static AuthenticationBuilder AddOidc(IServiceCollection services, LupiraBffAuthOptions options)
    {
        var auth = services.AddAuthentication(o =>
            {
                o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(o =>
            {
                o.Cookie.Name = options.CookieName;
                o.Cookie.HttpOnly = true;
                o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.SlidingExpiration = true;
                o.ExpireTimeSpan = TimeSpan.FromHours(8);
                o.ForwardChallenge = OpenIdConnectDefaults.AuthenticationScheme;
                // XHR calls want a 401 to react to, not an HTML redirect to Authentik.
                o.Events.OnRedirectToLogin = ctx => ApiAware(ctx, StatusCodes.Status401Unauthorized);
                o.Events.OnRedirectToAccessDenied = ctx => ApiAware(ctx, StatusCodes.Status403Forbidden);
            })
            .AddOpenIdConnect(o =>
            {
                o.Authority = options.Authority;
                o.ClientId = options.ClientId;
                // The BFF's protection is holding tokens server-side, not client auth. A secret is set only
                // if one is configured (a dedicated confidential client) — otherwise this stays a public PKCE client.
                if (!string.IsNullOrWhiteSpace(options.ClientSecret)) o.ClientSecret = options.ClientSecret;
                o.ResponseType = "code";
                o.UsePkce = true;
                o.SaveTokens = true;
                o.GetClaimsFromUserInfoEndpoint = true;
                o.MapInboundClaims = false;
                o.RequireHttpsMetadata = true;
                o.TokenValidationParameters.NameClaimType = "email";
                o.TokenValidationParameters.RoleClaimType = "groups";
                o.Scope.Clear();
                foreach (var s in options.Scopes) o.Scope.Add(s);
                // OIDC is the challenge scheme, so unauthenticated API calls land here. XHRs/native callers
                // want a 401 to react to, not a 302 to Authentik. Full-page flows like /auth/login are not
                // under an API prefix and still redirect.
                o.Events.OnRedirectToIdentityProvider = context =>
                {
                    if (IsApiPath(context.HttpContext))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.HandleResponse();
                    }

                    return Task.CompletedTask;
                };
            });

        // Keeps the forwarded access token fresh via the refresh token (offline_access).
        services.AddOpenIdConnectAccessTokenManagement();
        return auth;
    }

    private static bool IsApiPath(HttpContext context) =>
        context.RequestServices.GetRequiredService<ApiPaths>().Contains(context.Request.Path);

    private static Task ApiAware<T>(RedirectContext<T> ctx, int statusCode)
        where T : AuthenticationSchemeOptions
    {
        if (IsApiPath(ctx.HttpContext))
        {
            ctx.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        }

        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    }

    private static string? NonBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
