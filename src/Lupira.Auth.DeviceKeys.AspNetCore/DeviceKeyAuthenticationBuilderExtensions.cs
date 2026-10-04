using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Lupira.Auth.DeviceKeys.AspNetCore;

public static class DeviceKeyAuthenticationBuilderExtensions
{
    public static AuthenticationBuilder AddLupiraDeviceKeys<TStore>(this AuthenticationBuilder builder)
        where TStore : class, IDeviceKeyStore
    {
        builder.Services.AddScoped<IDeviceKeyStore, TStore>();
        return builder.AddScheme<AuthenticationSchemeOptions, DeviceKeyAuthHandler>(DeviceKeyAuthHandler.SchemeName, null);
    }
}
