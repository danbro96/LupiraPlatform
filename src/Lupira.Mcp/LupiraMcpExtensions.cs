using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lupira.Mcp;

public static class LupiraMcpExtensions
{
    /// <summary>Streamable-HTTP MCP server with <see cref="StrictToolArguments"/>; chain <c>WithTools&lt;T&gt;()</c>.</summary>
    public static IMcpServerBuilder AddLupiraMcp(this IServiceCollection services) =>
        services.AddMcpServer().WithHttpTransport()
            .WithRequestFilters(f => f.AddCallToolFilter(StrictToolArguments.Filter));

    public static IEndpointConventionBuilder MapLupiraMcp(
        this IEndpointRouteBuilder app, string path = "/mcp", string policy = "ApiPolicy") =>
        app.MapMcp(path).RequireAuthorization(policy);
}
