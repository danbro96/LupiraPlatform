namespace Lupira.Contracts.PlaceRefs;

public sealed class PlaceReferenceCountDto
{
    public required Guid PlaceId { get; set; }

    public required int LiveCount { get; set; }

    public required int DeletedCount { get; set; }
}
