using Microsoft.AspNetCore.HttpOverrides;

namespace Lupira.Hosting.Defaults;

public sealed class LupiraDefaultsOptions
{
    public const string DataProtectionKeyPathKey = "DataProtection:KeyPath";

    /// <summary>Numbers must be JSON numbers, never quoted strings.</summary>
    public bool StrictNumbers { get; set; } = true;

    public bool CaseInsensitiveProperties { get; set; }

    /// <summary>Writes every <see cref="DateTimeOffset"/> in UTC ("Z" form).</summary>
    public bool UtcDateTimeOffsets { get; set; }

    /// <summary>Binding failures throw into the exception handler outside Development too, so a 400 says which
    /// input was wrong.</summary>
    public bool ThrowOnBadRequest { get; set; }

    /// <summary><see cref="ForwardedHeaders.None"/> skips the middleware.</summary>
    public ForwardedHeaders ForwardedHeaders { get; set; } = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    /// <summary>Fills the empty body of a bare 4xx/5xx (auth challenges, <c>TypedResults.NotFound</c>) with ProblemDetails.</summary>
    public bool StatusCodePages { get; set; } = true;

    /// <summary>Prefixes with their own error shape (MCP's JSON-RPC) that status-code pages leave alone.</summary>
    public IList<string> StatusCodePagesExcludedPrefixes { get; set; } = ["/mcp"];

    /// <summary>Defaults to the host's ApplicationName; keys persist only when <c>DataProtection:KeyPath</c> is set.</summary>
    public string? DataProtectionApplicationName { get; set; }
}
