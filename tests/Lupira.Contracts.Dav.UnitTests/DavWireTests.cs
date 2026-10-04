using System.Text.Json;
using Xunit;

namespace Lupira.Contracts.Dav.UnitTests;

public sealed class DavWireTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Collections_round_trip_with_the_kind_by_name()
    {
        var dto = new DavCollectionsDto
        {
            Principal = new DavPrincipalDto { DisplayName = "Owner" },
            Collections =
            [
                new DavCollectionDto { Id = Guid.Empty, Kind = DavCollectionKind.TodoList, Ctag = "c1", SyncToken = "s1" },
            ],
        };

        var json = JsonSerializer.Serialize(dto, Web);

        Assert.Contains("\"kind\":\"TodoList\"", json, StringComparison.Ordinal);
        Assert.Equal(DavCollectionKind.TodoList, JsonSerializer.Deserialize<DavCollectionsDto>(json, Web)!.Collections[0].Kind);
    }
}
