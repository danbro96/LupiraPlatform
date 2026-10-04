using Marten;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Lupira.Postgres.Health.UnitTests;

public sealed class DatabaseReadyCheckTests
{
    [Fact]
    public async Task An_unreachable_database_is_unhealthy_with_the_exception()
    {
        using var store = DocumentStore.For(o =>
        {
            o.Connection("Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=1");
            o.AutoCreateSchemaObjects = JasperFx.AutoCreate.None;
        });

        var result = await new DatabaseReadyCheck(store).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Postgres unreachable.", result.Description);
        Assert.NotNull(result.Exception);
    }
}
