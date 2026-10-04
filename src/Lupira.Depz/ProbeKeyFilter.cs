using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Lupira.Depz;

/// <summary>Gates /depz on the shared <c>X-Probe-Key</c>; a blank configured key rejects everything.</summary>
public sealed class ProbeKeyFilter(IOptions<DepzOptions> options) : IEndpointFilter
{
    public const string HeaderName = ProbeKey.HeaderName;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!ProbeKey.Matches(options.Value.ProbeKey, context.HttpContext.Request.Headers[HeaderName].ToString()))
            return TypedResults.Unauthorized();
        return await next(context);
    }
}
