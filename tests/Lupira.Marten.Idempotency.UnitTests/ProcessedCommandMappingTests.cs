using Marten;
using Xunit;

namespace Lupira.Marten.Idempotency.UnitTests;

public class ProcessedCommandMappingTests
{
    [Fact]
    public void Ledger_table_name_does_not_depend_on_the_namespace()
    {
        var packaged = TableOf<ProcessedCommand>(o => o.Schema.For<ProcessedCommand>().Identity(x => x.CommandId));
        var elsewhere = TableOf<Elsewhere.ProcessedCommand>(o => o.Schema.For<Elsewhere.ProcessedCommand>().Identity(x => x.CommandId));

        Assert.Equal("mt_doc_processedcommand", packaged);
        Assert.Equal(packaged, elsewhere);
    }

    private static string TableOf<T>(Action<StoreOptions> configure)
    {
        var options = new StoreOptions();
        options.Connection("Host=localhost;Database=unused");
        configure(options);
        return ((IReadOnlyStoreOptions) options).FindOrResolveDocumentType(typeof(T)).TableName.Name;
    }
}
