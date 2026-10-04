namespace Lupira.Identity.Marten.UnitTests.Extended;

public sealed class Principal : Marten.Principal
{
    public Guid? ContactId { get; set; }
}
