namespace Lupira.Clients.ServiceTokens;

/// <summary>A minted access token. <see cref="RotatedRefreshToken"/> is set when Authentik rotated the refresh token;
/// an exchange never returns one.</summary>
public sealed record IssuedToken(string AccessToken, TimeSpan ExpiresIn, string? RotatedRefreshToken = null);
