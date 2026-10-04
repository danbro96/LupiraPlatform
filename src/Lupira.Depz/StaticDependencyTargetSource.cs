namespace Lupira.Depz;

public sealed class StaticDependencyTargetSource(IReadOnlyList<DependencyTarget> targets) : IDependencyTargetSource
{
    public IReadOnlyList<DependencyTarget> GetTargets() => targets;
}
