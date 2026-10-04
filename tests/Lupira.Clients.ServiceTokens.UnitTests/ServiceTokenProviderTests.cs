using System.Net;
using Xunit;

namespace Lupira.Clients.ServiceTokens.UnitTests;

public class ServiceTokenProviderTests
{
    private static OutboundHopOptions Hop(string scope) => new()
    {
        BaseUrl = "http://geo.test/",
        TokenUrl = "https://auth.test/application/o/token/",
        ClientId = "lupira-photo-svc",
        ClientSecret = "svc-secret",
        Scope = scope,
    };

    private static (ServiceTokenProvider Provider, TokenStubHandler Tokens) Create()
    {
        var tokens = new TokenStubHandler();
        return (new ServiceTokenProvider(new TokenEndpointClient(new StubHttpClientFactory(tokens)), new TokenCache(TimeProvider.System)), tokens);
    }

    private static async Task<HttpRequestMessage> ApplyAsync(ServiceTokenProvider provider, IOutboundHopOptions hop, string? devUserOverride = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "http://hop.test/");
        if (devUserOverride is not null) req.Options.Set(ServiceTokenProvider.DevUserOverride, devUserOverride);
        await provider.ApplyAsync(req, hop, default);
        return req;
    }

    private static string? Header(HttpRequestMessage req, string name) =>
        req.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : null;

    [Fact]
    public async Task Credentials_attach_one_cached_bearer_per_scope()
    {
        var (provider, tokens) = Create();

        var geo = await ApplyAsync(provider, Hop("lupira-geo-aud"));
        await ApplyAsync(provider, Hop("lupira-geo-aud"));
        var location = await ApplyAsync(provider, Hop("lupira-location-aud"));

        Assert.Equal("Bearer tok-lupira-geo-aud", Header(geo, "Authorization"));
        Assert.Equal("Bearer tok-lupira-location-aud", Header(location, "Authorization"));
        Assert.Equal(2, tokens.Forms.Count);
    }

    [Fact]
    public async Task Credentials_win_over_dev_headers()
    {
        var (provider, _) = Create();
        var hop = Hop("lupira-geo-aud");
        hop.DevServiceId = "dev-svc";
        hop.DevUser = "a@x.test";

        var req = await ApplyAsync(provider, hop, devUserOverride: "acting@x.test");

        Assert.NotNull(req.Headers.Authorization);
        Assert.Null(Header(req, "X-Dev-Service"));
        Assert.Null(Header(req, "X-Dev-User"));
    }

    [Fact]
    public async Task Mint_failure_throws_the_token_endpoint_error()
    {
        var (provider, tokens) = Create();
        tokens.Respond = _ => (HttpStatusCode.BadRequest, """{"error":"invalid_scope","error_description":"unknown scope"}""");

        var ex = await Assert.ThrowsAsync<TokenEndpointException>(() => ApplyAsync(provider, Hop("nope")));

        Assert.Equal(TokenErrorKind.InvalidScope, ex.Kind);
    }

    [Fact]
    public async Task Dev_service_id_sends_x_dev_service()
    {
        var (provider, tokens) = Create();

        var req = await ApplyAsync(provider, new OutboundHopOptions { BaseUrl = "http://a.test/", DevServiceId = "lupira-comms-dev" });

        Assert.Equal("lupira-comms-dev", Header(req, "X-Dev-Service"));
        Assert.Null(Header(req, "X-Dev-User"));
        Assert.Empty(tokens.Forms);
    }

    [Fact]
    public async Task Dev_user_sends_x_dev_user_with_scopes_when_set()
    {
        var (provider, _) = Create();

        var plain = await ApplyAsync(provider, new OutboundHopOptions { DevUser = "svc@x.test" });
        var scoped = await ApplyAsync(provider, new OutboundHopOptions { DevUser = "svc@x.test", DevScopes = "internal:read" });

        Assert.Equal("svc@x.test", Header(plain, "X-Dev-User"));
        Assert.Null(Header(plain, "X-Dev-Scopes"));
        Assert.Equal("svc@x.test", Header(scoped, "X-Dev-User"));
        Assert.Equal("internal:read", Header(scoped, "X-Dev-Scopes"));
    }

    [Fact]
    public async Task Request_dev_user_override_wins_over_the_hop()
    {
        var (provider, _) = Create();

        var req = await ApplyAsync(provider, new OutboundHopOptions { DevUser = "svc@x.test", DevServiceId = "dev-svc" }, devUserOverride: "acting@x.test");

        Assert.Equal("acting@x.test", Header(req, "X-Dev-User"));
        Assert.Null(Header(req, "X-Dev-Service"));
    }

    [Fact]
    public async Task Nothing_configured_sends_no_auth()
    {
        var (provider, _) = Create();

        var req = await ApplyAsync(provider, new OutboundHopOptions { BaseUrl = "http://a.test/" });

        Assert.Empty(req.Headers);
    }

    [Fact]
    public async Task Interface_defaults_leave_dev_fields_unset()
    {
        var (provider, _) = Create();

        var req = await ApplyAsync(provider, new MinimalHop());

        Assert.Empty(req.Headers);
    }

    private sealed class MinimalHop : IOutboundHopOptions
    {
        public string? TokenUrl => null;

        public string? ClientId => null;

        public string? ClientSecret => null;

        public string? Scope => null;

        public bool IsConfigured => true;
    }
}
