using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lupira.Clients.ServiceTokens.UnitTests;

public class ServiceTokenHandlerTests
{
    private static readonly OutboundHopOptions Hop = new()
    {
        BaseUrl = "http://assistant.test/",
        TokenUrl = "https://auth.test/application/o/token/",
        ClientId = "lupira-assistant-svc",
        ClientSecret = "svc-secret",
    };

    private static (HttpClient Client, RecordingHandler Target, TokenStubHandler Tokens) Create(Action<IHttpClientBuilder> addAuth, Action<IServiceCollection>? configure = null)
    {
        var target = new RecordingHandler();
        var tokens = new TokenStubHandler();
        var services = new ServiceCollection();
        configure?.Invoke(services);
        addAuth(services.AddHttpClient("hop", c => c.BaseAddress = new Uri("http://assistant.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => target));
        services.AddLupiraTokenEndpoint().ConfigurePrimaryHttpMessageHandler(() => tokens);
        var client = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient("hop");
        return (client, target, tokens);
    }

    [Fact]
    public async Task Attaches_the_bearer_and_reuses_it()
    {
        var (client, target, tokens) = Create(b => b.AddLupiraServiceToken(Hop));

        await client.GetAsync("fires");
        await client.GetAsync("fires");

        Assert.All(target.Requests, r => Assert.Equal("Bearer tok-lupira-assistant-svc", r.Headers.Authorization?.ToString()));
        Assert.Single(tokens.Forms);
    }

    [Fact]
    public async Task Binds_the_hop_from_options()
    {
        var (client, target, _) = Create(
            b => b.AddLupiraServiceToken<OutboundHopOptions>(),
            s => s.Configure<OutboundHopOptions>(o => o.DevServiceId = "lupira-cal-dispatcher"));

        await client.GetAsync("fires");

        Assert.Equal("lupira-cal-dispatcher", Assert.Single(target.Requests).Headers.GetValues("X-Dev-Service").Single());
    }

    [Fact]
    public async Task Mint_failure_is_an_http_request_exception_and_skips_the_call()
    {
        var (client, target, tokens) = Create(b => b.AddLupiraServiceToken(Hop));
        tokens.Respond = _ => (HttpStatusCode.Unauthorized, """{"error":"invalid_client","error_description":"bad secret"}""");

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("fires"));

        var inner = Assert.IsType<TokenEndpointException>(ex.InnerException);
        Assert.Equal(TokenErrorKind.InvalidClient, inner.Kind);
        Assert.Contains("bad secret", ex.Message);
        Assert.Empty(target.Requests);
    }

    [Fact]
    public void Token_endpoint_registration_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddLupiraTokenEndpoint();
        services.AddLupiraTokenEndpoint();
        var sp = services.BuildServiceProvider();

        Assert.Single(services, d => d.ServiceType == typeof(ServiceTokenProvider));
        Assert.Equal(TimeSpan.FromSeconds(10), sp.GetRequiredService<IHttpClientFactory>().CreateClient(TokenEndpointClient.HttpClientName).Timeout);
    }
}
