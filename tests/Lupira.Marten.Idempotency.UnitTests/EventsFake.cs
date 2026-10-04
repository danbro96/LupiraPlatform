using System.Reflection;

namespace Lupira.Marten.Idempotency.UnitTests;

public class EventsFake : DispatchProxy
{
    public SessionFake Session { get; set; } = null!;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Session.InvokeEvents(targetMethod, args);
}
