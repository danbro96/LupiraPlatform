using Marten;

namespace Lupira.Identity.Marten.UnitTests;

internal static class OfflineStore
{
    public static DocumentStore Create(Action<StoreOptions>? configure = null) => DocumentStore.For(o =>
    {
        o.Connection("Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=1");
        o.AutoCreateSchemaObjects = JasperFx.AutoCreate.None;
        configure?.Invoke(o);
    });
}
