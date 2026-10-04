using System.Security.Claims;
using System.Text;
using Lupira.Bff.Proxy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Lupira.Bff.Auth.UnitTests;

/// <summary>
/// Hosts a BFF wired with the shipped auth policy, with the YARP clusters pointed at an in-process stub
/// upstream. The bearer scheme is re-keyed to a local symmetric signing key so tests mint their own tokens;
/// the OIDC handler gets a static configuration so challenges never fetch metadata.
/// </summary>
public sealed class AuthHost : IAsyncDisposable
{
    public const string Issuer = "https://auth.test/";

    private static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("lupira-bff-auth-unit-test-signing-key-0123456789abcdef"));

    private readonly WebApplication _app;

    private AuthHost(WebApplication app, StubUpstream upstream, ExposedSurface surface)
    {
        _app = app;
        Upstream = upstream;
        Surface = surface;
    }

    public StubUpstream Upstream { get; }

    public ExposedSurface Surface { get; }

    public static void Cal(LupiraBffAuthOptions o)
    {
        o.EnableOidc = true;
        o.EnableBearer = true;
        o.Authority = Issuer;
        o.ClientId = "lupira-cal";
        o.Audience = "lupira-cal";
        o.CookieName = "__Host-lupira-cal";
        o.AdminGroups = ["cal-admins", "platform-admins"];
        o.DevGroups = ["cal-admins"];
    }

    public static async Task<AuthHost> StartAsync(
        string fixture,
        string environment,
        Action<LupiraBffAuthOptions> configure,
        Action<WebApplication>? map = null,
        IReadOnlyDictionary<string, string>? settings = null)
    {
        var upstream = await StubUpstream.StartAsync();
        var surface = ExposedSurface.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", $"{fixture}.exposed.json")));

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        foreach (var cluster in surface.Operations.Select(o => o.Cluster).Distinct())
            builder.Configuration[$"ReverseProxy:Clusters:{cluster}:Destinations:primary:Address"] = upstream.Address;
        foreach (var (key, value) in settings ?? new Dictionary<string, string>())
            builder.Configuration[key] = value;

        builder.AddLupiraBffProxy(o => o.Surface = surface);
        builder.AddLupiraBffAuth(configure);
        builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
        {
            o.Authority = null;
            o.ConfigurationManager = null;
            o.RequireHttpsMetadata = false;
            o.TokenValidationParameters.ValidIssuer = Issuer;
            o.TokenValidationParameters.IssuerSigningKey = SigningKey;
        });
        builder.Services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, o =>
        {
            o.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(
                new OpenIdConnectConfiguration
                {
                    Issuer = Issuer,
                    AuthorizationEndpoint = $"{Issuer}authorize",
                    TokenEndpoint = $"{Issuer}token",
                });
        });

        var app = builder.Build();
        app.UseLupiraBffDeviceKeyGate();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapLupiraAuthEndpoints();
        map?.Invoke(app);
        app.MapLupiraBffProxy();
        app.MapFallback(() => Results.Content("<html></html>", "text/html"));
        await app.StartAsync();
        return new AuthHost(app, upstream, surface);
    }

    public static string MintToken(string audience = "lupira-cal", string email = "user@test", params string[] groups)
    {
        var claims = new List<Claim> { new("email", email), new("sub", $"test|{email}") };
        claims.AddRange(groups.Select(g => new Claim("groups", g)));
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            Expires = DateTime.UtcNow.AddMinutes(10),
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
        });
    }

    public HttpClient Client(string? bearer = null)
    {
        var client = _app.GetTestClient();
        if (bearer is not null) client.DefaultRequestHeaders.Authorization = new("Bearer", bearer);
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
        await Upstream.DisposeAsync();
    }
}
