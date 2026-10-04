using Lupira.Auth.DevUser;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Lupira.Auth.Jwt.UnitTests;

/// <summary>The adoption call each consumer flavour makes in place of <see cref="TodaySetups"/>.</summary>
internal static class PackageSetups
{
    public static void Simple(WebApplicationBuilder builder, bool isOpenApiBuild) =>
        builder.AddLupiraJwt(null, Args(isOpenApiBuild));

    public static void Tasks(WebApplicationBuilder builder, bool isOpenApiBuild) =>
        builder.AddLupiraJwt(Strict, Args(isOpenApiBuild));

    public static void Assistant(WebApplicationBuilder builder, bool isOpenApiBuild)
    {
        var auth = builder.AddLupiraJwt(
            o =>
            {
                Strict(o);
                o.McpChallenge = false;
                o.DevScheme = TodaySetups.AssistantDevScheme;
                o.RegisterDevScheme = false;
            },
            Args(isOpenApiBuild));
        if (builder.Environment.IsDevelopment())
            auth.AddScheme<AuthenticationSchemeOptions, DevHeaderAuthHandler>(TodaySetups.AssistantDevScheme, _ => { });
    }

    public static void Comms(WebApplicationBuilder builder, bool isOpenApiBuild) =>
        builder.AddLupiraJwt(
            o =>
            {
                o.Validation = JwtValidationProfile.Strict;
                o.RelaxHttpsMetadataInDevelopment = false;
                o.DevOrJwtDefault = true;
                o.SkipBearerWhenUnconfigured = true;
            },
            Args(isOpenApiBuild));

    public static void Mtg(WebApplicationBuilder builder, bool isOpenApiBuild) =>
        builder.AddLupiraJwt(
            o =>
            {
                o.SectionName = "Auth";
                o.Validation = JwtValidationProfile.RawClaims;
                o.RelaxHttpsMetadataInDevelopment = false;
                o.McpChallenge = false;
                o.RequireConfig = OidcConfigRequirement.None;
                o.DevScheme = null;
            },
            Args(isOpenApiBuild));

    private static void Strict(LupiraJwtOptions o)
    {
        o.Validation = JwtValidationProfile.Strict;
        o.RelaxHttpsMetadataInDevelopment = false;
        o.RequireConfig = OidcConfigRequirement.Always;
        o.DevOrJwtDefault = true;
    }

    private static string[] Args(bool isOpenApiBuild) => isOpenApiBuild ? ["dotnet-getdocument.dll"] : [];
}
