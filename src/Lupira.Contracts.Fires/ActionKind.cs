using System.Text.Json.Serialization;

namespace Lupira.Contracts.Fires;

/// <summary>A deterministic, no-LLM action executed directly at fire time.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ActionKind>))]
public enum ActionKind
{
    SendCheckIn,
    Notify,
    CreateLinkedTask,
    ExpireTarget,
    RescheduleSelf,
    RunJob,
    Rescore,
}
