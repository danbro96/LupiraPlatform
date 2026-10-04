using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Lupira.Testing.Postgres;

/// <summary>
/// Hosts the real app against an ephemeral Postgres (Testcontainers). Runs in <c>Development</c> so the dev auth
/// handler is wired (<c>X-Dev-User</c>) — no Authentik needed. Data is reset per test via <see cref="ResetAsync"/>.
/// </summary>
public abstract class LupiraApiFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    private readonly PostgreSqlContainer _postgres;
    private bool _schemaApplied;

    protected LupiraApiFactory(string image = PostgresImages.Postgres)
    {
        _postgres = new PostgreSqlBuilder(image).Build();
        _postgres.StartAsync().GetAwaiter().GetResult();
    }

    public string ConnectionString => _postgres.GetConnectionString();

    /// <summary>The <c>Auth:Oidc:Authority</c> the host is given, or null when <see cref="AuthentikSlug"/> is unset.</summary>
    public string? Authority => AuthentikSlug is { } slug ? HostSettings.Authority(slug) : null;

    protected virtual string ConnectionStringName => "Postgres";

    /// <summary>Names a never-contacted <c>https://auth.test/application/o/&lt;slug&gt;/</c> authority — it only feeds
    /// the RFC 9728 metadata and the JWT challenge. Null leaves <c>Auth:Oidc:Authority</c> unset.</summary>
    protected virtual string? AuthentikSlug => null;

    /// <summary>Ensure the schema exists (once), then wipe all data — call at the start of each test.</summary>
    public async Task ResetAsync()
    {
        if (!_schemaApplied)
        {
            await ApplySchemaAsync();
            _schemaApplied = true;
        }

        await ResetDataAsync();
    }

    protected virtual Task ApplySchemaAsync() => Task.CompletedTask;

    protected virtual Task ResetDataAsync() => Task.CompletedTask;

    /// <summary>Extra configuration keys, applied with the connection string and authority.</summary>
    protected virtual void AddSettings(IDictionary<string, string?> settings)
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        var settings = HostSettings.Compose(ConnectionStringName, ConnectionString, AuthentikSlug);
        AddSettings(settings);

        // Host settings reach Program's eager builder.Configuration reads; the in-memory source wins over appsettings.
        foreach (var (key, value) in settings) builder.UseSetting(key, value);
        builder.ConfigureAppConfiguration(cfg => cfg.AddInMemoryCollection(settings));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _postgres.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
