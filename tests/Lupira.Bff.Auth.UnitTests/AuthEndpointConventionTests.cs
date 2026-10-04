using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Lupira.Bff.Auth.UnitTests;

public sealed class AuthEndpointConventionTests
{
    [Fact]
    public async Task Conventions_on_the_returned_group_reach_the_auth_endpoints()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.AddLupiraBffAuth(o => o.ApiPrefixes = ["/api"]);
        await using var app = builder.Build();

        app.MapLupiraAuthEndpoints().WithTags("Auth");

        var user = ((IEndpointRouteBuilder) app).DataSources.SelectMany(s => s.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText == "/auth/user");
        Assert.Contains("Auth", user.Metadata.GetRequiredMetadata<ITagsMetadata>().Tags);
    }
}
