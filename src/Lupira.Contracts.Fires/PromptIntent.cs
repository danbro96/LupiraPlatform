using System.Text.Json.Serialization;

namespace Lupira.Contracts.Fires;

/// <summary>What an LLM-interpreted run should accomplish.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PromptIntent>))]
public enum PromptIntent
{
    EnrichRecord,
    Research,
    CreateFollowUp,
    Monitor,
    Summarise,
    AskUser,
}
