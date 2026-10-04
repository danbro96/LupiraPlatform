using System.Net;
using System.Security.Claims;
using Lupira.Auth.DeviceKeys.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lupira.Auth.DeviceKeys.UnitTests;

public sealed class DeviceKeyAuthHandlerTests
{
    private static readonly (Guid KeyId, string Secret, string Hash) Minted = DeviceKeyHashing.Mint();
    private static readonly Guid Principal = Guid.NewGuid();
    private static readonly Guid Device = Guid.NewGuid();

    private sealed class Store : IDeviceKeyStore
    {
        public static DeviceApiKey? Key { get; set; }

        public Task<DeviceApiKey?> FindAsync(Guid keyId, CancellationToken ct) =>
            Task.FromResult(Key?.Id == keyId ? Key : null);
    }

    private static async Task<WebApplication> StartAsync(DeviceApiKey? key)
    {
        Store.Key = key;
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication().AddLupiraDeviceKeys<Store>();
        builder.Services.AddAuthorization(o => o.AddPolicy("Ingest", p => p
            .AddAuthenticationSchemes(DeviceKeyAuthHandler.SchemeName).RequireAuthenticatedUser()));
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/ingest", (ClaimsPrincipal user) => DeviceKeyClaims.Get(user).ToString()).RequireAuthorization("Ingest");
        await app.StartAsync();
        return app;
    }

    private static DeviceApiKey Key(string[]? scopes = null, DateTimeOffset? revokedAt = null) => new()
    {
        Id = Minted.KeyId,
        PrincipalId = Principal,
        DeviceId = Device,
        KeyHash = Minted.Hash,
        Scopes = scopes ?? ["ingest"],
        RevokedAt = revokedAt,
    };

    private static async Task<HttpResponseMessage> GetAsync(WebApplication app, string? authorization)
    {
        var client = app.GetTestClient();
        if (authorization is not null) client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorization);
        return await client.GetAsync("/ingest");
    }

    [Fact]
    public async Task A_valid_key_resolves_its_principal_and_device()
    {
        await using var app = await StartAsync(Key());

        var res = await GetAsync(app, $"DeviceKey {DeviceKeyHashing.Format(Minted.KeyId, Minted.Secret)}");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal((Principal, Device).ToString(), await res.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Bearer abc")]
    [InlineData("DeviceKey not-a-key")]
    public async Task A_missing_or_malformed_key_is_401(string? authorization)
    {
        await using var app = await StartAsync(Key());

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync(app, authorization)).StatusCode);
    }

    [Fact]
    public async Task A_wrong_secret_is_401()
    {
        await using var app = await StartAsync(Key());

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync(app, $"DeviceKey {DeviceKeyHashing.Format(Minted.KeyId, "wrong")}")).StatusCode);
    }

    [Fact]
    public async Task A_revoked_key_is_401()
    {
        await using var app = await StartAsync(Key(revokedAt: DateTimeOffset.UtcNow));

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync(app, $"DeviceKey {DeviceKeyHashing.Format(Minted.KeyId, Minted.Secret)}")).StatusCode);
    }

    [Fact]
    public async Task A_key_without_the_ingest_scope_is_401()
    {
        await using var app = await StartAsync(Key(scopes: []));

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync(app, $"DeviceKey {DeviceKeyHashing.Format(Minted.KeyId, Minted.Secret)}")).StatusCode);
    }
}
