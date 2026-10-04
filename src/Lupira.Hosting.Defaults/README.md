# Lupira.Hosting.Defaults

Host defaults: JSON contract, forwarded headers behind the tunnel, ProblemDetails status-code pages, persisted data-protection keys.

```csharp
builder.AddLupiraDefaults(o => { o.UtcDateTimeOffsets = true; o.ThrowOnBadRequest = true; o.ForwardedHeaders |= ForwardedHeaders.XForwardedHost; });
app.UseLupiraDefaults();
app.UseExceptionHandler();
```
