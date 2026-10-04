using Marten;

namespace Lupira.Identity.Marten;

/// <summary><see cref="PrincipalDirectory{TPrincipal}"/> over the plain <see cref="Principal"/> document.</summary>
public sealed class PrincipalDirectory(IDocumentSession session) : PrincipalDirectory<Principal>(session);
