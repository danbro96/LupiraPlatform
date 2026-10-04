using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lupira.Hosting.Problems.UnitTests;

public sealed class ProblemExceptionHandlerTests
{
    private static async Task<WebApplication> StartAsync(Action<ProblemExceptionOptions>? configure = null)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLupiraProblems(configure);
        builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
        var app = builder.Build();
        app.UseExceptionHandler();
        app.MapPost("/items", ([Microsoft.AspNetCore.Mvc.FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey) => TypedResults.NoContent());
        app.MapGet("/boom", IResult () => throw new InvalidOperationException("secret detail"));
        await app.StartAsync();
        return app;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(WebApplication app, HttpRequestMessage request)
    {
        var res = await app.GetTestClient().SendAsync(request);
        return (res.StatusCode, await res.Content.ReadFromJsonAsync<JsonElement>());
    }

    private static HttpRequestMessage BadIdempotencyKey() =>
        new(HttpMethod.Post, "/items") { Headers = { { "Idempotency-Key", "not-a-guid" } } };

    [Fact]
    public async Task A_binding_failure_is_a_400_problem_with_the_framework_message()
    {
        await using var app = await StartAsync();

        var (status, body) = await SendAsync(app, BadIdempotencyKey());

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal("Bad request", body.GetProperty("title").GetString());
        Assert.Contains("idempotencyKey", body.GetProperty("detail").GetString());
        Assert.True(body.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task The_idempotency_hook_rewrites_the_detail()
    {
        await using var app = await StartAsync(o => o.BadRequestDetail = IdempotencyKeyProblem.Detail);

        var (_, body) = await SendAsync(app, BadIdempotencyKey());

        Assert.Equal("Idempotency-Key must be a GUID.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task A_fault_is_a_500_without_the_exception_message()
    {
        await using var app = await StartAsync();

        var (status, body) = await SendAsync(app, new HttpRequestMessage(HttpMethod.Get, "/boom"));

        Assert.Equal(HttpStatusCode.InternalServerError, status);
        Assert.Equal("Internal server error", body.GetProperty("title").GetString());
        Assert.False(body.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task The_internal_error_hook_supplies_the_detail()
    {
        await using var app = await StartAsync(o => o.InternalErrorDetail = _ => "Reference: abc");

        var (_, body) = await SendAsync(app, new HttpRequestMessage(HttpMethod.Get, "/boom"));

        Assert.Equal("Reference: abc", body.GetProperty("detail").GetString());
    }
}
