namespace Lupira.Sync;

/// <summary>One page of a <c>/sync/*</c> feed. <c>reset</c> starts a full sync: rows the full sync never mentions are gone.</summary>
public sealed class SyncPage<T>
{
    public required string Cursor { get; set; }

    public required bool HasMore { get; set; }

    public required bool Reset { get; set; }

    public required List<T> Changed { get; set; }

    public required List<Guid> Deleted { get; set; }
}
