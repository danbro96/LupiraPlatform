namespace Lupira.Bff.Auth;

/// <summary>The share-link session: a cookie holding the token the BFF replays upstream.</summary>
public sealed class LupiraGuestSessionOptions
{
    public string SchemeName { get; set; } = "Guest";

    public string PolicyName { get; set; } = "Guest";

    public required string CookieName { get; set; }

    public required string RequiredClaim { get; set; }

    /// <summary>Fixed, not sliding: the cookie must not outlive the share it was minted from.</summary>
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromHours(12);
}
