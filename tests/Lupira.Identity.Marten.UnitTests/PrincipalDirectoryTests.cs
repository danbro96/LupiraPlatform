using Npgsql;
using Xunit;

namespace Lupira.Identity.Marten.UnitTests;

public sealed class PrincipalDirectoryTests
{
    [Fact]
    public void Emails_normalize_to_trimmed_lowercase()
    {
        Assert.Equal("p@x.test", PrincipalDirectory<Principal>.Normalize("  P@X.Test "));
    }

    [Fact]
    public void A_wrapped_unique_violation_is_detected()
    {
        var unique = new PostgresException("dup", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation);

        Assert.True(PrincipalDirectory<Principal>.IsUniqueViolation(new InvalidOperationException("outer", unique)));
        Assert.False(PrincipalDirectory<Principal>.IsUniqueViolation(new PostgresException("fk", "ERROR", "ERROR", PostgresErrorCodes.ForeignKeyViolation)));
    }

    [Fact]
    public async Task An_empty_email_is_not_found_without_a_query()
    {
        using var store = OfflineStore.Create();
        await using var session = store.LightweightSession();

        Assert.Null(await new PrincipalDirectory(session).FindByEmailAsync("  "));
    }

    [Fact]
    public async Task An_empty_lookup_returns_no_rows_without_a_query()
    {
        using var store = OfflineStore.Create();
        await using var session = store.LightweightSession();

        Assert.Empty(await new PrincipalDirectory(session).LookupAsync([Guid.Empty]));
    }
}
