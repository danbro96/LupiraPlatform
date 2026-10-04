namespace Lupira.Contracts.PlaceRefs;

public sealed class PlaceReferencesResponse
{
    public required List<PlaceReferenceCountDto> Places { get; set; }
}
