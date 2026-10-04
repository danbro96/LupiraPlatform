using System.Text;

namespace Lupira.Depz;

/// <summary>A fixed header mirroring the real client's auth (Bearer, X-API-Key, Basic) or identity (User-Agent).</summary>
public sealed class StaticHeaderProbeCredential(string headerName, string value) : IProbeCredential
{
    public string HeaderName { get; } = headerName;

    public string Value { get; } = value;

    public static StaticHeaderProbeCredential Bearer(string token) => new("Authorization", $"Bearer {token}");

    public static StaticHeaderProbeCredential ApiKey(string key) => new("X-API-Key", key);

    public static StaticHeaderProbeCredential Basic(string user, string? password) =>
        new("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}"))}");

    public Task ApplyAsync(HttpRequestMessage request, HttpClient client, CancellationToken ct)
    {
        request.Headers.TryAddWithoutValidation(HeaderName, Value);
        return Task.CompletedTask;
    }
}
