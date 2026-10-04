using System.Text.Json.Serialization;

namespace Lupira.Contracts.Fires;

/// <summary>What a <see cref="Ref"/> points at.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RefKind>))]
public enum RefKind
{
    Event,
    Contact,
    Task,
    Place,
    External,
}
