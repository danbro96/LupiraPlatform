using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Lupira.Auth.Service.AspNetCore;

public static class ServiceAuthenticationBuilderExtensions
{
    public static AuthenticationBuilder AddLupiraServiceAuth(this AuthenticationBuilder builder, ServiceAuthOptions options)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.TryAddScoped<CurrentService>();
        builder.AddScheme<AuthenticationSchemeOptions, ServiceAuthHandler>(ServiceAuthHandler.SchemeName, _ => { });

        if (options.IsConfigured)
        {
            builder.AddJwtBearer(ServiceAuthOptions.JwtSchemeName, o =>
            {
                o.Authority = options.Authority;
                o.Audience = options.Audience;
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                };
            });
        }

        return builder;
    }
}
