using Microsoft.OpenApi;

namespace Lupira.Hosting.OpenApi;

/// <summary>The document's security scheme and the id operations reference it by.</summary>
public sealed class ApiSecurity
{
    public const string DefaultBearerDescription = "OIDC bearer token. Send as `Authorization: Bearer <token>`.";

    private readonly Func<OpenApiSecurityScheme> createScheme;

    private ApiSecurity(string schemeId, Func<OpenApiSecurityScheme> createScheme)
    {
        SchemeId = schemeId;
        this.createScheme = createScheme;
    }

    public string SchemeId { get; }

    public static ApiSecurity Bearer(string description = DefaultBearerDescription) =>
        new("Bearer", () => new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = description,
        });

    public static ApiSecurity ApiKeyHeader(string headerName, string description) =>
        new("ApiKey", () => new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = headerName,
            Description = description,
        });

    public static ApiSecurity Cookie(string cookieName, string description) =>
        new("Cookie", () => new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Cookie,
            Name = cookieName,
            Description = description,
        });

    internal OpenApiSecurityScheme CreateScheme() => createScheme();
}
