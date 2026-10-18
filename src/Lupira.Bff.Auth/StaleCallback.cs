namespace Lupira.Bff.Auth;

/// <summary>Failures of a sign-in callback that a fresh challenge can fix: a stale tab or a replayed form post.</summary>
internal static class StaleCallback
{
    public static bool Is(Exception failure) => failure.Message is
        "Correlation failed."
        or "Unable to unprotect the message.State."
        or "OpenIdConnectAuthenticationHandler: message.State is null or empty.";
}
