using System.Net.Http.Headers;
using System.Security.Claims;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Lupira.Bff.Proxy;

internal static class UpstreamCredentialTransforms
{
    public static void Apply(TransformBuilderContext context, ProxyRoute? route, bool isDevelopment, string devUser)
    {
        if (route?.Group.PathMap is { } map)
        {
            var mount = (route.RemovePrefix ?? string.Empty) + map.Bff;
            context.AddRequestTransform(transform => RefillPath(transform, mount, map));
        }

        switch (route?.Group.Credential ?? UpstreamCredential.Session)
        {
            case UpstreamCredential.Session:
                context.AddRequestTransform(transform => Session(transform, isDevelopment, devUser));
                break;
            case UpstreamCredential.None:
                context.AddRequestTransform(Strip);
                break;
            case UpstreamCredential.DeviceKey:
                // Only the upstream can validate a device key, so it must reach the upstream intact.
                break;
        }
    }

    internal static async Task<string?> SessionAccessToken(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true
            || context.RequestServices.GetService<IUserTokenManager>() is null)
        {
            return null;
        }

        var token = await context.GetUserAccessTokenAsync().GetToken();
        var accessToken = token.AccessToken.ToString();
        return string.IsNullOrEmpty(accessToken) ? null : accessToken;
    }

    private static async ValueTask Session(RequestTransformContext transform, bool isDevelopment, string devUser)
    {
        var incoming = transform.HttpContext.Request.Headers.Authorization.ToString();
        if (incoming.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return;   // native caller — its own token flows through untouched

        if (isDevelopment)
        {
            // Replace, never append: StringValues joins duplicates with a comma and the upstream's dev
            // handler derives its principal from the value, so a caller-supplied header would other-
            // wise change who the request runs as.
            transform.ProxyRequest.Headers.Remove(DevUser.HeaderName);
            transform.ProxyRequest.Headers.TryAddWithoutValidation(DevUser.HeaderName, devUser);
            return;
        }

        if (await SessionAccessToken(transform.HttpContext) is { } token)
            transform.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // Attaching a member token would widen the link to that member.
    private static ValueTask Strip(RequestTransformContext transform)
    {
        transform.ProxyRequest.Headers.Authorization = null;
        transform.ProxyRequest.Headers.Remove(DevUser.HeaderName);
        return ValueTask.CompletedTask;
    }

    // Path is set from the original request so this does not depend on transform ordering.
    private static ValueTask RefillPath(RequestTransformContext transform, string mount, ExposedPathMap map)
    {
        if (!transform.HttpContext.Request.Path.StartsWithSegments(mount, out var rest))
            return ValueTask.CompletedTask;

        var upstream = map.Upstream;
        foreach (var (parameter, claim) in map.Claims)
        {
            var value = transform.HttpContext.User.FindFirstValue(claim);
            if (string.IsNullOrEmpty(value)) return ValueTask.CompletedTask;   // the group's policy already rejected this
            upstream = upstream.Replace($"{{{parameter}}}", value, StringComparison.Ordinal);
        }

        transform.Path = new PathString(upstream).Add(rest);
        return ValueTask.CompletedTask;
    }
}
