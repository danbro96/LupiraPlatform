using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lupira.Auth.Jwt.UnitTests;

internal static class JwtTestHost
{
    public static async Task<WebApplication> StartAsync(Action<LupiraJwtOptions>? configure = null, bool apiPolicy = true)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth:Oidc:Authority"] = AuthSnapshot.Authority,
            ["Auth:Oidc:Audience"] = AuthSnapshot.Audience,
        });
        builder.AddLupiraJwt(configure, []);
        builder.Services.AddAuthorizationBuilder().AddLupiraApiPolicy(LupiraJwtSchemes.Api(builder.Environment));
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        var mcp = app.MapGet("/mcp", (HttpContext ctx) => TypedResults.Ok(ctx.User.Identity?.Name));
        var me = app.MapGet("/me", (HttpContext ctx) => TypedResults.Ok(ctx.User.Identity?.Name));
        if (apiPolicy)
        {
            mcp.RequireAuthorization("ApiPolicy");
            me.RequireAuthorization("ApiPolicy");
        }
        else
        {
            mcp.RequireAuthorization();
            me.RequireAuthorization();
        }

        await app.StartAsync();
        return app;
    }
}
