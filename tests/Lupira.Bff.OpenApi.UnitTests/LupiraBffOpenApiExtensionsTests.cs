using Lupira.Bff.Proxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lupira.Bff.OpenApi.UnitTests;

public class LupiraBffOpenApiExtensionsTests
{
    [Fact]
    public void The_proxy_route_guards_read_the_embedded_upstream_specs()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment
        {
            ApplicationName = typeof(LupiraBffOpenApiExtensionsTests).Assembly.GetName().Name!,
        });
        services.AddLupiraBffOpenApi(o => o.Upstreams.Add(new UpstreamSpec { Cluster = "tasks-api", Name = "LupiraTasksApi" }));

        var specs = services.BuildServiceProvider().GetRequiredService<IOptions<RouteGuardOptions>>().Value.Specs;

        Assert.Equal(["tasks-api"], specs.Keys);
        Assert.NotNull(specs["tasks-api"]["paths"]?["/lists/{listId}"]);
    }
}
