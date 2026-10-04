using System.Text.Json;
using Xunit;

namespace Lupira.Sync.UnitTests;

public class SectionGuardDtoTests
{
    [Fact]
    public void Serializes_as_ts_and_cmd()
    {
        var cmd = Guid.Parse("0190f3a1-7c2e-7d4b-9a1f-2b3c4d5e6f70");
        var json = JsonSerializer.Serialize(SectionGuardDto.From(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), cmd), JsonSerializerOptions.Web);
        Assert.Equal("""{"ts":"2026-01-02T03:04:05+00:00","cmd":"0190f3a1-7c2e-7d4b-9a1f-2b3c4d5e6f70"}""", json);
    }
}
