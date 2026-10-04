namespace Lupira.Contracts.Fires;

/// <summary>The 202 body of <c>POST /fires</c>. <c>Duplicate</c> = the dedupe key was already recorded (a lost-ack re-push).</summary>
public sealed class FireAcceptedResponse
{
    public required Guid InboundItemId { get; set; }

    public required bool Duplicate { get; set; }
}
