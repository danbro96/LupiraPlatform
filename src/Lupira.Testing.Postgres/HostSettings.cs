namespace Lupira.Testing.Postgres;

internal static class HostSettings
{
    public const string AuthorityKey = "Auth:Oidc:Authority";

    public static string Authority(string authentikSlug) => $"https://auth.test/application/o/{authentikSlug}/";

    public static Dictionary<string, string?> Compose(string connectionStringName, string connectionString, string? authentikSlug)
    {
        var settings = new Dictionary<string, string?> { [$"ConnectionStrings:{connectionStringName}"] = connectionString };
        if (authentikSlug is not null) settings[AuthorityKey] = Authority(authentikSlug);
        return settings;
    }
}
