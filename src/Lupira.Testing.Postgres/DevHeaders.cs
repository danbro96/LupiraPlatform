using System.Net.Http.Headers;

namespace Lupira.Testing.Postgres;

internal static class DevHeaders
{
    public const string User = "X-Dev-User";
    public const string Groups = "X-Dev-Groups";
    public const string Scopes = "X-Dev-Scopes";
    public const string Service = "X-Dev-Service";
    public const string DeviceKeyScheme = "DeviceKey";

    public static HttpClient AsUser(this HttpClient client, string email, IReadOnlyCollection<string> groups)
    {
        client.DefaultRequestHeaders.Add(User, email);
        if (groups.Count > 0) client.DefaultRequestHeaders.Add(Groups, string.Join(',', groups));
        return client;
    }

    public static HttpClient WithScopes(this HttpClient client, IReadOnlyCollection<string> scopes)
    {
        if (scopes.Count > 0) client.DefaultRequestHeaders.Add(Scopes, string.Join(' ', scopes));
        return client;
    }

    public static HttpClient AsService(this HttpClient client, string serviceId)
    {
        client.DefaultRequestHeaders.Add(Service, serviceId);
        return client;
    }

    public static HttpClient WithDeviceKey(this HttpClient client, string apiKey)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(DeviceKeyScheme, apiKey);
        return client;
    }
}
