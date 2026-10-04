namespace Lupira.Hosting.OpenApi.UnitTests;

public sealed class WidgetDto
{
    public required Guid Id { get; set; }

    public required WidgetStatus Status { get; set; }

    public required WidgetStatus? PreviousStatus { get; set; }

    public required DateTimeOffset UpdatedAt { get; set; }

    public required DateTimeOffset? RetiredAt { get; set; }
}
