using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lupira.Clients.ServiceTokens;

public static class ServiceTokenExtensions
{
    private static readonly TimeSpan TokenEndpointTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Registers <see cref="TokenEndpointClient"/>, <see cref="TokenCache"/> and <see cref="ServiceTokenProvider"/>
    /// as singletons; returns the token endpoint's client builder for extra handlers.</summary>
    public static IHttpClientBuilder AddLupiraTokenEndpoint(this IServiceCollection services)
    {
        var first = !services.Any(d => d.ServiceType == typeof(TokenEndpointClient));
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<TokenEndpointClient>();
        services.TryAddSingleton<TokenCache>();
        services.TryAddSingleton<ServiceTokenProvider>();

        var builder = services.AddHttpClient(TokenEndpointClient.HttpClientName);
        if (first) builder.ConfigureHttpClient(c => c.Timeout = TokenEndpointTimeout);
        return builder;
    }

    public static IHttpClientBuilder AddLupiraServiceToken(this IHttpClientBuilder builder, IOutboundHopOptions hop) =>
        builder.AddLupiraServiceToken(_ => hop);

    public static IHttpClientBuilder AddLupiraServiceToken<TOptions>(this IHttpClientBuilder builder)
        where TOptions : class, IOutboundHopOptions =>
        builder.AddLupiraServiceToken(sp => sp.GetRequiredService<IOptions<TOptions>>().Value);

    public static IHttpClientBuilder AddLupiraServiceToken(this IHttpClientBuilder builder, Func<IServiceProvider, IOutboundHopOptions> hop)
    {
        builder.Services.AddLupiraTokenEndpoint();
        return builder.AddHttpMessageHandler(sp => new ServiceTokenHandler(sp.GetRequiredService<ServiceTokenProvider>(), hop(sp)));
    }
}
