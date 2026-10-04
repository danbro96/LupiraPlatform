using Xunit;

namespace Lupira.Testing.Mcp.UnitTests;

public sealed class ResourceMetadataConformanceTests(ConformanceHost host) : McpResourceMetadataTests, IClassFixture<ConformanceHost>
{
    protected override string Issuer => ConformanceHost.Issuer;

    protected override HttpClient CreateAnonymousClient() => host.CreateClient();
}
