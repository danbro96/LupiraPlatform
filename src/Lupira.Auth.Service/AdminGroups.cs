namespace Lupira.Auth.Service;

/// <summary>Authentik groups that grant admin rights in one app: <c>{app}-admins</c> plus <c>platform-admins</c>.</summary>
public static class AdminGroups
{
    public const string Platform = "platform-admins";

    public static string[] For(string app) => [$"{app}-admins", Platform];

    public static bool Grants(IEnumerable<string> adminGroups, IEnumerable<string> groups) =>
        groups.Any(g => adminGroups.Contains(g, StringComparer.OrdinalIgnoreCase));
}
