using System.Text.Json.Serialization;

namespace Lupira.Contracts.Fires;

/// <summary>Whether a calendar is part of the user's agenda (Agenda) or agent-managed system scaffolding (System).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CalendarClass>))]
public enum CalendarClass
{
    Agenda,
    System,
}
