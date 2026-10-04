using Microsoft.AspNetCore.Mvc.Testing;

namespace Lupira.Testing.Postgres;

/// <summary>Clients for a host running the Development auth handlers (<c>X-Dev-User</c>, <c>X-Dev-Service</c>,
/// device keys).</summary>
public static class DevClientExtensions
{
    public static HttpClient ApiClient<TProgram>(this WebApplicationFactory<TProgram> factory, string email, params string[] groups)
        where TProgram : class =>
        factory.CreateClient().AsUser(email, groups);

    /// <summary>A member client whose token also carries <paramref name="scopes"/>, e.g. <c>internal:read</c>.</summary>
    public static HttpClient ScopedClient<TProgram>(this WebApplicationFactory<TProgram> factory, string email, params string[] scopes)
        where TProgram : class =>
        factory.CreateClient().AsUser(email, []).WithScopes(scopes);

    public static HttpClient ServiceClient<TProgram>(this WebApplicationFactory<TProgram> factory, string serviceId)
        where TProgram : class =>
        factory.CreateClient().AsService(serviceId);

    public static HttpClient DeviceKeyClient<TProgram>(this WebApplicationFactory<TProgram> factory, string apiKey)
        where TProgram : class =>
        factory.CreateClient().WithDeviceKey(apiKey);

    /// <summary>A client with no auth header — for asserting unauthenticated requests are rejected.</summary>
    public static HttpClient AnonymousClient<TProgram>(this WebApplicationFactory<TProgram> factory)
        where TProgram : class =>
        factory.CreateClient();
}
