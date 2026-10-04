using Microsoft.Extensions.Configuration;

namespace Lupira.Bff.Proxy;

public static class DevUser
{
    public const string HeaderName = "X-Dev-User";

    public const string ConfigurationKey = "Dev:User";

    public const string DefaultName = "dev@localhost";

    public static string From(IConfiguration configuration) => configuration[ConfigurationKey] ?? DefaultName;
}
