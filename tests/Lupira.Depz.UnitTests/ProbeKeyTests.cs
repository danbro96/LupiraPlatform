using Xunit;

namespace Lupira.Depz.UnitTests;

public class ProbeKeyTests
{
    [Theory]
    [InlineData("secret", "secret", true)]
    [InlineData("secret", "Secret", false)]
    [InlineData("secret", "secret2", false)]
    [InlineData("secret", "", false)]
    [InlineData("secret", null, false)]
    [InlineData("", "", false)]
    [InlineData(null, null, false)]
    public void Matches_only_a_configured_key(string? configured, string? presented, bool expected) =>
        Assert.Equal(expected, ProbeKey.Matches(configured, presented));
}
