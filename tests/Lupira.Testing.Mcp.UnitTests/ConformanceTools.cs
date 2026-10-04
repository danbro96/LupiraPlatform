using ModelContextProtocol.Server;

namespace Lupira.Testing.Mcp.UnitTests;

[McpServerToolType]
public sealed class ConformanceTools
{
    [McpServerTool(Name = "list_things")]
    public string ListThings() => "[]";

    [McpServerTool(Name = "echo")]
    public string Echo(string text) => text;
}
