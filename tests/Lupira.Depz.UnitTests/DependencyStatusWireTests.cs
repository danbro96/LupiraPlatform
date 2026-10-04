using System.Text.Json;
using Lupira.Contracts.Depz;
using Xunit;

namespace Lupira.Depz.UnitTests;

public sealed class DependencyStatusWireTests
{
    [Fact]
    public void Status_serializes_by_name_without_a_registered_converter()
    {
        var json = JsonSerializer.Serialize(
            new DependencyDto { Name = "a", Status = DependencyStatus.NoCredential },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("\"status\":\"NoCredential\"", json);
    }
}
