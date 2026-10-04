using Lupira.Testing.Postgres.IntegrationTests;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var eagerAuthority = builder.Configuration["Auth:Oidc:Authority"];
var app = builder.Build();

app.MapGet("/settings", (IConfiguration config) => new SettingsProbe
{
    EagerAuthority = eagerAuthority,
    Authority = config["Auth:Oidc:Authority"],
    Extra = config["Probe:Extra"],
    Environment = app.Environment.EnvironmentName,
});

app.MapGet("/whoami", (HttpRequest request) => request.Headers["X-Dev-User"].ToString());

app.MapGet("/notes/count", async (IConfiguration config) =>
{
    await using var conn = new NpgsqlConnection(config.GetConnectionString("probe"));
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand("select count(*) from notes", conn);
    return (long)(await cmd.ExecuteScalarAsync())!;
});

app.Run();

public partial class Program;
