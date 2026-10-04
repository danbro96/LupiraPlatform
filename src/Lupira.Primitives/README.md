# Lupira.Primitives

BCL-only primitives: `DeterministicGuid.From(key)`, `ContentHash.Of(content)`, `UtcDateTimeOffsetConverter`.

```csharp
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new UtcDateTimeOffsetConverter()));
```
