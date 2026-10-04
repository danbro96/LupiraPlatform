using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lupira.Hosting.Defaults.UnitTests;

public sealed class PipelineDefaultsTests
{
    private static async Task<string?[]> RequestAsync(Microsoft.AspNetCore.Builder.WebApplication app)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/request")
        {
            Headers =
            {
                { "X-Forwarded-Proto", "https" },
                { "X-Forwarded-For", "203.0.113.7" },
                { "X-Forwarded-Host", "cal.lupira.com" },
            },
        };
        var res = await app.GetTestClient().SendAsync(request);
        return (await res.Content.ReadFromJsonAsync<string?[]>())!;
    }

    [Fact]
    public async Task Forwarded_for_and_proto_are_trusted_but_not_host()
    {
        await using var app = await DefaultsTestHost.StartAsync();

        var values = await RequestAsync(app);

        Assert.Equal("https", values[0]);
        Assert.Equal("localhost", values[1]);
        Assert.Equal("203.0.113.7", values[2]);
    }

    [Fact]
    public async Task Forwarded_host_is_opt_in()
    {
        await using var app = await DefaultsTestHost.StartAsync(o => o.ForwardedHeaders |= ForwardedHeaders.XForwardedHost);

        var values = await RequestAsync(app);

        Assert.Equal("cal.lupira.com", values[1]);
    }

    [Fact]
    public async Task Forwarded_headers_can_be_switched_off()
    {
        await using var app = await DefaultsTestHost.StartAsync(o => o.ForwardedHeaders = ForwardedHeaders.None);

        var values = await RequestAsync(app);

        Assert.Equal("http", values[0]);
    }

    [Fact]
    public async Task A_bare_404_becomes_a_problem()
    {
        await using var app = await DefaultsTestHost.StartAsync();

        var res = await app.GetTestClient().GetAsync("/missing");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Excluded_prefixes_keep_the_bare_status()
    {
        await using var app = await DefaultsTestHost.StartAsync(o => o.StatusCodePagesExcludedPrefixes.Add("/internal"));

        var mcp = await app.GetTestClient().GetAsync("/mcp/missing");
        var internalRes = await app.GetTestClient().GetAsync("/internal/missing");

        Assert.Empty(await mcp.Content.ReadAsByteArrayAsync());
        Assert.Empty(await internalRes.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Status_code_pages_can_be_switched_off()
    {
        await using var app = await DefaultsTestHost.StartAsync(o => o.StatusCodePages = false);

        var res = await app.GetTestClient().GetAsync("/missing");

        Assert.Empty(await res.Content.ReadAsByteArrayAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Throw_on_bad_request_is_opt_in(bool enabled)
    {
        await using var app = await DefaultsTestHost.StartAsync(o => o.ThrowOnBadRequest = enabled);

        Assert.Equal(enabled, app.Services.GetRequiredService<IOptions<RouteHandlerOptions>>().Value.ThrowOnBadRequest);
    }

    [Fact]
    public async Task Keys_persist_to_the_configured_path_under_the_application_name()
    {
        var keyPath = Directory.CreateTempSubdirectory("lupira-dp-").FullName;
        try
        {
            await using var app = await DefaultsTestHost.StartAsync(settings: new() { [LupiraDefaultsOptions.DataProtectionKeyPathKey] = keyPath });

            app.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("test").Protect("payload");

            Assert.NotEmpty(Directory.GetFiles(keyPath, "key-*.xml"));
            Assert.Equal("SampleApi", app.Services.GetRequiredService<IOptions<DataProtectionOptions>>().Value.ApplicationDiscriminator);
        }
        finally
        {
            Directory.Delete(keyPath, recursive: true);
        }
    }

    [Fact]
    public async Task The_application_name_can_be_overridden()
    {
        var keyPath = Directory.CreateTempSubdirectory("lupira-dp-").FullName;
        try
        {
            await using var app = await DefaultsTestHost.StartAsync(
                o => o.DataProtectionApplicationName = "LupiraCalBff",
                new() { [LupiraDefaultsOptions.DataProtectionKeyPathKey] = keyPath });

            Assert.Equal("LupiraCalBff", app.Services.GetRequiredService<IOptions<DataProtectionOptions>>().Value.ApplicationDiscriminator);
        }
        finally
        {
            Directory.Delete(keyPath, recursive: true);
        }
    }

    [Fact]
    public async Task Without_a_key_path_no_repository_is_set()
    {
        await using var app = await DefaultsTestHost.StartAsync();

        Assert.Null(app.Services.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository);
    }
}
