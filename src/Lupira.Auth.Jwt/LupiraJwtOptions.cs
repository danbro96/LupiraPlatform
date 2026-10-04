using Lupira.Auth.DevUser;

namespace Lupira.Auth.Jwt;

public sealed class LupiraJwtOptions
{
    public const string DevOrJwtScheme = "DevOrJwt";

    public string SectionName { get; set; } = OidcAuthOptions.SectionName;

    public JwtValidationProfile Validation { get; set; } = JwtValidationProfile.Default;

    public bool RelaxHttpsMetadataInDevelopment { get; set; } = true;

    public bool McpChallenge { get; set; } = true;

    public OidcConfigRequirement RequireConfig { get; set; } = OidcConfigRequirement.OutsideDevelopment;

    public bool SkipBearerWhenUnconfigured { get; set; }

    public string? DevScheme { get; set; } = DevAuthenticationBuilderExtensions.DefaultScheme;

    public bool RegisterDevScheme { get; set; } = true;

    public bool DevOrJwtDefault { get; set; }
}
