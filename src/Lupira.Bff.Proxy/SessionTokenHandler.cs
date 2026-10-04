using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Lupira.Bff.Proxy;

/// <summary>
/// Attaches the caller's credential to an upstream call, the same way the YARP transform does for a
/// proxied route — that transform only runs on proxy routes, so a handler would otherwise call
/// upstream unauthenticated.
/// </summary>
public sealed class SessionTokenHandler(IHttpContextAccessor accessor, IHostEnvironment environment, IConfiguration configuration)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = accessor.HttpContext;
        if (context is null) return await base.SendAsync(request, cancellationToken);

        var incoming = context.Request.Headers.Authorization.ToString();
        if (incoming.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            // A native caller presented its own token; the upstreams validate it themselves.
            request.Headers.TryAddWithoutValidation("Authorization", incoming);
        }
        else if (environment.IsDevelopment())
        {
            request.Headers.Remove(DevUser.HeaderName);
            request.Headers.TryAddWithoutValidation(DevUser.HeaderName, DevUser.From(configuration));
        }
        else if (await UpstreamCredentialTransforms.SessionAccessToken(context) is { } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
