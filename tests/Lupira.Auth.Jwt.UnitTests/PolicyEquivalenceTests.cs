using System.Security.Claims;
using Lupira.Auth.DevUser;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lupira.Auth.Jwt.UnitTests;

public sealed class PolicyEquivalenceTests
{
    private const string GatewayClient = "lupira-dav";

    private static readonly ClaimsPrincipal[] Principals =
    [
        new(new ClaimsIdentity()),
        User("Bearer"),
        User("Bearer", ("scope", "openid internal:read")),
        User("Bearer", ("scope", "openid"), ("scope", "internal:read")),
        User("Bearer", ("scope", "internal:readx internal")),
        User("Bearer", ("azp", GatewayClient)),
        User("Bearer", ("azp", "other")),
        User(DevAuthenticationBuilderExtensions.DefaultScheme),
        User(DevAuthenticationBuilderExtensions.DefaultScheme, ("scope", "internal:read")),
    ];

    public static TheoryData<string, string, string?> Cases()
    {
        var data = new TheoryData<string, string, string?>();
        foreach (var consumer in new[] { "Cal", "Contact", "Career", "Location", "Tasks" })
        {
            foreach (var environment in new[] { "Development", "Production" })
            {
                data.Add(consumer, environment, GatewayClient);
                data.Add(consumer, environment, null);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Package_policies_match_the_consumer_policies(string consumer, string environment, string? clientId)
    {
        var expected = await DescribeAsync(environment, b => Today(consumer, b, clientId));
        var actual = await DescribeAsync(environment, b => Package(consumer, b, clientId));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Api_schemes_add_the_dev_scheme_only_in_Development()
    {
        Assert.Equal(["Bearer", "Dev"], LupiraJwtSchemes.Api(new Env("Development")));
        Assert.Equal(["Bearer"], LupiraJwtSchemes.Api(new Env("Production")));
        Assert.Equal(["Bearer", "DevHeader"], LupiraJwtSchemes.Api(new Env("Development"), "DevHeader"));
    }

    private static void Today(string consumer, WebApplicationBuilder builder, string? davGatewayClientId)
    {
        string[] apiSchemes = builder.Environment.IsDevelopment()
            ? [JwtBearerDefaults.AuthenticationScheme, DevAuthenticationBuilderExtensions.DefaultScheme]
            : [JwtBearerDefaults.AuthenticationScheme];
        switch (consumer)
        {
            case "Cal" or "Contact":
                builder.Services.AddAuthorizationBuilder()
                    .AddPolicy("ApiPolicy", p => p.AddAuthenticationSchemes(apiSchemes).RequireAuthenticatedUser())
                    .AddPolicy("DavBackendPolicy", p => p.AddAuthenticationSchemes(apiSchemes).RequireAuthenticatedUser()
                        .RequireAssertion(ctx =>
                            ctx.User.Identity?.AuthenticationType == DevAuthenticationBuilderExtensions.DefaultScheme
                            || (davGatewayClientId is not null && ctx.User.HasClaim("azp", davGatewayClientId))))
                    .AddPolicy("InternalPolicy", p => p.AddAuthenticationSchemes(apiSchemes).RequireAuthenticatedUser()
                        .RequireAssertion(ctx => ctx.User.FindAll("scope")
                            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                            .Contains("internal:read")));
                break;
            case "Career":
                builder.Services.AddAuthorizationBuilder()
                    .AddPolicy("ApiPolicy", p => p.AddAuthenticationSchemes(apiSchemes).RequireAuthenticatedUser())
                    .AddPolicy("PublicReadPolicy", p => p.AddAuthenticationSchemes(apiSchemes).RequireAuthenticatedUser());
                break;
            case "Location":
                builder.Services.AddAuthorizationBuilder()
                    .AddPolicy("ApiPolicy", p => p.AddAuthenticationSchemes(apiSchemes).RequireAuthenticatedUser())
                    .AddPolicy("IngestPolicy", p => p.AddAuthenticationSchemes("DeviceKey").RequireAuthenticatedUser())
                    .AddPolicy("InternalPolicy", p => p.AddAuthenticationSchemes(apiSchemes).RequireAuthenticatedUser()
                        .RequireAssertion(ctx => ctx.User.FindAll("scope")
                            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                            .Contains("internal:read")));
                break;
            case "Tasks":
                builder.Services.AddAuthorization(o =>
                {
                    o.AddPolicy("ShareToken", p => p.AddAuthenticationSchemes("ShareToken").RequireAuthenticatedUser());
                    o.AddPolicy("DavBackendPolicy", p => p
                        .RequireAuthenticatedUser()
                        .RequireAssertion(ctx =>
                            ctx.User.Identity?.AuthenticationType == DevAuthenticationBuilderExtensions.DefaultScheme
                            || (davGatewayClientId is not null && ctx.User.HasClaim("azp", davGatewayClientId))));
                });
                break;
        }
    }

    private static void Package(string consumer, WebApplicationBuilder builder, string? davGatewayClientId)
    {
        var apiSchemes = LupiraJwtSchemes.Api(builder.Environment);
        var authorization = builder.Services.AddAuthorizationBuilder();
        _ = consumer switch
        {
            "Cal" or "Contact" => authorization
                .AddLupiraApiPolicy(apiSchemes)
                .AddLupiraGatewayAzpPolicy(apiSchemes, davGatewayClientId)
                .AddLupiraInternalScopePolicy(apiSchemes),
            "Career" => authorization
                .AddLupiraApiPolicy(apiSchemes)
                .AddLupiraApiPolicy(apiSchemes, "PublicReadPolicy"),
            "Location" => authorization
                .AddLupiraApiPolicy(apiSchemes)
                .AddLupiraApiPolicy(["DeviceKey"], "IngestPolicy")
                .AddLupiraInternalScopePolicy(apiSchemes),
            "Tasks" => authorization
                .AddPolicy("ShareToken", p => p.AddAuthenticationSchemes("ShareToken").RequireAuthenticatedUser())
                .AddLupiraGatewayAzpPolicy([], davGatewayClientId),
            _ => throw new ArgumentOutOfRangeException(nameof(consumer)),
        };
    }

    private static async Task<string> DescribeAsync(string environment, Action<WebApplicationBuilder> configure)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = environment });
        configure(builder);
        await using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var authorization = app.Services.GetRequiredService<IAuthorizationService>();
        var lines = new List<string>
        {
            $"default: schemes [{string.Join(",", options.DefaultPolicy.AuthenticationSchemes)}] fallback {options.FallbackPolicy is not null}",
        };
        foreach (var name in new[] { "ApiPolicy", "PublicReadPolicy", "IngestPolicy", "InternalPolicy", "DavBackendPolicy", "ShareToken" })
        {
            if (options.GetPolicy(name) is not { } policy)
                continue;
            var outcomes = new List<bool>();
            foreach (var user in Principals)
                outcomes.Add((await authorization.AuthorizeAsync(user, null, policy)).Succeeded);
            lines.Add($"{name}: schemes [{string.Join(",", policy.AuthenticationSchemes)}]"
                + $" requirements [{string.Join(",", policy.Requirements.Select(r => r.GetType().Name))}]"
                + $" outcomes [{string.Join(",", outcomes)}]");
        }

        return string.Join("\n", lines);
    }

    private static ClaimsPrincipal User(string authenticationType, params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), authenticationType));
}
