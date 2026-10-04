using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lupira.Depz.UnitTests;

public sealed class DepzEndpointTests
{
    private const string Key = "probe-secret";

    private static async Task<WebApplication> StartAsync(string probeKey, Action<WebApplication>? map = null)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLupiraDepz(o =>
        {
            o.ProbeKey = probeKey;
            o.ServiceName = "lupira-test-web";
            o.MetricPrefix = "test";
            o.StartupDelay = TimeSpan.FromHours(1);
        });
        builder.Services.AddLupiraDepzTargets([]);
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
        builder.Services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        map?.Invoke(app);
        app.MapDepz();
        await app.StartAsync();
        return app;
    }

    private static HttpClient Client(WebApplication app, string? key)
    {
        var client = app.GetTestClient();
        if (key is not null) client.DefaultRequestHeaders.Add(ProbeKeyFilter.HeaderName, key);
        return client;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-key")]
    public async Task Without_the_probe_key_is_401_not_a_login_challenge(string? key)
    {
        await using var app = await StartAsync(Key);

        var res = await Client(app, key).GetAsync("/depz");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Null(res.Headers.Location);
    }

    [Fact]
    public async Task With_the_probe_key_serves_the_report_anonymously()
    {
        await using var app = await StartAsync(Key);

        var res = await Client(app, Key).GetAsync("/depz");

        res.EnsureSuccessStatusCode();
        var report = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("lupira-test-web", report.GetProperty("service").GetString());
        Assert.Equal(JsonValueKind.Null, report.GetProperty("lastPolledUtc").ValueKind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task A_blank_configured_key_turns_the_endpoint_off(string? presented)
    {
        await using var app = await StartAsync(string.Empty);

        var res = await Client(app, presented).GetAsync("/depz");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task A_host_endpoint_may_use_any_name()
    {
        await using var app = await StartAsync(Key, a => a.MapGet("/api/dependencies", () => "host").WithName("GetDependencies").AllowAnonymous());

        Assert.Equal(HttpStatusCode.OK, (await Client(app, Key).GetAsync("/depz")).StatusCode);
    }
}
