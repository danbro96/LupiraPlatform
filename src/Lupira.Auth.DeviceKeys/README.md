# Lupira.Auth.DeviceKeys

The per-device ingest credential `DeviceKey {keyId:N}.{secret}`: the stored `DeviceApiKey` (hash only), `DeviceKeyHashing` (mint, hash, constant-time verify, parse, format), `DeviceKeyClaims` and the `IDeviceKeyStore` port. BCL only, so a domain project can mint and store keys.

The handler lives in `Lupira.Auth.DeviceKeys.AspNetCore`.
