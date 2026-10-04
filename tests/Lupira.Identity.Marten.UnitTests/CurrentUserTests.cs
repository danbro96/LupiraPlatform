using System.Security.Claims;
using Lupira.Identity.Marten.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lupira.Identity.Marten.UnitTests;

public sealed class CurrentUserTests
{
    [Fact]
    public async Task A_principal_without_sub_or_email_is_rejected()
    {
        using var store = OfflineStore.Create();
        await using var session = store.LightweightSession();
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", "n")], "test")) } };
        var user = new CurrentUser(http, new PrincipalDirectory(session), session, Options.Create(new CurrentUserOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => user.GetAsync());
    }

    [Fact]
    public void Registration_resolves_CurrentUser_with_the_configured_stamp()
    {
        using var store = OfflineStore.Create();
        var services = new ServiceCollection();
        services.AddScoped(_ => store.LightweightSession());
        services.AddLupiraPrincipalDirectory();
        services.AddLupiraCurrentUser(o => o.StampProvenance = true);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<CurrentUser>());
        Assert.True(scope.ServiceProvider.GetRequiredService<IOptions<CurrentUserOptions>>().Value.StampProvenance);
    }

    [Fact]
    public void Generic_registration_resolves_a_subclass_CurrentUser()
    {
        using var store = OfflineStore.Create();
        var services = new ServiceCollection();
        services.AddScoped(_ => store.LightweightSession());
        services.AddLupiraPrincipalDirectory<Extended.Principal>();
        services.AddLupiraCurrentUser<Extended.Principal>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<CurrentUser<Extended.Principal>>());
    }
}
