using Microsoft.AspNetCore.Http;

namespace Lupira.Hosting.Observability;

internal static class SpanFilter
{
    public static Func<HttpContext, bool> For(IEnumerable<string> filteredPaths)
    {
        var paths = filteredPaths.Select(p => new PathString(p)).ToArray();
        return ctx => IsTraced(ctx.Request.Path, paths);
    }

    public static bool IsTraced(PathString path, IReadOnlyList<PathString> filteredPaths) =>
        !filteredPaths.Any(path.StartsWithSegments);
}
