using Xunit;

namespace Lupira.Testing.Postgres.UnitTests;

public sealed class DevHeadersTests
{
    private static string? Header(HttpClient client, string name) =>
        client.DefaultRequestHeaders.TryGetValues(name, out var values) ? string.Join("|", values) : null;

    [Fact]
    public void User_without_groups_sends_only_the_email()
    {
        using var client = new HttpClient().AsUser("alice@x.test", []);

        Assert.Equal("alice@x.test", Header(client, "X-Dev-User"));
        Assert.Null(Header(client, "X-Dev-Groups"));
    }

    [Fact]
    public void Groups_are_comma_joined()
    {
        using var client = new HttpClient().AsUser("alice@x.test", ["cal-admins", "family"]);

        Assert.Equal("cal-admins,family", Header(client, "X-Dev-Groups"));
    }

    [Fact]
    public void Scopes_are_space_joined_and_omitted_when_empty()
    {
        using var scoped = new HttpClient().WithScopes(["internal:read", "internal:write"]);
        using var plain = new HttpClient().WithScopes([]);

        Assert.Equal("internal:read internal:write", Header(scoped, "X-Dev-Scopes"));
        Assert.Null(Header(plain, "X-Dev-Scopes"));
    }

    [Fact]
    public void Service_sends_the_service_id()
    {
        using var client = new HttpClient().AsService("comms-telegram");

        Assert.Equal("comms-telegram", Header(client, "X-Dev-Service"));
        Assert.Null(Header(client, "X-Dev-User"));
    }

    [Fact]
    public void Device_key_uses_the_device_key_scheme()
    {
        using var client = new HttpClient().WithDeviceKey("abc.def");

        Assert.Equal("DeviceKey abc.def", client.DefaultRequestHeaders.Authorization?.ToString());
    }
}
