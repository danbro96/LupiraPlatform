using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Lupira.Auth.Jwt.UnitTests;

internal sealed class Env(string environmentName) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;

    public string ApplicationName { get; set; } = "test";

    public string ContentRootPath { get; set; } = "/";

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
