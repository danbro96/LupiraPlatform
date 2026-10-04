using System.Text.Json.Serialization;

namespace Lupira.Contracts.Fires;

/// <summary>On a missed contract: retry once then ask (Retry), ask immediately (Ask), or drop (Drop).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FallbackMode>))]
public enum FallbackMode
{
    Retry,
    Ask,
    Drop,
}
