using Xunit;

namespace Lupira.Bff.Auth.UnitTests;

public sealed class StaleCallbackTests
{
    [Theory]
    [InlineData("Correlation failed.")]
    [InlineData("Unable to unprotect the message.State.")]
    [InlineData("OpenIdConnectAuthenticationHandler: message.State is null or empty.")]
    public void A_lost_or_unreadable_state_is_stale(string message) =>
        Assert.True(StaleCallback.Is(new Exception(message)));

    [Fact]
    public void A_provider_error_is_not_stale() =>
        Assert.False(StaleCallback.Is(new Exception("Message contains error: 'access_denied', error_description: 'error_description is null', error_uri: 'error_uri is null'.")));
}
