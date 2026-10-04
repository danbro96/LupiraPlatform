namespace Lupira.Auth.Jwt;

/// <summary>`dotnet build` regenerates openapi/ via getdocument, which boots Program with no real config.</summary>
internal static class OpenApiBuild
{
    public static bool IsRunning(IEnumerable<string> commandLineArgs) =>
        commandLineArgs.Any(a => a.Contains("getdocument", StringComparison.OrdinalIgnoreCase));
}
