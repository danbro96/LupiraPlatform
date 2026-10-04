namespace Lupira.Depz;

/// <summary>The roster the poller sweeps. Built once and reused, so credentials keep their token caches.</summary>
public interface IDependencyTargetSource
{
    IReadOnlyList<DependencyTarget> GetTargets();
}
