namespace Lupira.Contracts.PlaceRefs;

public sealed class CheckPlaceReferencesRequest
{
    public required List<Guid> PlaceIds { get; set; }
}
