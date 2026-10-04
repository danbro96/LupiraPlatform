namespace Lupira.Testing.Postgres.IntegrationTests;

public sealed class SettingsProbe
{
    public required string? EagerAuthority { get; set; }

    public required string? Authority { get; set; }

    public required string? Extra { get; set; }

    public required string Environment { get; set; }
}
