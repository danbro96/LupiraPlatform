using System.Diagnostics;
using JasperFx.Events;
using Xunit;

namespace Lupira.Identity.Marten.UnitTests;

public sealed class EventActorTests
{
    private sealed record Happened;

    [Fact]
    public void Of_reads_the_actor_header()
    {
        var stamped = Event.For(new Happened());
        stamped.Headers = new Dictionary<string, object> { [EventActor.HeaderKey] = "p-1" };

        Assert.Equal("p-1", EventActor.Of(stamped));
        Assert.Null(EventActor.Of(Event.For(new Happened())));
    }

    [Fact]
    public void Principal_stamp_sets_last_modified_by_email_source_and_trace()
    {
        using var store = OfflineStore.Create();
        using var session = store.LightweightSession();
        using var activity = new Activity("test").Start();
        var principal = new Principal { Id = Guid.NewGuid(), Email = "p@x.test" };

        EventActor.Stamp(session, principal, EventActor.SourceDav);

        Assert.Equal(principal.Id.ToString(), session.LastModifiedBy);
        Assert.Equal("p@x.test", session.GetHeader(EventActor.EmailHeaderKey));
        Assert.Equal("dav", session.GetHeader(EventActor.SourceHeaderKey));
        Assert.Null(session.GetHeader(EventActor.HeaderKey));
        Assert.Equal(activity.TraceId.ToString(), session.CorrelationId);
        Assert.Equal(activity.SpanId.ToString(), session.CausationId);
    }

    [Fact]
    public void Command_stamp_sets_the_actor_header_and_command_causation()
    {
        using var store = OfflineStore.Create();
        using var session = store.LightweightSession();
        var commandId = Guid.NewGuid();

        EventActor.Stamp(session, "share:family", null, commandId);

        Assert.Equal("share:family", session.GetHeader(EventActor.HeaderKey));
        Assert.Equal("share:family", session.LastModifiedBy);
        Assert.Null(session.GetHeader(EventActor.EmailHeaderKey));
        Assert.Equal("api", session.GetHeader(EventActor.SourceHeaderKey));
        Assert.Equal(commandId.ToString(), session.CausationId);
    }
}
