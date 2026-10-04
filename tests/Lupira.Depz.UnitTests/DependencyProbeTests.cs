using System.Net;
using System.Text;
using Lupira.Contracts.Depz;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lupira.Depz.UnitTests;

public sealed class DependencyProbeTests
{
    private static DependencyProbe Probe(StubHandler handler)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLupiraDepz(o =>
        {
            o.ServiceName = "lupira-test-api";
            o.MetricPrefix = "test";
        });
        services.AddHttpClient(DependencyProbe.ProbeClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider().GetRequiredService<DependencyProbe>();
    }

    private static DependencyTarget Target(IProbeCredential? credential = null, string baseUrl = "http://upstream:8080") => new()
    {
        Name = "lupira-up-api",
        BaseUrl = baseUrl,
        ProbePath = "readyz",
        Credential = credential,
    };

    [Theory]
    [InlineData(HttpStatusCode.OK, DependencyStatus.Healthy)]
    [InlineData(HttpStatusCode.NoContent, DependencyStatus.Healthy)]
    [InlineData(HttpStatusCode.Unauthorized, DependencyStatus.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, DependencyStatus.Unauthorized)]
    [InlineData(HttpStatusCode.ServiceUnavailable, DependencyStatus.Degraded)]
    [InlineData(HttpStatusCode.NotFound, DependencyStatus.Degraded)]
    public async Task Maps_the_probe_status(HttpStatusCode code, DependencyStatus expected)
    {
        var result = await Probe(StubHandler.Status(code)).ProbeAsync(Target(), CancellationToken.None);

        Assert.Equal(expected, result.Status);
        Assert.Equal("lupira-up-api", result.Name);
        Assert.NotNull(result.LatencyMs);
        if (expected == DependencyStatus.Healthy) Assert.Null(result.Error);
        else Assert.Equal($"readyz returned {(int) code}", result.Error);
    }

    [Fact]
    public async Task Probes_the_path_under_the_base_url()
    {
        var handler = StubHandler.Status(HttpStatusCode.OK);

        await Probe(handler).ProbeAsync(Target(baseUrl: "http://upstream:8080/svc"), CancellationToken.None);

        Assert.Equal("http://upstream:8080/svc/readyz", Assert.Single(handler.Requests).RequestUri!.ToString());
    }

    [Fact]
    public async Task A_blank_base_url_is_unconfigured_and_never_sent()
    {
        var handler = StubHandler.Status(HttpStatusCode.OK);

        var result = await Probe(handler).ProbeAsync(Target(baseUrl: " "), CancellationToken.None);

        Assert.Equal(DependencyStatus.Unconfigured, result.Status);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task A_transport_failure_is_down()
    {
        var result = await Probe(new StubHandler(_ => throw new HttpRequestException("refused")))
            .ProbeAsync(Target(), CancellationToken.None);

        Assert.Equal(DependencyStatus.Down, result.Status);
        Assert.Equal("refused", result.Error);
    }

    [Fact]
    public async Task Client_credentials_mint_once_and_send_the_bearer()
    {
        var handler = new StubHandler(req => req.Method == HttpMethod.Post
            ? Json("""{"access_token":"tok","expires_in":300}""")
            : new HttpResponseMessage(HttpStatusCode.OK));
        var probe = Probe(handler);
        var target = Target(new ClientCredentialsProbeCredential
        {
            TokenUrl = "http://idp/token",
            ClientId = "id",
            ClientSecret = "secret",
            Scope = "api",
        });

        await probe.ProbeAsync(target, CancellationToken.None);
        var result = await probe.ProbeAsync(target, CancellationToken.None);

        Assert.Equal(DependencyStatus.Healthy, result.Status);
        Assert.Single(handler.Requests, r => r.Method == HttpMethod.Post);
        Assert.All(handler.Requests.Where(r => r.Method == HttpMethod.Get), r => Assert.Equal("Bearer tok", r.Headers.Authorization!.ToString()));
    }

    [Fact]
    public async Task A_refused_token_is_no_credential_with_the_idp_body()
    {
        var handler = new StubHandler(req => req.Method == HttpMethod.Post
            ? Json("""{"error":"invalid_client"}""", HttpStatusCode.BadRequest)
            : new HttpResponseMessage(HttpStatusCode.OK));
        var target = Target(new ClientCredentialsProbeCredential { TokenUrl = "http://idp/token", ClientId = "id", ClientSecret = "bad" });

        var result = await Probe(handler).ProbeAsync(target, CancellationToken.None);

        Assert.Equal(DependencyStatus.NoCredential, result.Status);
        Assert.Equal("""token mint failed: HTTP 400: {"error":"invalid_client"}""", result.Error);
        Assert.DoesNotContain(handler.Requests, r => r.Method == HttpMethod.Get);
    }

    [Fact]
    public async Task Without_client_credentials_the_dev_user_header_is_sent()
    {
        var handler = StubHandler.Status(HttpStatusCode.OK);

        await Probe(handler).ProbeAsync(Target(new ClientCredentialsProbeCredential { DevUser = "dev@lupira.test" }), CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("dev@lupira.test", request.Headers.GetValues("X-Dev-User").Single());
        Assert.Null(request.Headers.Authorization);
    }

    [Theory]
    [InlineData("bearer", "Authorization", "Bearer k")]
    [InlineData("apikey", "X-API-Key", "k")]
    [InlineData("basic", "Authorization", "Basic dTpr")]
    public async Task Static_header_credentials_are_sent(string kind, string header, string expected)
    {
        var handler = StubHandler.Status(HttpStatusCode.OK);
        var credential = kind switch
        {
            "bearer" => StaticHeaderProbeCredential.Bearer("k"),
            "apikey" => StaticHeaderProbeCredential.ApiKey("k"),
            _ => StaticHeaderProbeCredential.Basic("u", "k"),
        };

        await Probe(handler).ProbeAsync(Target(credential), CancellationToken.None);

        Assert.Equal(expected, Assert.Single(handler.Requests).Headers.GetValues(header).Single());
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}
