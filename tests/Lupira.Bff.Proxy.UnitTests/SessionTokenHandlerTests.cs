using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Lupira.Bff.Proxy.UnitTests;

public class SessionTokenHandlerTests
{
    [Fact]
    public async Task Caller_bearer_passes_verbatim()
    {
        var sent = await Send("Production", ctx => ctx.Request.Headers.Authorization = "Bearer native-token");

        Assert.Equal("Bearer native-token", sent.Headers.Authorization?.ToString());
        Assert.False(sent.Headers.Contains(DevUser.HeaderName));
    }

    [Fact]
    public async Task Development_replaces_the_dev_user_header()
    {
        var sent = await Send("Development", _ => { }, request => request.Headers.TryAddWithoutValidation(DevUser.HeaderName, "intruder@test"));

        Assert.Equal(["dev@test"], sent.Headers.GetValues(DevUser.HeaderName));
        Assert.Null(sent.Headers.Authorization);
    }

    [Fact]
    public async Task Production_without_a_session_sends_no_credential()
    {
        var sent = await Send("Production", _ => { });

        Assert.Null(sent.Headers.Authorization);
        Assert.False(sent.Headers.Contains(DevUser.HeaderName));
    }

    private static async Task<HttpRequestMessage> Send(
        string environment, Action<HttpContext> arrange, Action<HttpRequestMessage>? prepare = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new(DevUser.ConfigurationKey, "dev@test")])
            .Build();
        var context = new DefaultHttpContext { RequestServices = new ServiceCollection().BuildServiceProvider() };
        arrange(context);

        var capture = new CaptureHandler();
        var handler = new SessionTokenHandler(
            new HttpContextAccessor { HttpContext = context },
            new Environment { EnvironmentName = environment },
            configuration)
        {
            InnerHandler = capture,
        };

        var request = new HttpRequestMessage(HttpMethod.Get, "http://upstream/items");
        prepare?.Invoke(request);
        using var invoker = new HttpMessageInvoker(handler);
        await invoker.SendAsync(request, CancellationToken.None);
        return capture.Request!;
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage());
        }
    }

    private sealed class Environment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";

        public string ApplicationName { get; set; } = "test";

        public string WebRootPath { get; set; } = string.Empty;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
