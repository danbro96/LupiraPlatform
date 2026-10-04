using System.Text.Json.Serialization;

namespace Lupira.Contracts.Fires;

/// <summary>Model size tier; the LLM gateway maps it to a concrete model. Vendor-neutral and durable across gateway model swaps.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ModelTier>))]
public enum ModelTier
{
    Small,
    Medium,
    Large,
}
