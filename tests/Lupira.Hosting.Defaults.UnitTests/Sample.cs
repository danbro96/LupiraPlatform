namespace Lupira.Hosting.Defaults.UnitTests;

public sealed class Sample
{
    public required string Name { get; set; }

    public int Count { get; set; }

    public DayOfWeek Day { get; set; }

    public DateTimeOffset At { get; set; }
}
