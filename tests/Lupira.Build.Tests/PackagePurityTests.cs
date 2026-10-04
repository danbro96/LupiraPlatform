using System.Xml.Linq;
using Xunit;

namespace Lupira.Build.Tests;

public sealed class PackagePurityTests
{
    private static readonly string[] PurePrefixes = ["Lupira.Primitives", "Lupira.Results", "Lupira.Contracts."];

    public static TheoryData<string> PureProjects() => new(PureProjectPaths());

    [Fact]
    public void The_pure_packages_are_found() =>
        Assert.Contains(PureProjectPaths(), p => p.EndsWith("Lupira.Results.csproj", StringComparison.Ordinal));

    [Theory]
    [MemberData(nameof(PureProjects))]
    public void Has_no_framework_or_package_references(string project)
    {
        var references = XDocument.Load(Path.Combine(RepoRoot(), project)).Descendants()
            .Where(e => e.Name.LocalName is "FrameworkReference" or "PackageReference")
            .Select(e => $"{e.Name.LocalName} {e.Attribute("Include")?.Value}");

        Assert.Empty(references);
    }

    private static IEnumerable<string> PureProjectPaths() =>
        Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(p => PurePrefixes.Any(Path.GetFileNameWithoutExtension(p).StartsWith))
            .Select(p => Path.GetRelativePath(RepoRoot(), p));

    private static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "LupiraPlatform.slnx")))
                return dir.FullName;
        }

        throw new InvalidOperationException("LupiraPlatform.slnx not found above the test output.");
    }
}
