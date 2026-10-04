using Microsoft.AspNetCore.Authorization;

namespace Lupira.Auth.Service.AspNetCore;

public static class ServiceAuthorizationOptionsExtensions
{
    public static AuthorizationOptions AddLupiraServicePolicy(this AuthorizationOptions authorization, ServiceAuthOptions options)
    {
        authorization.AddPolicy(ServiceAuthHandler.SchemeName, p => p
            .AddAuthenticationSchemes(options.AuthenticationSchemes)
            .RequireAuthenticatedUser());
        return authorization;
    }
}
