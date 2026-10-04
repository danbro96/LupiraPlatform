using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;

namespace Lupira.Auth.Jwt;

/// <summary>MCP auth spec: a 401 on /mcp advertises the RFC 9728 metadata so clients can discover the
/// issuer. HandleResponse suppresses the default bare "Bearer" header so exactly one goes out.</summary>
internal static class McpChallenge
{
    public static string ResourceMetadataUrl(HttpRequest r) =>
        $"{r.Scheme}://{r.Host}/.well-known/oauth-protected-resource/mcp";

    public static Task OnChallenge(JwtBearerChallengeContext ctx)
    {
        if (ctx.Request.Path.StartsWithSegments("/mcp"))
        {
            ctx.HandleResponse();
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            ctx.Response.Headers.WWWAuthenticate = $"Bearer resource_metadata=\"{ResourceMetadataUrl(ctx.Request)}\"";
        }

        return Task.CompletedTask;
    }
}
