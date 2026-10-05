using System.Text.Json;
using Xunit;

namespace Lupira.Sync.UnitTests;

public class SyncPageTests
{
    [Fact]
    public void Serializes_with_camel_case_members()
    {
        var page = new SyncPage<string>
        {
            Cursor = "42.abc",
            HasMore = true,
            Reset = false,
            Changed = ["a"],
            Deleted = [Guid.Parse("0190f3a1-7c2e-7d4b-9a1f-2b3c4d5e6f70")],
        };
        Assert.Equal(
            """{"cursor":"42.abc","hasMore":true,"reset":false,"changed":["a"],"deleted":["0190f3a1-7c2e-7d4b-9a1f-2b3c4d5e6f70"]}""",
            JsonSerializer.Serialize(page, JsonSerializerOptions.Web));
    }
}
