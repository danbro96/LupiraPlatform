namespace Lupira.Auth.DeviceKeys;

public interface IDeviceKeyStore
{
    Task<DeviceApiKey?> FindAsync(Guid keyId, CancellationToken ct);
}
