using Lupira.Auth.DevUser;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Lupira.Auth.Jwt;

public static class JwtHostApplicationBuilderExtensions
{
    public static AuthenticationBuilder AddLupiraJwt(this IHostApplicationBuilder builder, Action<LupiraJwtOptions>? configure = null) =>
        builder.AddLupiraJwt(configure, Environment.GetCommandLineArgs());

    internal static AuthenticationBuilder AddLupiraJwt(
        this IHostApplicationBuilder builder, Action<LupiraJwtOptions>? configure, IEnumerable<string> commandLineArgs)
    {
        var options = new LupiraJwtOptions();
        configure?.Invoke(options);
        var isDevelopment = builder.Environment.IsDevelopment();

        var oidc = builder.Configuration.GetSection(options.SectionName).Get<OidcAuthOptions>() ?? new OidcAuthOptions();
        if (!OpenApiBuild.IsRunning(commandLineArgs))
            EnsureConfigured(options, oidc, isDevelopment);

        if (options.DevOrJwtDefault && isDevelopment && options.DevScheme is null)
            throw new InvalidOperationException($"{nameof(LupiraJwtOptions.DevOrJwtDefault)} needs a {nameof(LupiraJwtOptions.DevScheme)}.");

        var defaultScheme = options.DevOrJwtDefault && isDevelopment ? LupiraJwtOptions.DevOrJwtScheme : JwtBearerDefaults.AuthenticationScheme;
        var auth = options.DevOrJwtDefault
            ? builder.Services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = defaultScheme;
                o.DefaultChallengeScheme = defaultScheme;
            })
            : builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

        var hasBearer = oidc.IsConfigured || !options.SkipBearerWhenUnconfigured;
        if (hasBearer)
            auth.AddJwtBearer(o => ConfigureBearer(o, options, oidc, isDevelopment));

        if (isDevelopment && options.DevScheme is { } devScheme)
        {
            if (options.RegisterDevScheme)
                auth.AddLupiraDevHeaderAuth(devScheme);
            if (options.DevOrJwtDefault)
            {
                auth.AddPolicyScheme(LupiraJwtOptions.DevOrJwtScheme, LupiraJwtOptions.DevOrJwtScheme, o =>
                    o.ForwardDefaultSelector = ctx =>
                        ctx.Request.Headers.ContainsKey(DevHeaderAuthHandler.HeaderName) || !hasBearer
                            ? devScheme
                            : JwtBearerDefaults.AuthenticationScheme);
            }
        }

        return auth;
    }

    private static void EnsureConfigured(LupiraJwtOptions options, OidcAuthOptions oidc, bool isDevelopment)
    {
        switch (options.RequireConfig)
        {
            case OidcConfigRequirement.OutsideDevelopment when !isDevelopment && !oidc.IsConfigured:
                throw new InvalidOperationException($"{options.SectionName} Authority + Audience are required outside Development.");
            case OidcConfigRequirement.Always when string.IsNullOrWhiteSpace(oidc.Authority):
                throw new InvalidOperationException($"{options.SectionName}:Authority is required.");
            case OidcConfigRequirement.Always when string.IsNullOrWhiteSpace(oidc.Audience):
                throw new InvalidOperationException($"{options.SectionName}:Audience is required.");
        }
    }

    private static void ConfigureBearer(JwtBearerOptions bearer, LupiraJwtOptions options, OidcAuthOptions oidc, bool isDevelopment)
    {
        bearer.Authority = oidc.Authority;
        bearer.Audience = oidc.Audience;
        if (options.RelaxHttpsMetadataInDevelopment)
            bearer.RequireHttpsMetadata = !isDevelopment;
        if (options.Validation != JwtValidationProfile.Default)
            bearer.MapInboundClaims = false;
        if (options.Validation == JwtValidationProfile.Strict)
        {
            bearer.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                NameClaimType = DevClaims.NameType,
                RoleClaimType = DevClaims.RoleType,
            };
        }

        if (options.McpChallenge)
            bearer.Events = new JwtBearerEvents { OnChallenge = McpChallenge.OnChallenge };
    }
}
