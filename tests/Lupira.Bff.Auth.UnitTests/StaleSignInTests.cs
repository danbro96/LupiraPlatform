using System.Net;
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
}
