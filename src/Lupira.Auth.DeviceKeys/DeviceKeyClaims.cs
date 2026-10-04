using System.Security.Claims;

namespace Lupira.Auth.DeviceKeys;

/// <summary>Claim types carried by a device-key-authenticated request.</summary>
public static class DeviceKeyClaims
{
    public const string PrincipalId = "principal_id";
    public const string DeviceId = "device_id";

    /// <summary>The resolved (principal, device) the device key acts as. Throws if the request was not device-key authed.</summary>
    public static (Guid PrincipalId, Guid DeviceId) Get(ClaimsPrincipal p) =>
        (Guid.Parse(p.FindFirst(PrincipalId)?.Value ?? throw new InvalidOperationException("Not a device-key principal.")),
         Guid.Parse(p.FindFirst(DeviceId)?.Value ?? throw new InvalidOperationException("Not a device-key principal.")));
}
