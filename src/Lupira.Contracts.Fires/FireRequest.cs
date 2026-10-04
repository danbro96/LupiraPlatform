namespace Lupira.Contracts.Fires;

/// <summary>The <c>POST /fires</c> wire body: a due fire with its scheduled-fire context, the calendar classification
/// Policy needs, and exactly one of <see cref="Prompt"/>/<see cref="Action"/>.</summary>
public sealed class FireRequest
{
    public required string PrincipalId { get; set; }

    public required Guid ItemId { get; set; }

    public required Guid CalendarId { get; set; }

    public required CalendarClass CalendarClass { get; set; }

    public required CalendarKind CalendarKind { get; set; }

    public required DateTimeOffset OccurrenceAt { get; set; }

    public required string DedupeKey { get; set; }

    public DateTimeOffset? ExpireAfter { get; set; }

    public ItemPrompt? Prompt { get; set; }

    public ItemAction? Action { get; set; }
}
