# Lupira.Auth.DeviceKeys.AspNetCore

`DeviceKey` authentication scheme: verifies the key against `IDeviceKeyStore`, rejects revoked keys and keys without the `ingest` scope, and emits `principal_id` / `device_id` claims.

```csharp
builder.Services.AddAuthentication().AddLupiraDeviceKeys<MartenDeviceKeyStore>();
```
