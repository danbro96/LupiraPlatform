# Lupira.Testing.Postgres

Integration-test host for Lupira APIs: the real app on an ephemeral Postgres, in `Development` with dev-header auth.

```csharp
public sealed class TasksApiTestFactory : LupiraApiFactory<Program>
{
    protected override string ConnectionStringName => "tasks";
    protected override string AuthentikSlug => "lupira-tasks";

    public IDocumentStore Store => Services.GetRequiredService<IDocumentStore>();

    protected override Task ApplySchemaAsync() => Store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
    protected override Task ResetDataAsync() => Store.Advanced.ResetAllData();

    protected override void AddSettings(IDictionary<string, string?> settings) =>
        settings["RateLimit:RequestsPerMinute"] = "100000";
}

[CollectionDefinition("integration")]
public sealed class TasksApiCollection : ICollectionFixture<TasksApiTestFactory>;
```

- Image: `: LupiraApiFactory<Program>(PostgresImages.PostGis)`; `PgVector` for pgvector.
- Settings go in as host settings (seen by `Program`'s eager `builder.Configuration` reads) and as in-memory configuration (wins over `appsettings`).
- Test services: override `ConfigureWebHost`, call `base`, then `builder.ConfigureTestServices(...)`.
- Clients: `Factory.ApiClient("alice@x.test", "cal-admins")`, `ScopedClient("svc@x.test", "internal:read")`, `ServiceClient("comms-telegram")`, `DeviceKeyClient(key)`, `AnonymousClient()` — extensions on any `WebApplicationFactory<T>`.
