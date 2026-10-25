namespace Lupira.Results;

/// <summary>A no-content operation outcome (e.g. delete).</summary>
public readonly record struct OpResult(OpStatus Status, string? Error)
{
    public string? Type { get; init; }

    public bool IsOk => Status == OpStatus.Ok;

    public static OpResult Ok() => new(OpStatus.Ok, null);

    public static OpResult NotFound() => new(OpStatus.NotFound, null);

    public static OpResult Forbidden(string error) => new(OpStatus.Forbidden, error);

    public static OpResult Forbidden(string error, string type) => new(OpStatus.Forbidden, error) { Type = type };

    public static OpResult Invalid(string error) => new(OpStatus.Invalid, error);

    public static OpResult Invalid(string error, string type) => new(OpStatus.Invalid, error) { Type = type };

    public static OpResult Conflict(string error) => new(OpStatus.Conflict, error);

    public static OpResult Conflict(string error, string type) => new(OpStatus.Conflict, error) { Type = type };

    public static OpResult Unavailable(string error) => new(OpStatus.Unavailable, error);

    public static OpResult Unavailable(string error, string type) => new(OpStatus.Unavailable, error) { Type = type };
}
