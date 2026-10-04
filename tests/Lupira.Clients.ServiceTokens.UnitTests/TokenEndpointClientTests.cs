using System.Net;
using Xunit;

namespace Lupira.Clients.ServiceTokens.UnitTests;

public class TokenEndpointClientTests
{
    private static readonly TokenExchangeOptions Exchange = new()
    {
        TokenUrl = "https://auth.test/application/o/token/",
        ClientId = "lupira-cal",
        ClientSecret = "s3cret",
    };

    private static readonly OutboundHopOptions Hop = new()
    {
        TokenUrl = "https://auth.test/application/o/token/",
        ClientId = "lupira-geo-svc",
        ClientSecret = "svc-secret",
        Scope = "lupira-geo-aud",
    };

    private static TokenEndpointClient Client(TokenStubHandler handler) => new(new StubHttpClientFactory(handler));

    [Fact]
    public async Task Exchange_posts_the_rfc8693_form()
    {
        var handler = new TokenStubHandler();
        var issued = await Client(handler).ExchangeAsync(Exchange, "member-token", "lupira-contact", default);

        var form = Assert.Single(handler.Forms);
        Assert.Equal("urn:ietf:params:oauth:grant-type:token-exchange", form["grant_type"]);
        Assert.Equal("lupira-cal", form["client_id"]);
        Assert.Equal("s3cret", form["client_secret"]);
        Assert.Equal("member-token", form["subject_token"]);
        Assert.Equal("urn:ietf:params:oauth:token-type:access_token", form["subject_token_type"]);
        Assert.Equal("lupira-contact", form["audience"]);
        Assert.Equal("openid profile email", form["scope"]);
        Assert.Equal("tok-lupira-contact", issued.AccessToken);
        Assert.Equal(TimeSpan.FromSeconds(300), issued.ExpiresIn);
        Assert.Null(issued.RotatedRefreshToken);
    }

    [Fact]
    public async Task Client_credentials_posts_the_hop_credentials_and_scope()
    {
        var handler = new TokenStubHandler();
        await Client(handler).ClientCredentialsAsync(Hop, default);

        var form = Assert.Single(handler.Forms);
        Assert.Equal("client_credentials", form["grant_type"]);
        Assert.Equal("lupira-geo-svc", form["client_id"]);
        Assert.Equal("lupira-geo-aud", form["scope"]);
    }

    [Fact]
    public async Task Client_credentials_omits_a_blank_scope()
    {
        var handler = new TokenStubHandler();
        await Client(handler).ClientCredentialsAsync(new OutboundHopOptions { TokenUrl = "https://auth.test/t/", ClientId = "c", ClientSecret = "s" }, default);

        Assert.DoesNotContain("scope", Assert.Single(handler.Forms).Keys);
    }

    [Fact]
    public async Task Refresh_posts_the_refresh_grant_without_scope_and_returns_the_rotated_token()
    {
        var handler = new TokenStubHandler { Respond = _ => (HttpStatusCode.OK, """{"access_token":"at","expires_in":300,"refresh_token":"rt-2"}""") };

        var issued = await Client(handler).RefreshAsync(Exchange, "rt-1", default);

        Assert.Equal("https://auth.test/application/o/token/", handler.Uris.Single()!.ToString());
        var form = Assert.Single(handler.Forms);
        Assert.Equal(["client_id", "client_secret", "grant_type", "refresh_token"], form.Keys.Order());
        Assert.Equal("refresh_token", form["grant_type"]);
        Assert.Equal("rt-1", form["refresh_token"]);
        Assert.Equal("at", issued.AccessToken);
        Assert.Equal("rt-2", issued.RotatedRefreshToken);
    }

    [Theory]
    [InlineData("access_denied", TokenErrorKind.AccessDenied)]
    [InlineData("invalid_grant", TokenErrorKind.InvalidGrant)]
    [InlineData("invalid_target", TokenErrorKind.InvalidTarget)]
    [InlineData("invalid_client", TokenErrorKind.InvalidClient)]
    [InlineData("invalid_request", TokenErrorKind.InvalidRequest)]
    [InlineData("invalid_scope", TokenErrorKind.InvalidScope)]
    [InlineData("something_else", TokenErrorKind.Other)]
    public async Task Maps_rfc_error_bodies(string error, TokenErrorKind kind)
    {
        var handler = new TokenStubHandler { Respond = _ => (HttpStatusCode.BadRequest, $$"""{"error":"{{error}}","error_description":"nope"}""") };

        var ex = await Assert.ThrowsAsync<TokenEndpointException>(() => Client(handler).ExchangeAsync(Exchange, "t", "lupira-contact", default));

        Assert.Equal(kind, ex.Kind);
        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("nope", ex.Description);
    }

    [Fact]
    public async Task Non_json_error_is_unavailable_and_keeps_the_body()
    {
        var handler = new TokenStubHandler { Respond = _ => (HttpStatusCode.BadGateway, "<html>502</html>") };

        var ex = await Assert.ThrowsAsync<TokenEndpointException>(() => Client(handler).ExchangeAsync(Exchange, "t", "lupira-contact", default));

        Assert.Equal(TokenErrorKind.Unavailable, ex.Kind);
        Assert.Contains("<html>502</html>", ex.Description);
    }

    [Fact]
    public async Task Non_oauth_json_error_keeps_the_body()
    {
        var handler = new TokenStubHandler { Respond = _ => (HttpStatusCode.InternalServerError, """{"detail":"boom"}""") };

        var ex = await Assert.ThrowsAsync<TokenEndpointException>(() => Client(handler).ClientCredentialsAsync(Hop, default));

        Assert.Equal(TokenErrorKind.Other, ex.Kind);
        Assert.Equal("""{"detail":"boom"}""", ex.Description);
    }

    [Fact]
    public async Task Missing_access_token_is_other()
    {
        var handler = new TokenStubHandler { Respond = _ => (HttpStatusCode.OK, """{"expires_in":300}""") };

        var ex = await Assert.ThrowsAsync<TokenEndpointException>(() => Client(handler).RefreshAsync(Exchange, "rt", default));

        Assert.Equal(TokenErrorKind.Other, ex.Kind);
    }

    [Fact]
    public async Task Missing_expires_in_defaults_to_five_minutes()
    {
        var handler = new TokenStubHandler { Respond = _ => (HttpStatusCode.OK, """{"access_token":"at"}""") };

        var issued = await Client(handler).RefreshAsync(Exchange, "rt", default);

        Assert.Equal(TimeSpan.FromMinutes(5), issued.ExpiresIn);
    }

    [Theory]
    [InlineData(TokenErrorKind.InvalidGrant, "invalid_grant")]
    [InlineData(TokenErrorKind.InvalidTarget, "invalid_target")]
    [InlineData(TokenErrorKind.AccessDenied, "access_denied")]
    [InlineData(TokenErrorKind.Unavailable, null)]
    [InlineData(TokenErrorKind.Other, null)]
    public void Kind_round_trips_to_the_wire(TokenErrorKind kind, string? wire)
    {
        Assert.Equal(wire, kind.ToWire());
        if (wire is not null) Assert.Equal(kind, TokenErrorKindWire.Parse(wire));
    }
}
