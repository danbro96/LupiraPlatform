using System.Net.Http.Headers;

namespace Lupira.Clients.ServiceTokens;

/// <summary>Service auth for one outbound hop: a cached client-credentials bearer when the hop has credentials, else
/// the Development dev headers (<c>X-Dev-Service</c>, or <c>X-Dev-User</c> + <c>X-Dev-Scopes</c>), else nothing.</summary>
public sealed class ServiceTokenProvider(TokenEndpointClient tokens, TokenCache cache)
{
    /// <summary>Per-request <c>X-Dev-User</c> that wins over the hop's configured one (e.g. the acting member).</summary>
    public static readonly HttpRequestOptionsKey<string> DevUserOverride = new("Lupira.ServiceTokens.DevUser");

    private const string DevUserHeader = "X-Dev-User";
    private const string DevServiceHeader = "X-Dev-Service";
    private const string DevScopesHeader = "X-Dev-Scopes";

    public Task<string> GetTokenAsync(IConfidentialClient client, CancellationToken ct) =>
        cache.GetOrMintAsync(TokenCache.ClientCredentialsKey(client), token => tokens.ClientCredentialsAsync(client, token), null, ct);

    /// <summary>Authenticates <paramref name="request"/> for the hop; throws <see cref="TokenEndpointException"/> when the mint fails.</summary>
    public async Task ApplyAsync(HttpRequestMessage request, IOutboundHopOptions hop, CancellationToken ct)
    {
        if (hop.HasCredentials)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(hop, ct));
            return;
        }

        var devUser = request.Options.TryGetValue(DevUserOverride, out var acting) && !string.IsNullOrWhiteSpace(acting) ? acting : null;
        if (devUser is null && !string.IsNullOrWhiteSpace(hop.DevServiceId))
        {
            request.Headers.TryAddWithoutValidation(DevServiceHeader, hop.DevServiceId);
            return;
        }

        devUser ??= hop.DevUser;
        if (string.IsNullOrWhiteSpace(devUser)) return;
        request.Headers.TryAddWithoutValidation(DevUserHeader, devUser);
        if (!string.IsNullOrWhiteSpace(hop.DevScopes)) request.Headers.TryAddWithoutValidation(DevScopesHeader, hop.DevScopes);
    }
}
