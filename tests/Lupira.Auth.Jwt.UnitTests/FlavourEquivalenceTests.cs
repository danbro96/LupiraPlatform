using Microsoft.AspNetCore.Builder;
using Xunit;

namespace Lupira.Auth.Jwt.UnitTests;

public sealed class FlavourEquivalenceTests
{
    private const string FrameworkSkew = " skew 00:05:00 ";
    private const string PackageSkew = " skew 00:00:30 ";

    public static TheoryData<string, string, bool, bool> Cases()
    {
        var data = new TheoryData<string, string, bool, bool>();
        foreach (var flavour in new[] { "Simple", "Tasks", "Assistant", "Comms", "Mtg" })
        {
            foreach (var environment in new[] { "Development", "Production" })
            {
                foreach (var configured in new[] { true, false })
                {
                    data.Add(flavour, environment, configured, false);
                    data.Add(flavour, environment, configured, true);
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Package_resolves_exactly_what_the_consumer_configures_today(string flavour, string environment, bool configured, bool openApi)
    {
        var (today, package) = Setups(flavour);

        var expected = (await AuthSnapshot.TakeAsync(today, environment, configured, openApi)).Replace(FrameworkSkew, PackageSkew, StringComparison.Ordinal);
        var actual = await AuthSnapshot.TakeAsync(package, environment, configured, openApi);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(JwtValidationProfile.Default)]
    [InlineData(JwtValidationProfile.RawClaims)]
    [InlineData(JwtValidationProfile.Strict)]
    public async Task Every_profile_narrows_the_clock_skew(JwtValidationProfile profile)
    {
        var snapshot = await AuthSnapshot.TakeAsync((b, _) => b.AddLupiraJwt(o => o.Validation = profile, []), "Production", configured: true);

        Assert.Contains(PackageSkew, snapshot, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Snapshots_tell_the_flavours_apart()
    {
        var snapshots = new HashSet<string>();
        foreach (var flavour in new[] { "Simple", "Tasks", "Assistant", "Comms", "Mtg" })
        {
            snapshots.Add(await AuthSnapshot.TakeAsync(Setups(flavour).Package, "Development", configured: true)
                + await AuthSnapshot.TakeAsync(Setups(flavour).Package, "Development", configured: false));
        }

        Assert.Equal(5, snapshots.Count);
    }

    private static (Action<WebApplicationBuilder, bool> Today, Action<WebApplicationBuilder, bool> Package) Setups(string flavour) => flavour switch
    {
        "Simple" => (TodaySetups.Simple, PackageSetups.Simple),
        "Tasks" => (TodaySetups.Tasks, PackageSetups.Tasks),
        "Assistant" => (TodaySetups.Assistant, PackageSetups.Assistant),
        "Comms" => (TodaySetups.Comms, PackageSetups.Comms),
        "Mtg" => ((b, _) => TodaySetups.Mtg(b), PackageSetups.Mtg),
        _ => throw new ArgumentOutOfRangeException(nameof(flavour)),
    };
}
