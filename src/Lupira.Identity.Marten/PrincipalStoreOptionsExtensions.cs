using Marten;

namespace Lupira.Identity.Marten;

public static class PrincipalStoreOptionsExtensions
{
    public static StoreOptions AddLupiraPrincipals(this StoreOptions opts) => opts.AddLupiraPrincipals<Principal>();

    public static StoreOptions AddLupiraPrincipals<TPrincipal>(this StoreOptions opts)
        where TPrincipal : Principal
    {
        opts.Schema.For<TPrincipal>().Index(x => x.AuthentikSub, i => i.IsUnique = true).Index(x => x.Email);
        return opts;
    }
}
