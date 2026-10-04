using System.Text.Json;

namespace Lupira.Depz;

/// <summary>Mirrors the real clients' auth: creds → client-credentials bearer, DevUser → X-Dev-User, else
/// anonymous. The token is cached per instance until 30 s before expiry.</summary>
public sealed class ClientCredentialsProbeCredential : IProbeCredential
{
    private (string Token, DateTimeOffset ExpiresAt)? _cached;

    public string? TokenUrl { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? Scope { get; set; }

    public string? DevUser { get; set; }

    public async Task ApplyAsync(HttpRequestMessage request, HttpClient client, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(TokenUrl) && !string.IsNullOrWhiteSpace(ClientId)
            && !string.IsNullOrWhiteSpace(ClientSecret))
        {
            string token;
            try
            {
                token = await MintAsync(client, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"token mint failed: {ex.Message}", ex);
            }

            request.Headers.Authorization = new("Bearer", token);
        }
        else if (!string.IsNullOrWhiteSpace(DevUser))
        {
            request.Headers.TryAddWithoutValidation("X-Dev-User", DevUser);
        }
    }

    private async Task<string> MintAsync(HttpClient client, CancellationToken ct)
    {
        if (_cached is { } cached && DateTimeOffset.UtcNow < cached.ExpiresAt)
            return cached.Token;

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = ClientId!,
            ["client_secret"] = ClientSecret!,
        };
        // The scope pulls in the audience mapping; binding it on the provider alone is not enough.
        if (!string.IsNullOrWhiteSpace(Scope)) form["scope"] = Scope!;

        using var response = await client.PostAsync(TokenUrl, new FormUrlEncodedContent(form), ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        // The IdP's error body (invalid_client / invalid_scope / unauthorized_client) is the whole
        // diagnostic — a bare status code says nothing about which side is misconfigured.
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"HTTP {(int) response.StatusCode}: {Truncate(raw)}");
        using var payload = JsonDocument.Parse(raw);
        var token = payload.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("token response had no access_token");
        var expiresIn = payload.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 300;
        _cached = (token, DateTimeOffset.UtcNow.AddSeconds(expiresIn - 30));
        return token;
    }

    private static string Truncate(string s) => s.Length <= 300 ? s : s[..300];
}
