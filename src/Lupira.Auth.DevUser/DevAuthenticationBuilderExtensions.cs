using Microsoft.AspNetCore.Authentication;

namespace Lupira.Auth.DevUser;

public static class DevAuthenticationBuilderExtensions
{
    public const string DefaultScheme = "Dev";

    /// <summary><see cref="DevHeaderAuthHandler"/> for APIs; add it only in Development.</summary>
    public static AuthenticationBuilder AddLupiraDevHeaderAuth(this AuthenticationBuilder builder, string scheme = DefaultScheme) =>
        builder.AddScheme<AuthenticationSchemeOptions, DevHeaderAuthHandler>(scheme, null);

    /// <summary><see cref="DevConfigUserAuthHandler"/> for BFFs; add it only outside Production.</summary>
    public static AuthenticationBuilder AddLupiraDevConfigUserAuth(
        this AuthenticationBuilder builder, string scheme = DefaultScheme, Action<DevConfigUserOptions>? configure = null) =>
        builder.AddScheme<DevConfigUserOptions, DevConfigUserAuthHandler>(scheme, configure);
}
