using System.Text;
using Lupira.Auth.DevUser;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lupira.Auth.Jwt.UnitTests;

/// <summary>Everything a setup resolves to — schemes, defaults, bearer options, challenge and DevOrJwt forwarding — as text.</summary>
internal static class AuthSnapshot
{
    public const string Authority = "https://auth.test/application/o/lupira-test/";
    public const string Audience = "lupira-test";

    public static async Task<string> TakeAsync(
        Action<WebApplicationBuilder, bool> setup, string environment, bool configured, bool isOpenApiBuild = false)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.Configuration.AddInMemoryCollection(Settings(configured));
        try
        {
            setup(builder, isOpenApiBuild);
        }
        catch (InvalidOperationException e)
        {
            return "throws: " + e.Message;
        }

        await using var app = builder.Build();
        return await DescribeAsync(app.Services);
    }

    private static Dictionary<string, string?> Settings(bool configured)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Auth:LastSeenWriteInterval"] = "00:15:00",
            ["Auth:AllowedOrigins:0"] = "https://mtg.test",
        };
        if (configured)
        {
            settings["Auth:Oidc:Authority"] = Authority;
            settings["Auth:Oidc:Audience"] = Audience;
            settings["Auth:Authority"] = Authority;
            settings["Auth:Audience"] = Audience;
        }

        return settings;
    }

    private static async Task<string> DescribeAsync(IServiceProvider services)
    {
        var sb = new StringBuilder();
        var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
        foreach (var scheme in (await provider.GetAllSchemesAsync()).OrderBy(s => s.Name, StringComparer.Ordinal))
            sb.AppendLine($"scheme {scheme.Name} = {scheme.HandlerType.Name}");

        var auth = services.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        sb.AppendLine($"default {auth.DefaultScheme} / authenticate {auth.DefaultAuthenticateScheme} / challenge {auth.DefaultChallengeScheme}"
            + $" / forbid {auth.DefaultForbidScheme} / signIn {auth.DefaultSignInScheme} / signOut {auth.DefaultSignOutScheme}");
        sb.AppendLine($"resolved authenticate {(await provider.GetDefaultAuthenticateSchemeAsync())?.Name}"
            + $" / challenge {(await provider.GetDefaultChallengeSchemeAsync())?.Name} / forbid {(await provider.GetDefaultForbidSchemeAsync())?.Name}");

        if (await provider.GetSchemeAsync(JwtBearerDefaults.AuthenticationScheme) is { } bearerScheme)
        {
            var o = services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(bearerScheme.Name);
            var p = o.TokenValidationParameters;
            sb.AppendLine($"bearer authority {o.Authority} audience {o.Audience} metadata {o.MetadataAddress} https {o.RequireHttpsMetadata}"
                + $" mapInbound {o.MapInboundClaims} saveToken {o.SaveToken} errorDetails {o.IncludeErrorDetails}"
                + $" configManager {o.ConfigurationManager is not null} handlers {string.Join(",", o.TokenHandlers.Select(h => h.GetType().Name))}");
            sb.AppendLine($"tvp issuer {p.ValidateIssuer} audience {p.ValidateAudience} lifetime {p.ValidateLifetime}"
                + $" signingKey {p.ValidateIssuerSigningKey} requireExp {p.RequireExpirationTime} requireSigned {p.RequireSignedTokens}"
                + $" name {p.NameClaimType} role {p.RoleClaimType} validAudience {p.ValidAudience} validIssuer {p.ValidIssuer}"
                + $" skew {p.ClockSkew} saveSignin {p.SaveSigninToken} authType {p.AuthenticationType}");
            foreach (var path in new[] { "/mcp", "/mcp/tools", "/me" })
                sb.AppendLine($"challenge {path}: {await ChallengeAsync(o, bearerScheme, path)}");
        }

        if (await provider.GetSchemeAsync(LupiraJwtOptions.DevOrJwtScheme) is { } policyScheme)
        {
            var selector = services.GetRequiredService<IOptionsMonitor<PolicySchemeOptions>>().Get(policyScheme.Name).ForwardDefaultSelector!;
            var withHeader = new DefaultHttpContext();
            withHeader.Request.Headers[DevHeaderAuthHandler.HeaderName] = "a@b.se";
            sb.AppendLine($"devOrJwt header -> {selector(withHeader)}, none -> {selector(new DefaultHttpContext())}");
        }

        return sb.ToString();
    }

    private static async Task<string> ChallengeAsync(JwtBearerOptions options, AuthenticationScheme scheme, string path)
    {
        var http = new DefaultHttpContext();
        http.Request.Scheme = "http";
        http.Request.Host = new HostString("localhost");
        http.Request.Path = path;
        var ctx = new JwtBearerChallengeContext(http, scheme, options, new AuthenticationProperties());
        await options.Events.Challenge(ctx);
        return $"handled {ctx.Handled} status {http.Response.StatusCode} www-authenticate {http.Response.Headers.WWWAuthenticate}";
    }
}
