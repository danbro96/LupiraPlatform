using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Xunit;

namespace Lupira.Bff.OpenApi.UnitTests;

public class BffDocumentTransformerTests
{
    [Fact]
    public async Task Grafts_the_merged_surface_from_embedded_files_and_lets_a_csharp_endpoint_win()
    {
        var declared = new OpenApiPathItem { Description = "declared in C#" };
        var document = new OpenApiDocument { Paths = new OpenApiPaths { ["/api/me"] = declared } };
        var services = new ServiceCollection().BuildServiceProvider();
        var transformer = new BffDocumentTransformer(
            NullLogger<BffDocumentTransformer>.Instance,
            Options.Create(Fixture.TasksOptions()),
            new Environment { ApplicationName = typeof(BffDocumentTransformerTests).Assembly.GetName().Name! },
            services);

        await transformer.TransformAsync(
            document,
            new OpenApiDocumentTransformerContext { DocumentName = "v1", DescriptionGroups = [], ApplicationServices = services },
            CancellationToken.None);

        Assert.Same(declared, document.Paths["/api/me"]);
        Assert.Contains("/api/share/items/{itemId}", document.Paths.Keys);
        Assert.Equal(["Bearer", "Cookie", "GuestCookie"], document.Components!.SecuritySchemes!.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("LupiraTasks BFF", document.Info.Title);
    }

    private sealed class Environment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";

        public string ApplicationName { get; set; } = string.Empty;

        public string WebRootPath { get; set; } = string.Empty;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
