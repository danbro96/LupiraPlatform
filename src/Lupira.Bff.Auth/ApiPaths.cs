using Microsoft.AspNetCore.Http;

namespace Lupira.Bff.Auth;

internal sealed class ApiPaths(IReadOnlyList<string> prefixes)
{
    public bool Contains(PathString path) => prefixes.Any(p => path.StartsWithSegments(p));
}
