using System.Security.Claims;

namespace Lupira.Auth.DevUser;

/// <summary>Claims shaped like the JWT bearer's mapping (name = <c>email</c>, roles = <c>groups</c>), with a
/// stable <c>dev|&lt;email&gt;</c> subject so a dev caller resolves to the same principal every request.</summary>
public static class DevClaims
{
    public const string NameType = "email";
    public const string RoleType = "groups";

    public static ClaimsPrincipal Principal(string scheme, string email, IEnumerable<string> groups, IEnumerable<Claim>? extra = null)
    {
        var claims = new List<Claim> { new("sub", "dev|" + email), new("email", email) };
        claims.AddRange(groups.Select(g => new Claim(RoleType, g)));
        if (extra is not null)
            claims.AddRange(extra);
        return new ClaimsPrincipal(new ClaimsIdentity(claims, scheme, NameType, RoleType));
    }
}
