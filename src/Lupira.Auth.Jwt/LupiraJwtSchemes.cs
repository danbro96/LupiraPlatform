using Lupira.Auth.DevUser;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Hosting;

namespace Lupira.Auth.Jwt;

public static class LupiraJwtSchemes
{
    public static string[] Api(IHostEnvironment environment, string devScheme = DevAuthenticationBuilderExtensions.DefaultScheme) =>
        environment.IsDevelopment()
            ? [JwtBearerDefaults.AuthenticationScheme, devScheme]
            : [JwtBearerDefaults.AuthenticationScheme];
}
