namespace Lupira.Auth.DevUser.UnitTests;

public sealed class Who
{
    public required string Outcome { get; set; }

    public string? Name { get; set; }

    public string? AuthenticationType { get; set; }

    public required string[] Claims { get; set; }

    public required string[] Groups { get; set; }
}
