using Lupira.Auth.DevUser;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Lupira.Auth.Jwt.UnitTests;

/// <summary>The consumers' current Program.cs auth blocks, verbatim apart from the inlined resource-metadata URL.</summary>
internal static class TodaySetups
{
    public const string AssistantDevScheme = "DevHeader";

    public static void Simple(WebApplicationBuilder builder, bool isOpenApiBuild)
    {
        var oidc = builder.Configuration.GetSection(OidcAuthOptions.SectionName).Get<OidcAuthOptions>() ?? new OidcAuthOptions();
        if (!isOpenApiBuild && !builder.Environment.IsDevelopment()
            && (string.IsNullOrWhiteSpace(oidc.Authority) || string.IsNullOrWhiteSpace(oidc.Audience)))
            throw new InvalidOperationException("Auth:Oidc Authority + Audience are required outside Development.");

        var authBuilder = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = oidc.Authority;
                options.Audience = oidc.Audience;
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                options.Events = new JwtBearerEvents { OnChallenge = McpOnChallenge };
            });

        if (builder.Environment.IsDevelopment())
            authBuilder.AddLupiraDevHeaderAuth();
    }

    public static void Tasks(WebApplicationBuilder builder, bool isOpenApiBuild) => StrictAlways(builder, isOpenApiBuild, mcp: true, ownDevHandler: false);

    public static void Assistant(WebApplicationBuilder builder, bool isOpenApiBuild) => StrictAlways(builder, isOpenApiBuild, mcp: false, ownDevHandler: true);

    public static void Comms(WebApplicationBuilder builder, bool isOpenApiBuild)
    {
        var oidc = builder.Configuration.GetSection(OidcAuthOptions.SectionName).Get<OidcAuthOptions>() ?? new OidcAuthOptions();
        const string devOrJwtScheme = "DevOrJwt";
        var defaultScheme = builder.Environment.IsDevelopment()
            ? devOrJwtScheme
            : JwtBearerDefaults.AuthenticationScheme;

        if (!isOpenApiBuild && !builder.Environment.IsDevelopment() && !oidc.IsConfigured)
            throw new InvalidOperationException("Auth:Oidc Authority + Audience are required outside Development.");

        var authBuilder = builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = defaultScheme;
            options.DefaultChallengeScheme = defaultScheme;
        });

        if (oidc.IsConfigured)
        {
            authBuilder.AddJwtBearer(opts =>
            {
                opts.Authority = oidc.Authority;
                opts.Audience = oidc.Audience;
                opts.MapInboundClaims = false;
                opts.TokenValidationParameters = StrictParameters();
                opts.Events = new JwtBearerEvents { OnChallenge = McpOnChallenge };
            });
        }

        if (builder.Environment.IsDevelopment())
        {
            authBuilder.AddLupiraDevHeaderAuth();
            authBuilder.AddPolicyScheme(devOrJwtScheme, devOrJwtScheme, options =>
            {
                options.ForwardDefaultSelector = ctx =>
                    ctx.Request.Headers.ContainsKey(DevHeaderAuthHandler.HeaderName) || !oidc.IsConfigured
                        ? DevAuthenticationBuilderExtensions.DefaultScheme
                        : JwtBearerDefaults.AuthenticationScheme;
            });
        }
    }

    public static void Mtg(WebApplicationBuilder builder)
    {
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                builder.Configuration.GetSection("Auth").Bind(options);
                options.MapInboundClaims = false;
            });
    }

    public static Task McpOnChallenge(JwtBearerChallengeContext ctx)
    {
        if (ctx.Request.Path.StartsWithSegments("/mcp"))
        {
            ctx.HandleResponse();
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            ctx.Response.Headers.WWWAuthenticate =
                $"Bearer resource_metadata=\"{ctx.Request.Scheme}://{ctx.Request.Host}/.well-known/oauth-protected-resource/mcp\"";
        }

        return Task.CompletedTask;
    }

    private static void StrictAlways(WebApplicationBuilder builder, bool isOpenApiBuild, bool mcp, bool ownDevHandler)
    {
        var oidc = builder.Configuration.GetSection(OidcAuthOptions.SectionName).Get<OidcAuthOptions>() ?? new OidcAuthOptions();
        if (!isOpenApiBuild && string.IsNullOrWhiteSpace(oidc.Authority))
            throw new InvalidOperationException("Auth:Oidc:Authority is required.");
        if (!isOpenApiBuild && string.IsNullOrWhiteSpace(oidc.Audience))
            throw new InvalidOperationException("Auth:Oidc:Audience is required.");

        const string devOrJwtScheme = "DevOrJwt";
        var defaultScheme = builder.Environment.IsDevelopment()
            ? devOrJwtScheme
            : JwtBearerDefaults.AuthenticationScheme;

        var authBuilder = builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = defaultScheme;
            options.DefaultChallengeScheme = defaultScheme;
        });

        authBuilder.AddJwtBearer(opts =>
        {
            opts.Authority = oidc.Authority;
            opts.Audience = oidc.Audience;
            opts.MapInboundClaims = false;
            opts.TokenValidationParameters = StrictParameters();
            if (mcp)
                opts.Events = new JwtBearerEvents { OnChallenge = McpOnChallenge };
        });

        if (builder.Environment.IsDevelopment())
        {
            var devScheme = ownDevHandler ? AssistantDevScheme : DevAuthenticationBuilderExtensions.DefaultScheme;
            if (ownDevHandler)
                authBuilder.AddScheme<AuthenticationSchemeOptions, DevHeaderAuthHandler>(AssistantDevScheme, _ => { });
            else
                authBuilder.AddLupiraDevHeaderAuth();
            authBuilder.AddPolicyScheme(devOrJwtScheme, devOrJwtScheme, options =>
            {
                options.ForwardDefaultSelector = ctx =>
                    ctx.Request.Headers.ContainsKey(DevHeaderAuthHandler.HeaderName)
                        ? devScheme
                        : JwtBearerDefaults.AuthenticationScheme;
            });
        }
    }

    private static TokenValidationParameters StrictParameters() => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        NameClaimType = "email",
        RoleClaimType = "groups",
    };
}
