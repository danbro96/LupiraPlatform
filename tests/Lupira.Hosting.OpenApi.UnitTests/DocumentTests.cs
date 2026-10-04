using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Xunit;

namespace Lupira.Hosting.OpenApi.UnitTests;

public sealed class DocumentTests
{
    private static readonly Action<LupiraOpenApiOptions> Bearer = o =>
    {
        o.Title = "Widget API";
        o.Description = "Widgets for tests.";
    };

    private static readonly Action<LupiraOpenApiOptions> Cookie = o =>
    {
        o.Security = ApiSecurity.Cookie("__Host-widgets", "Session cookie minted by the BFF's OIDC login.");
        o.AuthDetection = AuthDetection.NotAllowAnonymous;
    };

    [Theory]
    [InlineData("bearer")]
    [InlineData("cookie")]
    public async Task Document_matches_golden(string shape)
    {
        await using var app = shape == "bearer"
            ? await TestApp.StartAsync(Bearer)
            : await TestApp.StartAsync(Cookie, fallbackPolicy: true);

        var actual = await app.DocumentAsync();

        var expected = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", $"{shape}.json"));
        Assert.Equal(expected.ReplaceLineEndings().TrimEnd(), actual.ReplaceLineEndings().TrimEnd());
    }

    [Fact]
    public async Task Secured_operation_gets_scheme_401_500_and_problem_bodies()
    {
        await using var app = await TestApp.StartAsync(Bearer);

        var operation = Operation(await Parse(app), "/widgets/{id}", "get");

        Assert.Equal("Bearer", Assert.Single(operation["security"]!.AsArray()[0]!.AsObject()).Key);
        Assert.Equal(["200", "404", "401", "500"], ResponseCodes(operation));
        foreach (var code in new[] { "404", "401", "500" })
            Assert.Equal("#/components/schemas/ProblemDetails", ProblemRef(operation, code));
    }

    [Fact]
    public async Task Anonymous_operation_gets_only_500()
    {
        await using var app = await TestApp.StartAsync(Bearer);

        var operation = Operation(await Parse(app), "/public", "get");

        Assert.Null(operation["security"]);
        Assert.Equal(["200", "500"], ResponseCodes(operation));
    }

    [Fact]
    public async Task AuthorizeData_ignores_endpoints_without_authorize_metadata()
    {
        await using var app = await TestApp.StartAsync(Bearer);

        Assert.Null(Operation(await Parse(app), "/shared/{token}/widgets", "get")["security"]);
    }

    [Fact]
    public async Task NotAllowAnonymous_secures_every_endpoint_without_allow_anonymous()
    {
        await using var app = await TestApp.StartAsync(Cookie, fallbackPolicy: true);
        var document = await Parse(app);

        Assert.Equal("Cookie", Assert.Single(Operation(document, "/shared/{token}/widgets", "get")["security"]!.AsArray()[0]!.AsObject()).Key);
        Assert.Null(Operation(document, "/public", "get")["security"]);
    }

    [Fact]
    public async Task UnauthorizedResponse_off_keeps_security_without_401()
    {
        await using var app = await TestApp.StartAsync(o => o.UnauthorizedResponse = false);

        var operation = Operation(await Parse(app), "/widgets/{id}", "get");

        Assert.NotNull(operation["security"]);
        Assert.DoesNotContain("401", ResponseCodes(operation));
    }

    [Fact]
    public async Task ApiKeyHeader_declares_a_header_scheme()
    {
        await using var app = await TestApp.StartAsync(o => o.Security = ApiSecurity.ApiKeyHeader("X-API-Key", "API key."));
        var document = await Parse(app);

        var scheme = document["components"]!["securitySchemes"]!["ApiKey"]!;
        Assert.Equal("apiKey", (string?) scheme["type"]);
        Assert.Equal("header", (string?) scheme["in"]);
        Assert.Equal("X-API-Key", (string?) scheme["name"]);
        Assert.Equal("ApiKey", Assert.Single(Operation(document, "/widgets/{id}", "get")["security"]!.AsArray()[0]!.AsObject()).Key);
    }

