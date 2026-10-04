using System.Text.Json.Serialization;

namespace Lupira.Contracts.Fires;

/// <summary>When a payload fires relative to its item's occurrence.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PromptFireKind>))]
public enum PromptFireKind
{
    OnStart,
    OnEnd,
    Offset,
    AllDayAt,
}
