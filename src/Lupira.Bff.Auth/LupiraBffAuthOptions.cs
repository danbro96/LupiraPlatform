using System.Security.Claims;

namespace Lupira.Bff.Auth;

public sealed class LupiraBffAuthOptions
{
    public const string AdminPolicy = "Admin";

    public const string HostCookiePrefix = "__Host-";

    public string? Authority { get; set; }

    public string? BearerAuthority { get; set; }

    public string? Audience { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? CookieName { get; set; }

    /// <summary>Authentik groups that mark a caller as admin (reported by /auth/user).</summary>
    public IList<string> AdminGroups { get; set; } = [];

    public IList<string> Scopes { get; set; } = ["openid", "profile", "email", "groups", "offline_access"];

    public bool EnableOidc { get; set; }

    public bool EnableBearer { get; set; }

    public LupiraGuestSessionOptions? Guest { get; set; }

    public bool RequireAuthenticatedFallback { get; set; }

    public string? RequiredGroup { get; set; }

    public IList<string> DevGroups { get; set; } = [];

    public IReadOnlyList<string>? ApiPrefixes { get; set; }

    public bool IsAdmin(ClaimsPrincipal user) =>
        user.FindAll("groups").Select(c => c.Value).Intersect(AdminGroups, StringComparer.OrdinalIgnoreCase).Any();
}
