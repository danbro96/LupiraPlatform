using System.Reflection;
using JasperFx.Events;
using Marten;
using EventOperations = Marten.Events.IEventStoreOperations;

namespace Lupira.Marten.Idempotency.UnitTests;

public class SessionFake : DispatchProxy
{
    public List<object> Inserted { get; } = [];

    public List<(Guid Stream, object[] Events)> Appended { get; } = [];

    public List<string> Calls { get; } = [];

    public Dictionary<Guid, ProcessedCommand> Ledger { get; } = [];

    public long? StreamVersion { get; set; }

    public Exception? SaveFailure { get; set; }

    public static (IDocumentSession Session, SessionFake Fake) Create()
    {
        var session = DispatchProxy.Create<IDocumentSession, SessionFake>();
        return (session, (SessionFake) (object) session);
    }

    public object? InvokeEvents(MethodInfo? targetMethod, object?[]? args) => Invoke(targetMethod, args);

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        Calls.Add(targetMethod!.Name);
        switch (targetMethod.Name)
        {
            case nameof(IDocumentSession.Insert):
                Inserted.AddRange((IEnumerable<object>) args![0]!);
                return null;
            case nameof(IDocumentSession.SaveChangesAsync):
                return SaveFailure is null ? Task.CompletedTask : Task.FromException(SaveFailure);
            case nameof(IDocumentSession.LoadAsync):
                return Task.FromResult(Ledger.GetValueOrDefault((Guid) args![0]!));
            case "get_Events":
                var events = DispatchProxy.Create<EventOperations, EventsFake>();
                ((EventsFake) (object) events).Session = this;
                return events;
            case nameof(EventOperations.FetchStreamStateAsync):
                return Task.FromResult(StreamVersion is { } v ? new StreamState((Guid) args![0]!, v, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow) : null);
            case nameof(EventOperations.Append):
                Appended.Add(((Guid) args![0]!, (object[]) args[1]!));
                return null;
            default:
                throw new NotSupportedException(targetMethod.ToString());
        }
    }
}
