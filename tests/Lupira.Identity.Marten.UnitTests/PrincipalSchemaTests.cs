using Marten;
using Xunit;

namespace Lupira.Identity.Marten.UnitTests;

public sealed class PrincipalSchemaTests
{
    private static string Ddl(Action<StoreOptions> register)
    {
        using var store = OfflineStore.Create(register);
        return store.Storage.ToDatabaseScript();
    }

    [Fact]
    public void The_helper_matches_the_hand_written_registration()
    {
        var helper = Ddl(o => o.AddLupiraPrincipals());
        var inline = Ddl(o => o.Schema.For<Principal>().Index(x => x.AuthentikSub, i => i.IsUnique = true).Index(x => x.Email));

        Assert.Equal(inline, helper);
        Assert.Contains("public.mt_doc_principal ", helper, StringComparison.Ordinal);
        Assert.Contains("CREATE UNIQUE INDEX mt_doc_principal_uidx_authentik_sub", helper, StringComparison.Ordinal);
        Assert.Contains("CREATE INDEX mt_doc_principal_idx_email", helper, StringComparison.Ordinal);
    }

    [Fact]
    public void A_subclass_named_Principal_keeps_the_table_and_indexes()
    {
        Assert.Equal(Ddl(o => o.AddLupiraPrincipals()), Ddl(o => o.AddLupiraPrincipals<Extended.Principal>()));
    }
}
