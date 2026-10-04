using Microsoft.AspNetCore.Http;

namespace Lupira.Hosting.Problems;

public sealed class ProblemExceptionOptions
{
    /// <summary>Replaces a binding failure's detail; null keeps the framework message.</summary>
    public Func<HttpContext, BadHttpRequestException, string?>? BadRequestDetail { get; set; }

    /// <summary>Detail for the 500 body (e.g. a correlation reference); null omits it.</summary>
    public Func<HttpContext, string?>? InternalErrorDetail { get; set; }
}
