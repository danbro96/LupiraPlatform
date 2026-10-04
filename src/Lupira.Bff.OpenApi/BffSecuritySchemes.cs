using System.Text.Json.Nodes;

namespace Lupira.Bff.OpenApi;

public static class BffSecuritySchemes
{
    public static JsonObject Cookie(string cookieName, string description) => new()
    {
        ["type"] = "apiKey",
        ["in"] = "cookie",
        ["name"] = cookieName,
        ["description"] = description,
    };

    public static JsonObject Bearer(string description) => new()
    {
        ["type"] = "http",
        ["scheme"] = "bearer",
        ["bearerFormat"] = "JWT",
        ["description"] = description,
    };
}
