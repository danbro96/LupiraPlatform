using Microsoft.AspNetCore.Http;

namespace Lupira.Hosting.Problems;

/// <summary>A <see cref="ProblemExceptionOptions.BadRequestDetail"/> hook for endpoints that take an <c>Idempotency-Key</c>.</summary>
public static class IdempotencyKeyProblem
{
    // Endpoints bind the header as `Guid? idempotencyKey`; the framework message names that parameter.
    public static string? Detail(HttpContext context, BadHttpRequestException bad) =>
        context.Request.Headers.TryGetValue("Idempotency-Key", out var key) && !Guid.TryParse(key.ToString(), out _)
        && bad.Message.Contains("idempotencyKey", StringComparison.Ordinal)
            ? "Idempotency-Key must be a GUID."
            : null;
}
