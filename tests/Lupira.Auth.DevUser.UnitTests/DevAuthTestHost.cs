using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lupira.Auth.DevUser.UnitTests;

internal static class DevAuthTestHost
{
    public static async Task<WebApplication> StartAsync(
        Action<AuthenticationBuilder> configure, Dictionary<string, string?>? settings = null, string scheme = DevAuthenticationBuilderExtensions.DefaultScheme)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        if (settings is not null)
            builder.Configuration.AddInMemoryCollection(settings);
        configure(builder.Services.AddAuthentication(scheme));
        var app = builder.Build();
        app.MapGet("/who", async (HttpContext ctx) =>
        {
            var result = await ctx.AuthenticateAsync();
            return TypedResults.Ok(new Who
            {
                Outcome = result.Succeeded ? "success" : result.None ? "none" : "fail",
                Name = result.Principal?.Identity?.Name,
                AuthenticationType = result.Principal?.Identity?.AuthenticationType,
                Claims = result.Principal?.Claims.Select(c => $"{c.Type}={c.Value}").ToArray() ?? [],
                Groups = result.Principal?.Claims.Where(c => result.Principal.IsInRole(c.Value)).Select(c => c.Value).ToArray() ?? [],
            });
        });
        await app.StartAsync();
        return app;
    }

    public static async Task<Who> WhoAsync(this WebApplication app, params (string Name, string Value)[] headers)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/who");
        foreach (var (name, value) in headers)
            request.Headers.TryAddWithoutValidation(name, value);
        var res = await app.GetTestClient().SendAsync(request);
        return (await res.Content.ReadFromJsonAsync<Who>())!;
    }
}
