namespace Lupira.Sync.Marten;

/// <summary>Streams changed since a sequence, ordered by their latest event. <see cref="NextSequence"/> resumes after
/// the last returned stream.</summary>
public sealed class ChangedStreams
{
    public required List<Guid> Ids { get; set; }

    public required long NextSequence { get; set; }

    public required bool HasMore { get; set; }
}
