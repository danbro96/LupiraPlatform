# Lupira.Mcp

MCP server conventions: strict tool arguments, `OpResult` → `McpException`, RFC 9728 resource metadata.

```csharp
builder.Services.AddLupiraMcp().WithTools<CalendarTools>();
app.MapMcpResourceMetadata(app.Configuration["Auth:Oidc:Authority"]);
app.MapLupiraMcp();
```
