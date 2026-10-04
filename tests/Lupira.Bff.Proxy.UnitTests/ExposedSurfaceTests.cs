using Microsoft.AspNetCore.Builder;
using Xunit;

namespace Lupira.Bff.Proxy.UnitTests;

public class ExposedSurfaceTests
{
    [Fact]
    public void Api_prefixes_cover_every_mounted_upstream_and_the_device_path()
    {
        var surface = Fixture.Surface("cal");

        Assert.Equal(
            ["/api", "/contact-api", "/geo-api", "/tasks-api", "/location-api", "/photo-api", "/comms-api", "/ingest"],
            surface.ApiPrefixes);
        Assert.Equal(["/ingest"], surface.DeviceKeyPrefixes);
    }

    [Fact]
    public void Only_documented_groups_reach_the_contract()
    {
        var surface = Fixture.Surface("cal");

        Assert.All(surface.Documented, o => Assert.Equal(ExposedGroup.DefaultName, o.Group.Name));
        Assert.Equal(surface.Operations.Count(o => o.Group.Name == ExposedGroup.DefaultName), surface.Documented.Count());
    }

    [Fact]
    public void Path_map_remounts_the_entry_and_keeps_the_upstream_path()
    {
        var operation = Fixture.Surface("tasks").Operations.Single(o => o.UpstreamEntry == "PATCH /shared/{token}/items/{itemId}");

        Assert.Equal("/share/items/{itemId}", operation.MappedPath);
        Assert.Equal("/api/share/items/{itemId}", operation.BffPath);
        Assert.True(operation.Group.Documented);
    }

    [Fact]
    public void Loads_the_embedded_file_of_the_application_assembly()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(ExposedSurfaceTests).Assembly.GetName().Name,
        });

        var surface = ExposedSurface.Load(builder.Environment);

        Assert.Contains(surface.Operations, o => o.Cluster == "assistant-api");
    }

    [Theory]
    [InlineData("""{ "guest": { "tasks-api": ["GET /x"] } }""", "not declared")]
    [InlineData("""{ "operations": { "tasks-api": ["GET /x"] } }""", "No BFF prefix")]
    [InlineData("""{ "clusters": { "a": { "prefix": "/a" } }, "operations": { "a": ["GET /files/{**path}"] } }""", "catch-all")]
    [InlineData("""{ "clusters": { "a": { "prefix": "/a" } }, "groups": { "s": { "catchAll": true } }, "s": { "a": ["POST /files/{**path}"] } }""", "GET-only")]
    [InlineData("""{ "clusters": { "a": { "prefix": "/a" } }, "groups": { "s": { "catchAll": true } }, "s": { "a": ["GET /files"] } }""", "must end in")]
    [InlineData("""{ "clusters": { "a": { "prefix": "/a" } }, "groups": { "s": { "catchAll": true, "documented": true } }, "s": { "a": ["GET /f/{**p}"] } }""", "cannot be documented")]
    [InlineData("""{ "clusters": { "a": { "prefix": "/a" } }, "groups": { "g": { "catchall": true } }, "g": { "a": ["GET /x"] } }""", "Unknown property")]
    [InlineData("""{ "clusters": { "a": { "prefix": "/a" } }, "groups": { "g": { "credential": "cookie" } }, "g": { "a": ["GET /x"] } }""", "credential")]
    [InlineData("""{ "clusters": { "a": { "prefix": "/a" } }, "groups": { "g": {} } }""", "lists no entries")]
    [InlineData("""{ "clusters": { "a": { "prefix": "/a" } }, "operations": { "a": ["GET /x", "GET /x"] } }""", "twice")]
    [InlineData("""{ "clusters": { "a": { "prefix": "/a" } }, "operations": { "a": ["get /x"] } }""", "VERB /path")]
    [InlineData("""{ "clusters": { "a": { "prefix": "/a" } }, "operations": { "a": ["GET x"] } }""", "start with '/'")]
    [InlineData("""{ "clusters": { "a": { "prefix": "/a" } }, "groups": { "g": { "pathMap": { "upstream": "/s/{t}", "bff": "/s", "claims": {} } } }, "g": { "a": ["GET /s/{t}"] } }""", "no claim")]
    [InlineData("""{ "clusters": { "a": { "prefix": "/a" } }, "groups": { "g": { "pathMap": { "upstream": "/s/{t}", "bff": "/s", "claims": { "t": "c" } } } }, "g": { "a": ["GET /other"] } }""", "outside the group's path map")]
    public void A_malformed_file_fails_loudly(string json, string message)
    {
        var error = Assert.Throws<InvalidOperationException>(() => ExposedSurface.Parse(json));
        Assert.Contains(message, error.Message, StringComparison.Ordinal);
    }
}
