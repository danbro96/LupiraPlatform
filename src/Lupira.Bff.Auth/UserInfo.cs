namespace Lupira.Bff.Auth;

public sealed class UserInfo
{
    public required string Email { get; set; }

    public string? Name { get; set; }

    public required IReadOnlyList<string> Groups { get; set; }

    public required bool IsAdmin { get; set; }
}
