using System.Text.Encodings.Web;
using Lupira.Mcp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lupira.Testing.Mcp.UnitTests;

/// <summary>Stands in for the JWT bearer's challenge in a Lupira API: <c>/mcp</c> 401s point at the RFC 9728 metadata.</summary>
public sealed class McpChallengeHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Scheme = "Bearer";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers.WWWAuthenticate = Request.Path.StartsWithSegments("/mcp")
            ? $"Bearer resource_metadata=\"{McpResourceMetadata.ResourceMetadataUrl(Request)}\""
            : "Bearer";
        return Task.CompletedTask;
    }
}
