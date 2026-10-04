using System.Text.Json;

namespace Lupira.Clients.ServiceTokens;

/// <summary>Authentik's token endpoint for every outbound grant: RFC 8693 exchange of a member bearer, client
/// credentials for calls with no member behind them, and refresh of a stored offline grant. Singleton over a named client.</summary>
public sealed class TokenEndpointClient(IHttpClientFactory httpFactory)
{
    public const string HttpClientName = "oauth-token";

    private const string TokenExchangeGrant = "urn:ietf:params:oauth:grant-type:token-exchange";
    private const string AccessTokenType = "urn:ietf:params:oauth:token-type:access_token";
    private const int MaxDescriptionLength = 300;

    public Task<IssuedToken> ExchangeAsync(IConfidentialClient client, string subjectToken, string audience, CancellationToken ct)
    {
        var form = Credentials(client, TokenExchangeGrant);
        form["subject_token"] = subjectToken;
        form["subject_token_type"] = AccessTokenType;
        form["audience"] = audience;
        if (!string.IsNullOrWhiteSpace(client.Scope)) form["scope"] = client.Scope!;
        return PostAsync(client.TokenUrl!, form, ct);
    }

    public Task<IssuedToken> ClientCredentialsAsync(IConfidentialClient client, CancellationToken ct)
    {
        var form = Credentials(client, "client_credentials");
        // The scope pulls in the audience mapping; binding it on the provider alone is not enough.
        if (!string.IsNullOrWhiteSpace(client.Scope)) form["scope"] = client.Scope!;
        return PostAsync(client.TokenUrl!, form, ct);
    }

    /// <summary>Refreshes a stored grant for the client's own audience. Authentik rotates the refresh token.</summary>
    public Task<IssuedToken> RefreshAsync(IConfidentialClient client, string refreshToken, CancellationToken ct)
    {
        var form = Credentials(client, "refresh_token");
        form["refresh_token"] = refreshToken;
        return PostAsync(client.TokenUrl!, form, ct);
    }

    private static Dictionary<string, string> Credentials(IConfidentialClient client, string grantType) => new()
    {
        ["grant_type"] = grantType,
        ["client_id"] = client.ClientId!,
        ["client_secret"] = client.ClientSecret!,
    };

    private async Task<IssuedToken> PostAsync(string tokenUrl, Dictionary<string, string> form, CancellationToken ct)
    {
        HttpResponseMessage resp;
        try
        {
            resp = await httpFactory.CreateClient(HttpClientName).PostAsync(tokenUrl, new FormUrlEncodedContent(form), ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            throw new TokenEndpointException(TokenErrorKind.Unavailable, null, ex.Message);
        }

        using (resp)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            using var doc = ParseBody(body, resp);
            var root = doc.RootElement;
            if (!resp.IsSuccessStatusCode)
            {
                var error = Text(root, "error");
                // The IdP's error body is the whole diagnostic — a bare status says nothing about which side is misconfigured.
                throw new TokenEndpointException(
                    TokenErrorKindWire.Parse(error), (int) resp.StatusCode, Text(root, "error_description") ?? (error is null ? Truncate(body) : null));
            }

            var accessToken = Text(root, "access_token");
            if (string.IsNullOrEmpty(accessToken))
                throw new TokenEndpointException(TokenErrorKind.Other, (int) resp.StatusCode, "response had no access_token");
            var expiresIn = root.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var s) ? s : 300;
            return new IssuedToken(accessToken, TimeSpan.FromSeconds(expiresIn), Text(root, "refresh_token"));
        }
    }

    private static JsonDocument ParseBody(string body, HttpResponseMessage resp)
    {
        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            throw new TokenEndpointException(
                resp.IsSuccessStatusCode ? TokenErrorKind.Other : TokenErrorKind.Unavailable, (int) resp.StatusCode, $"non-JSON response: {Truncate(body)}");
        }
    }

    private static string? Text(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string Truncate(string s) => s.Length <= MaxDescriptionLength ? s : s[..MaxDescriptionLength];
}
