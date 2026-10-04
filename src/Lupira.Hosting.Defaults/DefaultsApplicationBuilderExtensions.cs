using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lupira.Hosting.Defaults;

public static class DefaultsApplicationBuilderExtensions
{
    /// <summary>Forwarded headers then status-code pages; call first, before <c>UseExceptionHandler</c> and auth.</summary>
    public static IApplicationBuilder UseLupiraDefaults(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptions<LupiraDefaultsOptions>>().Value;

        if (options.ForwardedHeaders != ForwardedHeaders.None)
        {
            // cloudflared reaches us from a Docker-bridge IP, not loopback, so the default KnownProxies/KnownIPNetworks
            // allowlist would drop the headers — clear it. Safe only because the container's sole ingress is the tunnel.
            var forwarded = new ForwardedHeadersOptions { ForwardedHeaders = options.ForwardedHeaders };
            forwarded.KnownIPNetworks.Clear();
            forwarded.KnownProxies.Clear();
            app.UseForwardedHeaders(forwarded);
        }

        if (options.StatusCodePages)
        {
            var excluded = options.StatusCodePagesExcludedPrefixes.ToArray();
            app.UseWhen(c => !excluded.Any(p => c.Request.Path.StartsWithSegments(p)), b => b.UseStatusCodePages());
        }

        return app;
    }
}
