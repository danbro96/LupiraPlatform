using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Lupira.Hosting.Health.UnitTests;

public sealed class HealthEndpointTests
{
    private static async Task<WebApplication> StartAsync(string environment, bool failingReadyCheck)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        var health = builder.Services.AddLupiraHealth();
        if (failingReadyCheck)
            health.AddReadyCheck<FailingCheck>("postgres");
        builder.Services.AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);
        builder.Services.AddAuthorization();
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapLupiraHealth();
        app.MapLupiraPing();
        await app.StartAsync();
        return app;
    }

    [Fact]
    public async Task Livez_is_200_even_when_a_ready_check_fails()
    {
        await using var app = await StartAsync(Environments.Production, failingReadyCheck: true);

        var res = await app.GetTestClient().GetAsync("/livez");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Readyz_is_503_when_a_ready_check_fails()
    {
        await using var app = await StartAsync(Environments.Production, failingReadyCheck: true);

        var res = await app.GetTestClient().GetAsync("/readyz");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, res.StatusCode);
    }

    [Fact]
    public async Task Readyz_is_200_without_ready_checks()
    {
        await using var app = await StartAsync(Environments.Production, failingReadyCheck: false);

        var res = await app.GetTestClient().GetAsync("/readyz");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Production_answers_the_plain_status()
    {
        await using var app = await StartAsync(Environments.Production, failingReadyCheck: true);

        var res = await app.GetTestClient().GetAsync("/readyz");

        Assert.Equal("text/plain", res.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Unhealthy", await res.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    public async Task Outside_production_the_report_is_detailed_json(string environment)
    {
        await using var app = await StartAsync(environment, failingReadyCheck: true);

        var res = await app.GetTestClient().GetAsync("/readyz");
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("application/json", res.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Unhealthy", body.GetProperty("status").GetString());
        var check = Assert.Single(body.GetProperty("checks").EnumerateArray());
        Assert.Equal("postgres", check.GetProperty("name").GetString());
        Assert.Equal("database down", check.GetProperty("description").GetString());
    }

    [Fact]
    public async Task Pingz_requires_authentication()
    {
        await using var app = await StartAsync(Environments.Production, failingReadyCheck: false);

        var res = await app.GetTestClient().GetAsync("/pingz");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Pingz_echoes_subject_audiences_and_email()
    {
        await using var app = await StartAsync(Environments.Production, failingReadyCheck: false);
        var request = new HttpRequestMessage(HttpMethod.Get, "/pingz") { Headers = { { "X-Test-Sub", "user-1" } } };

        var res = await app.GetTestClient().SendAsync(request);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("user-1", body.GetProperty("subject").GetString());
        Assert.Equal(["cal-api", "geo-api"], body.GetProperty("audiences").EnumerateArray().Select(a => a.GetString()));
        Assert.Equal("a@b.se", body.GetProperty("email").GetString());
    }
}
