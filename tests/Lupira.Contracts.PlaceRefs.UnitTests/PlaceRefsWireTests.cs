using System.Text.Json;
using Xunit;

namespace Lupira.Contracts.PlaceRefs.UnitTests;

public sealed class PlaceRefsWireTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Request_reads_place_ids()
    {
        var body = JsonSerializer.Deserialize<CheckPlaceReferencesRequest>("""{"placeIds":["44444444-4444-4444-4444-444444444444"]}""", Web)!;

        Assert.Equal([Guid.Parse("44444444-4444-4444-4444-444444444444")], body.PlaceIds);
    }

    [Fact]
    public void Response_carries_live_and_deleted_counts()
    {
        var response = new PlaceReferencesResponse
        {
            Places = [new PlaceReferenceCountDto { PlaceId = Guid.Parse("44444444-4444-4444-4444-444444444444"), LiveCount = 2, DeletedCount = 1 }],
        };

        Assert.Equal(
            """{"places":[{"placeId":"44444444-4444-4444-4444-444444444444","liveCount":2,"deletedCount":1}]}""",
            JsonSerializer.Serialize(response, Web));
    }

    [Fact]
    public void Response_without_deleted_count_is_rejected()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PlaceReferencesResponse>(
            """{"places":[{"placeId":"44444444-4444-4444-4444-444444444444","liveCount":2}]}""", Web));
    }
}
