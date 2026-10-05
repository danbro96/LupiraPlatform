using JasperFx.Events;
using Lupira.Testing.Postgres;
using Marten;
using Testcontainers.PostgreSql;
using Xunit;

namespace Lupira.Sync.Marten.IntegrationTests;

public sealed class EventStoreFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(PostgresImages.Postgres).Build();

    public DocumentStore Store { get; private set; } = null!;

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Store = DocumentStore.For(o =>
        {
            o.Connection(ConnectionString);
            o.Events.DatabaseSchemaName = "events";
            o.Events.AppendMode = EventAppendMode.Quick;
        });
        await Store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        Store.Dispose();
        await _postgres.DisposeAsync();
    }

    /// <summary>Starts or appends to each stream in its own transaction, so sequences follow the given order.</summary>
    public async Task WriteAsync<TAggregate>(Guid stream, params object[] events)
        where TAggregate : class
    {
        await using var session = Store.LightweightSession();
        var state = await session.Events.FetchStreamStateAsync(stream);
        if (state is null) session.Events.StartStream<TAggregate>(stream, events);
        else session.Events.Append(stream, events);
        await session.SaveChangesAsync();
    }
}
