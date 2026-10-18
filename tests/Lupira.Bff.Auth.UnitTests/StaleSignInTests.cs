using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lupira.Bff.Auth.UnitTests;

public sealed class StaleSignInTests
{
    [Theory]
    [InlineData("/calendar?day=1", "/calendar?day=1")]
    [InlineData("https://evil.example/x", "/")]
    public async Task A_lost_correlation_cookie_challenges_again_for_the_same_page(string redirectUri, string expected)
    {
        await using var host = await AuthHost.StartAsync("cal", "Production", AuthHost.Cal);

        var res = await host.Client().SendAsync(Callback(Protect(host, new AuthenticationProperties { RedirectUri = redirectUri })));

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        var retry = RetryState(host, res);
        Assert.Equal(expected, retry.RedirectUri);
        Assert.True(retry.Items.ContainsKey(LupiraBffAuthExtensions.RetriedItem));
        Assert.Contains(res.Headers.GetValues("Set-Cookie"), c => c.StartsWith("__Host-lupira-cal-retry=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task An_unreadable_state_challenges_again_for_the_root()
    {
        await using var host = await AuthHost.StartAsync("cal", "Production", AuthHost.Cal);

        var res = await host.Client().SendAsync(Callback("not-a-state"));

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Equal("/", RetryState(host, res).RedirectUri);
    }

    [Fact]
    public async Task A_callback_without_state_challenges_again_for_the_root()
    {
        await using var host = await AuthHost.StartAsync("cal", "Production", AuthHost.Cal);

        var res = await host.Client().SendAsync(new HttpRequestMessage(HttpMethod.Post, "/signin-oidc")
        {
            Content = new FormUrlEncodedContent([new("code", "c")]),
        });

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Equal("/", RetryState(host, res).RedirectUri);
    }

    [Fact]
    public async Task A_retried_sign_in_that_fails_again_is_not_retried()
    {
        await using var host = await AuthHost.StartAsync("cal", "Production", AuthHost.Cal);
        var retried = new AuthenticationProperties { RedirectUri = "/" };
        retried.Items[LupiraBffAuthExtensions.RetriedItem] = "1";

        await Assert.ThrowsAsync<AuthenticationFailureException>(() => host.Client().SendAsync(Callback(Protect(host, retried))));
    }

    [Fact]
    public async Task An_unreadable_state_inside_the_retry_window_is_not_retried()
    {
        await using var host = await AuthHost.StartAsync("cal", "Production", AuthHost.Cal);
        var request = Callback("not-a-state");
        request.Headers.Add("Cookie", "__Host-lupira-cal-retry=1");

        await Assert.ThrowsAsync<AuthenticationFailureException>(() => host.Client().SendAsync(request));
    }

    [Fact]
    public async Task Other_remote_failures_are_not_retried()
    {
        await using var host = await AuthHost.StartAsync("cal", "Production", AuthHost.Cal);
        var properties = new AuthenticationProperties { RedirectUri = "/" };
        properties.Items[".xsrf"] = "nonce";
        var request = Callback(Protect(host, properties), ("error", "access_denied"));
        request.Headers.Add("Cookie", ".AspNetCore.Correlation.nonce=N");

        await Assert.ThrowsAsync<AuthenticationFailureException>(() => host.Client().SendAsync(request));
    }

    [Fact]
    public async Task A_successful_sign_in_forgets_the_retry_mark()
    {
        await using var host = await AuthHost.StartAsync("cal", "Production", AuthHost.Cal, oidc: IdentityProvider.Configure);

        var res = await host.Client().SendAsync(SuccessfulCallback(host, "__Host-lupira-cal-retry=1"));

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Equal("/calendar", res.Headers.Location?.OriginalString);
        var forgotten = Assert.Single(res.Headers.GetValues("Set-Cookie"), c => c.StartsWith("__Host-lupira-cal-retry=", StringComparison.Ordinal));
        Assert.StartsWith("__Host-lupira-cal-retry=; expires=Thu, 01 Jan 1970", forgotten, StringComparison.Ordinal);
        Assert.Contains("path=/", forgotten, StringComparison.Ordinal);
        Assert.Contains("secure", forgotten, StringComparison.Ordinal);
        Assert.Contains("samesite=none", forgotten, StringComparison.Ordinal);
        Assert.Contains("httponly", forgotten, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_sign_in_without_a_retry_mark_sets_no_cookie()
    {
        await using var host = await AuthHost.StartAsync("cal", "Production", AuthHost.Cal, oidc: IdentityProvider.Configure);

        var res = await host.Client().SendAsync(SuccessfulCallback(host));

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains(res.Headers.GetValues("Set-Cookie"), c => c.StartsWith("__Host-lupira-cal=", StringComparison.Ordinal));
        Assert.DoesNotContain(res.Headers.GetValues("Set-Cookie"), c => c.StartsWith("__Host-lupira-cal-retry=", StringComparison.Ordinal));
    }

    private static HttpRequestMessage SuccessfulCallback(AuthHost host, string? cookie = null)
    {
        var properties = new AuthenticationProperties { RedirectUri = "/calendar" };
        properties.Items[".xsrf"] = "nonce";
        properties.Items[OpenIdConnectDefaults.RedirectUriForCodePropertiesKey] = "http://localhost/signin-oidc";
        var request = Callback(Protect(host, properties), ("code", "c"));
        request.Headers.Add("Cookie", string.Join("; ", new[] { ".AspNetCore.Correlation.nonce=N", cookie }.OfType<string>()));
        return request;
    }

    private static HttpRequestMessage Callback(string state, params (string Key, string Value)[] fields) =>
        new(HttpMethod.Post, "/signin-oidc")
        {
            Content = new FormUrlEncodedContent([new("state", state), .. fields.Select(f => KeyValuePair.Create(f.Key, f.Value))]),
        };

    private static ISecureDataFormat<AuthenticationProperties> StateFormat(AuthHost host) =>
        host.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme).StateDataFormat;

    private static string Protect(AuthHost host, AuthenticationProperties properties) => StateFormat(host).Protect(properties);

    private static AuthenticationProperties RetryState(AuthHost host, HttpResponseMessage res)
    {
        var location = res.Headers.Location!;
        Assert.StartsWith($"{AuthHost.Issuer}authorize", location.ToString(), StringComparison.Ordinal);
        return StateFormat(host).Unprotect(QueryHelpers.ParseQuery(location.Query)["state"])!;
    }

    private sealed class IdentityProvider : HttpMessageHandler
    {
        public static void Configure(OpenIdConnectOptions o)
        {
            o.Backchannel = new HttpClient(new IdentityProvider());
            o.ProtocolValidator.RequireNonce = false;
            o.TokenValidationParameters.IssuerSigningKey = AuthHost.SigningKey;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            object body = request.RequestUri!.AbsolutePath == "/token"
                ? new { id_token = AuthHost.MintToken(), access_token = "at", token_type = "Bearer", expires_in = 300 }
                : new { sub = "test|user@test", email = "user@test" };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
        }
    }
}
