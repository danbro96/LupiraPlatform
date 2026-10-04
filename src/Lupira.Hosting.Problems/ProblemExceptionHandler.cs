using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lupira.Hosting.Problems;

/// <summary>
/// Catches the faults the result mapping doesn't — genuinely unhandled ones — logs them once and returns
/// the same ProblemDetails shape as the rest of the surface, so a client has a single error-parser path.
/// The trace id rides on the <c>traceId</c> extension that AddProblemDetails stamps.
/// </summary>
internal sealed class ProblemExceptionHandler(ILogger<ProblemExceptionHandler> log, IOptions<ProblemExceptionOptions> options)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        // A malformed body or bad route value surfaces as BadHttpRequestException carrying its own
        // status — the caller's mistake, not a fault, and not a 500.
        if (exception is BadHttpRequestException bad)
        {
            await TypedResults.Problem(
                title: "Bad request",
                detail: options.Value.BadRequestDetail?.Invoke(context, bad) ?? bad.Message,
                statusCode: bad.StatusCode,
                type: $"https://httpstatuses.com/{bad.StatusCode}").ExecuteAsync(context);
            return true;
        }

        log.LogError(exception, "Unhandled request exception on {Method} {Path}", context.Request.Method, context.Request.Path);

        await TypedResults.Problem(
            title: "Internal server error",
            detail: options.Value.InternalErrorDetail?.Invoke(context),
            statusCode: StatusCodes.Status500InternalServerError,
            type: "https://httpstatuses.com/500").ExecuteAsync(context);
        return true;
    }
}
