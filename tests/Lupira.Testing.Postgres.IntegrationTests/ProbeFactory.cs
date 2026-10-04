using Npgsql;

namespace Lupira.Testing.Postgres.IntegrationTests;

public sealed class ProbeFactory : LupiraApiFactory<Program>
{
    public int SchemaApplications { get; private set; }

    protected override string ConnectionStringName => "probe";

    protected override string AuthentikSlug => "lupira-probe";

    protected override void AddSettings(IDictionary<string, string?> settings) => settings["Probe:Extra"] = "extra";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseContentRoot(AppContext.BaseDirectory);
    }

    public async Task ExecuteAsync(string sql)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    protected override async Task ApplySchemaAsync()
    {
        SchemaApplications++;
        await ExecuteAsync("create table notes (id serial primary key)");
    }

    protected override Task ResetDataAsync() => ExecuteAsync("truncate table notes");
}
