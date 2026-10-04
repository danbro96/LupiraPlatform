using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Lupira.Hosting.Problems;

public static class ProblemsServiceCollectionExtensions
{
    /// <summary>ProblemDetails stamped with <c>traceId</c> plus <see cref="ProblemExceptionHandler"/>; pair with
    /// <c>app.UseExceptionHandler()</c>.</summary>
    public static IServiceCollection AddLupiraProblems(this IServiceCollection services, Action<ProblemExceptionOptions>? configure = null)
    {
        services.AddOptions<ProblemExceptionOptions>().Configure(o => configure?.Invoke(o));
        services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
            ctx.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier);
        services.AddExceptionHandler<ProblemExceptionHandler>();
        return services;
    }
}
