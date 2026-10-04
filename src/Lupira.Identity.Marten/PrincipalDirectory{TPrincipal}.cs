using Marten;
using Npgsql;

namespace Lupira.Identity.Marten;

/// <summary>
/// Resolves an authenticated principal (OIDC <c>sub</c> + email) to a local <typeparamref name="TPrincipal"/>,
/// JIT-provisioning on first sight. Resolves by <c>sub</c> first then email. The host's <c>CurrentUser</c>
/// supplies the claims; this never sees the request.
///
/// Provisioning is a check-then-insert, so two concurrent first-sight logins both reach it: a unique index on
/// <c>AuthentikSub</c> lets one win and the loser adopts the winner's row. Without both halves one login forks
/// into two principals and everything keyed to the principal id resolves to whichever row Postgres returns.
/// </summary>
public class PrincipalDirectory<TPrincipal>(IDocumentSession session)
    where TPrincipal : Principal, new()
{
    /// <summary>How stale <see cref="Principal.LastSeenAt"/> must be before a read refreshes it, so
    /// steady-state resolution doesn't write on every authenticated request.</summary>
    private static readonly TimeSpan LastSeenRefresh = TimeSpan.FromMinutes(5);

    private static readonly IReadOnlyDictionary<Guid, TPrincipal> EmptyLookup = new Dictionary<Guid, TPrincipal>();

    /// <summary>Looks up an existing principal by login email without provisioning. Used where a missing principal
    /// is a "not found" (e.g. revoking a grant), not a reason to create a placeholder.</summary>
    public async Task<TPrincipal?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        email = Normalize(email);
        if (email.Length == 0) return null;
        return await session.Query<TPrincipal>().Where(x => x.Email == email).OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
    }

    /// <summary>Looks up an existing principal by OIDC <c>sub</c> without provisioning. Used by service-to-service
    /// seams acting on a named principal — an unknown sub is a "not found", never a placeholder row.</summary>
    public async Task<TPrincipal?> FindBySubAsync(string sub, CancellationToken ct = default) =>
        await session.Query<TPrincipal>().Where(x => x.AuthentikSub == sub).OrderBy(x => x.Id).FirstOrDefaultAsync(ct);

    public async Task<TPrincipal> ResolveOrProvisionAsync(string? sub, string email, string? name, CancellationToken ct = default)
    {
        email = Normalize(email);

        var now = DateTimeOffset.UtcNow;
        var p = await FindAsync(sub, email, ct);

        if (p is null)
        {
            p = new TPrincipal { Id = Guid.CreateVersion7(), AuthentikSub = sub ?? $"email|{email}", Email = email, DisplayName = name, CreatedAt = now, LastSeenAt = now };
            session.Store(p);
            try
            {
                await session.SaveChangesAsync(ct);
                return p;
            }
            catch (Exception ex) when (IsUniqueViolation(ex))
            {
                // Lost the provisioning race: a concurrent request inserted this sub first. Adopt its row
                // rather than forking a second identity for the same login.
                session.EjectAllPendingChanges();
                p = await FindAsync(sub, email, ct);
                if (p is null) throw;
            }
        }

        var changed = false;
        if (sub is not null && p.AuthentikSub != sub && p.AuthentikSub.StartsWith("email|", StringComparison.Ordinal))
        {
            p.AuthentikSub = sub;
            changed = true;
        }

        if (email.Length > 0 && p.Email != email)
        {
            p.Email = email;
            changed = true;
        }

        if (name is not null && p.DisplayName != name)
        {
            p.DisplayName = name;
            changed = true;
        }

        if (now - p.LastSeenAt > LastSeenRefresh)
        {
            p.LastSeenAt = now;
            changed = true;
        }

        if (changed)
        {
            session.Store(p);
            await session.SaveChangesAsync(ct);
        }

        return p;
    }

    /// <summary>Batch-resolve stored principal ids to their principal rows for the read boundary.</summary>
    public async Task<IReadOnlyDictionary<Guid, TPrincipal>> LookupAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var distinct = ids.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (distinct.Length == 0) return EmptyLookup;
        var rows = await session.Query<TPrincipal>().Where(p => distinct.Contains(p.Id)).ToListAsync(ct);
        return rows.ToDictionary(p => p.Id);
    }

    /// <summary>The single normalization point for a login email. Lookup only matches if every read and
    /// write normalizes identically — a missed lowercase silently provisions a second principal.</summary>
    internal static string Normalize(string email) => email.Trim().ToLowerInvariant();

    internal static bool IsUniqueViolation(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
            if (e is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) return true;
        return false;
    }

    /// <summary>Resolve by <c>sub</c> then email, ordered so the result is stable if duplicate rows ever exist —
    /// an unordered <c>FirstOrDefault</c> over duplicates flips between them per request.</summary>
    private async Task<TPrincipal?> FindAsync(string? sub, string email, CancellationToken ct)
    {
        TPrincipal? p = null;
        if (sub is not null)
            p = await session.Query<TPrincipal>().Where(x => x.AuthentikSub == sub).OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (p is null && email.Length > 0)
            p = await session.Query<TPrincipal>().Where(x => x.Email == email).OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        return p;
    }
}
