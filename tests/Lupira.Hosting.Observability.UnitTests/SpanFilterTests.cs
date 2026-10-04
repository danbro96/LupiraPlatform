using Microsoft.AspNetCore.Http;
using Xunit;

namespace Lupira.Hosting.Observability.UnitTests;

public sealed class SpanFilterTests
{
    private static bool IsTraced(string path, params string[] extra)
    {
        var options = new LupiraTelemetryOptions();
        foreach (var p in extra)
            options.FilteredPaths.Add(p);
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        return SpanFilter.For(options.FilteredPaths)(ctx);
    }

    [Theory]
    [InlineData("/livez")]
    [InlineData("/readyz")]
    [InlineData("/pingz")]
    [InlineData("/depz")]
    [InlineData("/READYZ")]
    public void Probe_paths_are_not_traced(string path) => Assert.False(IsTraced(path));

    [Theory]
    [InlineData("/calendars")]
    [InlineData("/livezz")]
    [InlineData("/")]
    public void Other_paths_are_traced(string path) => Assert.True(IsTraced(path));

    [Fact]
    public void Extra_paths_drop_their_subtree()
    {
        Assert.False(IsTraced("/basemap/glyphs/0-255.pbf", "/basemap"));
        Assert.True(IsTraced("/basemaps", "/basemap"));
    }
}
