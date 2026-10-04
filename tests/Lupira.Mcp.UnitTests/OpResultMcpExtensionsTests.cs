using Lupira.Results;
using ModelContextProtocol;
using Xunit;

namespace Lupira.Mcp.UnitTests;

public sealed class OpResultMcpExtensionsTests
{
    public static TheoryData<OpResult<int>, string> Failures => new()
    {
        { OpResult<int>.NotFound(), "Not found." },
        { OpResult<int>.Forbidden("not yours"), "not yours" },
        { OpResult<int>.Invalid("bad name"), "bad name" },
        { OpResult<int>.Conflict("taken"), "taken" },
        { new OpResult<int>(OpStatus.Forbidden, 0, null), "Forbidden." },
        { new OpResult<int>(OpStatus.Invalid, 0, null), "Invalid request." },
        { new OpResult<int>(OpStatus.Conflict, 0, null), "Conflict." },
        { new OpResult<int>((OpStatus) 99, 0, "x"), "Unexpected result." },
    };

    [Fact]
    public void Ok_unwraps_the_value() => Assert.Equal(7, OpResult<int>.Ok(7).Require());

    [Theory]
    [MemberData(nameof(Failures))]
    public void A_failure_becomes_an_mcp_error(OpResult<int> result, string message)
    {
        Assert.Equal(message, Assert.Throws<McpException>(() => result.Require()).Message);
        Assert.Equal(message, Assert.Throws<McpException>(() => new OpResult(result.Status, result.Error).Require()).Message);
    }

    [Fact]
    public void Ok_without_content_passes() => OpResult.Ok().Require();
}