    [Fact]
    public async Task Null_security_declares_no_scheme_and_no_401()
    {
        await using var app = await TestApp.StartAsync(o => o.Security = null);
        var document = await Parse(app);

        Assert.Null(document["components"]!["securitySchemes"]);
        Assert.Equal(["200", "404", "500"], ResponseCodes(Operation(document, "/widgets/{id}", "get")));
    }

    [Fact]
    public async Task Title_unset_keeps_generated_info()
    {
        await using var app = await TestApp.StartAsync();

        var info = (await Parse(app))["info"]!;

        Assert.Equal("Widgets | v1", (string?) info["title"]);
        Assert.Equal("1.0.0", (string?) info["version"]);
        Assert.Null(info["description"]);
    }

    [Fact]
    public async Task Idempotency_header_documents_only_marked_operations()
    {
        await using var app = await TestApp.StartAsync(o => o.IdempotencyHeader<IdempotentMutation>("Command id."));
        var document = await Parse(app);

        var header = Assert.Single(Operation(document, "/widgets", "post")["parameters"]!.AsArray())!;
        Assert.Equal("Idempotency-Key", (string?) header["name"]);
        Assert.Equal("header", (string?) header["in"]);
        Assert.Null(header["required"]);
        Assert.Equal("Command id.", (string?) header["description"]);
        Assert.Equal("uuid", (string?) header["schema"]!["format"]);
        Assert.Null(Operation(document, "/public", "get")["parameters"]);
    }

    [Fact]
    public async Task Operation_transformers_run_before_the_built_in_one()
    {
        await using var app = await TestApp.StartAsync(o =>
        {
            o.IdempotencyHeader<IdempotentMutation>("Command id.");
            o.OperationTransformers.Add((operation, _, _) =>
            {
                operation.Parameters ??= [];
                operation.Parameters.Add(new OpenApiParameter { Name = "first", In = ParameterLocation.Query });
                return Task.CompletedTask;
            });
        });

        var parameters = Operation(await Parse(app), "/widgets", "post")["parameters"]!.AsArray();

        Assert.Equal(["first", "Idempotency-Key"], parameters.Select(p => (string?) p!["name"]));
    }

    [Fact]
    public async Task DropNullEnumMembers_strips_null_from_shared_enum_schemas()
    {
        await using var off = await TestApp.StartAsync();
        await using var on = await TestApp.StartAsync(o => o.DropNullEnumMembers = true);

        var offMembers = EnumMembers(await Parse(off));
        var onMembers = EnumMembers(await Parse(on));

        Assert.Contains(offMembers, m => m is null);
        Assert.Equal(["Newest", "Oldest"], onMembers);
    }

    [Fact]
    public async Task DateTimeOffsetAsString_restores_type_hidden_by_a_custom_converter()
    {
        await using var off = await TestApp.StartAsync(opaqueDateTimeOffset: true);
        await using var on = await TestApp.StartAsync(o => o.DateTimeOffsetAsString = true, opaqueDateTimeOffset: true);

        Assert.Null(WidgetProperty(await Parse(off), "updatedAt")["type"]);
        var document = await Parse(on);
        var updatedAt = WidgetProperty(document, "updatedAt");
        Assert.Equal("string", (string?) updatedAt["type"]);
        Assert.Equal("date-time", (string?) updatedAt["format"]);
        Assert.Equal(["null", "string"], WidgetProperty(document, "retiredAt")["type"]!.AsArray().Select(t => (string?) t));
    }

    private static async Task<JsonNode> Parse(Microsoft.AspNetCore.Builder.WebApplication app) =>
        JsonNode.Parse(await app.DocumentAsync())!;

    private static JsonNode Operation(JsonNode document, string path, string method) =>
        document["paths"]![path]![method]!;

    private static string[] ResponseCodes(JsonNode operation) =>
        [.. operation["responses"]!.AsObject().Select(r => r.Key)];

    private static string? ProblemRef(JsonNode operation, string code) =>
        (string?) operation["responses"]![code]!["content"]!["application/problem+json"]!["schema"]!["$ref"];

    private static List<string?> EnumMembers(JsonNode document) =>
        [.. document["components"]!["schemas"]!["WidgetSort"]!["enum"]!.AsArray().Select(m => m?.ToString())];

    private static JsonNode WidgetProperty(JsonNode document, string name) =>
        document["components"]!["schemas"]!["WidgetDto"]!["properties"]![name]!;
}
