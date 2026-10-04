using Lupira.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Xunit;

namespace Lupira.Hosting.Problems.UnitTests;

public sealed class OpResultMapTests
{
    public static TheoryData<OpStatus, int> ProblemStatuses => new()
    {
        { OpStatus.Invalid, StatusCodes.Status400BadRequest },
        { OpStatus.Forbidden, StatusCodes.Status403Forbidden },
        { OpStatus.Conflict, StatusCodes.Status409Conflict },
    };

    [Theory]
    [MemberData(nameof(ProblemStatuses))]
    public void Problem_shapes_map_each_failure_to_its_status(OpStatus status, int expected)
    {
        var value = new OpResult<int>(status, 0, "why");
        var unit = new OpResult(status, "why");

        Assert.All(
            new[]
            {
                OpResultMap.OkProblem(value).Result,
                OpResultMap.OkNotFoundProblem(value).Result,
                OpResultMap.OkNotFoundProblem(value, v => v.ToString()).Result,
                OpResultMap.AcceptedProblem(value).Result,
                OpResultMap.NoContentNotFoundProblem(unit).Result,
            },
            r =>
            {
                var problem = Assert.IsType<ProblemHttpResult>(r);
                Assert.Equal(expected, problem.StatusCode);
                Assert.Equal("why", problem.ProblemDetails.Detail);
                Assert.Equal($"https://httpstatuses.com/{expected}", problem.ProblemDetails.Type);
            });
    }

    [Fact]
    public void Success_shapes()
    {
        Assert.Equal(1, Assert.IsType<Ok<int>>(OpResultMap.OkOnly(OpResult<int>.Ok(1)).Result).Value);
        Assert.Equal("1", Assert.IsType<Ok<string>>(OpResultMap.OkNotFoundProblem(OpResult<int>.Ok(1), v => v.ToString()).Result).Value);
        Assert.Equal(1, Assert.IsType<Accepted<int>>(OpResultMap.AcceptedProblem(OpResult<int>.Ok(1)).Result).Value);
        Assert.IsType<NoContent>(OpResultMap.NoContentNotFound(OpResult.Ok()).Result);
    }

    [Fact]
    public void Not_found_maps_to_404()
    {
        Assert.IsType<NotFound>(OpResultMap.OkNotFound(OpResult<int>.NotFound()).Result);
        Assert.IsType<NotFound>(OpResultMap.OkNotFoundProblem(OpResult<int>.NotFound()).Result);
        Assert.IsType<NotFound>(OpResultMap.NoContentNotFound(OpResult.NotFound()).Result);
        Assert.IsType<NotFound>(OpResultMap.NoContentNotFoundProblem(OpResult.NotFound()).Result);
    }

    [Fact]
    public void A_status_the_shape_cannot_represent_throws()
    {
        Assert.Throws<InvalidOperationException>(() => OpResultMap.OkOnly(OpResult<int>.NotFound()));
        Assert.Throws<InvalidOperationException>(() => OpResultMap.OkNotFound(OpResult<int>.Invalid("x")));
        Assert.Throws<InvalidOperationException>(() => OpResultMap.OkProblem(OpResult<int>.NotFound()));
        Assert.Throws<InvalidOperationException>(() => OpResultMap.AcceptedProblem(OpResult<int>.NotFound()));
        Assert.Throws<InvalidOperationException>(() => OpResultMap.NoContentNotFound(OpResult.Conflict("x")));
    }
}
