namespace Lupira.Contracts.Dav;

public sealed class DavChangeDto
{
    public required string Uid { get; set; }

    public required string Etag { get; set; }
}
