using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Lupira.Hosting.Defaults.UnitTests;

public sealed class JsonDefaultsTests
{
    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    [Fact]
    public async Task Enums_are_written_as_names_and_offsets_keep_theirs()
    {
        await using var app = await DefaultsTestHost.StartAsync();

        var body = await app.GetTestClient().GetFromJsonAsync<JsonElement>("/sample");

        Assert.Equal("Friday", body.GetProperty("day").GetString());
        Assert.Equal("2026-10-04T12:00:00+02:00", body.GetProperty("at").GetString());
    }

    [Fact]
    public async Task Utc_offsets_are_opt_in()
    {
        await using var app = await DefaultsTestHost.StartAsync(o => o.UtcDateTimeOffsets = true);

        var body = await app.GetTestClient().GetFromJsonAsync<JsonElement>("/sample");

        Assert.Equal("2026-10-04T10:00:00Z", body.GetProperty("at").GetString());
    }

    [Fact]
    public async Task Quoted_numbers_are_rejected_by_default()
    {
        await using var app = await DefaultsTestHost.StartAsync();

        var res = await app.GetTestClient().PostAsync("/samples", Json("""{"name":"x","count":"5"}"""));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Quoted_numbers_bind_when_strict_numbers_is_off()
    {
        await using var app = await DefaultsTestHost.StartAsync(o => o.StrictNumbers = false);

        var res = await app.GetTestClient().PostAsync("/samples", Json("""{"name":"x","count":"5"}"""));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Property_names_are_case_sensitive_by_default()
    {
        await using var app = await DefaultsTestHost.StartAsync();

        var res = await app.GetTestClient().PostAsync("/samples", Json("""{"NAME":"x"}"""));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Case_insensitive_properties_bind()
    {
        await using var app = await DefaultsTestHost.StartAsync(o => o.CaseInsensitiveProperties = true);

        var res = await app.GetTestClient().PostAsync("/samples", Json("""{"NAME":"x","Day":"Monday"}"""));
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("x", body.GetProperty("name").GetString());
        Assert.Equal("Monday", body.GetProperty("day").GetString());
    }
}
