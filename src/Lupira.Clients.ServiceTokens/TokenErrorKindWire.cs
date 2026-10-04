namespace Lupira.Clients.ServiceTokens;

/// <summary>Maps <see cref="TokenErrorKind"/> to and from the token endpoint's <c>error</c> codes.</summary>
public static class TokenErrorKindWire
{
    public static TokenErrorKind Parse(string? error) => error switch
    {
        "invalid_request" => TokenErrorKind.InvalidRequest,
        "invalid_client" => TokenErrorKind.InvalidClient,
        "invalid_grant" => TokenErrorKind.InvalidGrant,
        "invalid_scope" => TokenErrorKind.InvalidScope,
        "invalid_target" => TokenErrorKind.InvalidTarget,
        "access_denied" => TokenErrorKind.AccessDenied,
        _ => TokenErrorKind.Other,
    };

    public static string? ToWire(this TokenErrorKind kind) => kind switch
    {
        TokenErrorKind.InvalidRequest => "invalid_request",
        TokenErrorKind.InvalidClient => "invalid_client",
        TokenErrorKind.InvalidGrant => "invalid_grant",
        TokenErrorKind.InvalidScope => "invalid_scope",
        TokenErrorKind.InvalidTarget => "invalid_target",
        TokenErrorKind.AccessDenied => "access_denied",
        _ => null,
    };
}
