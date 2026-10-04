namespace Lupira.Contracts.Fires;

/// <summary>A reference the fired payload acts on. <c>Id</c> for Event/Contact/Task/Place; <c>Url</c> for External.</summary>
public sealed record Ref(RefKind Kind, Guid? Id, string? Url);
