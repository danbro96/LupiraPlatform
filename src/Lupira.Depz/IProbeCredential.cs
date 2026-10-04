namespace Lupira.Depz;

/// <summary>How a probe authenticates to one edge. A throw is reported as
/// <see cref="Contracts.Depz.DependencyStatus.NoCredential"/>, never as Down.</summary>
public interface IProbeCredential
{
    Task ApplyAsync(HttpRequestMessage request, HttpClient client, CancellationToken ct);
}
