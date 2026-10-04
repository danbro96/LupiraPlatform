using System.Diagnostics;
using Lupira.Contracts.Depz;

namespace Lupira.Depz;

/// <summary>One edge probe: apply the target's credential, GET its probe path, map the outcome. Uses its own
/// named client so probe traffic never rides the real clients.</summary>
internal sealed class DependencyProbe(IHttpClientFactory httpFactory, DependencyTelemetry telemetry)
{
    public const string ProbeClientName = "depz-probe";

    public async Task<DependencyDto> ProbeAsync(DependencyTarget target, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(target.BaseUrl))
            return Result(target, DependencyStatus.Unconfigured, error: "no base URL configured");

        var client = httpFactory.CreateClient(ProbeClientName);
        var baseUrl = target.BaseUrl.EndsWith('/') ? target.BaseUrl : target.BaseUrl + "/";
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(baseUrl), target.ProbePath));

        if (target.Credential is { } credential)
        {
            try
            {
                await credential.ApplyAsync(request, client, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result(target, DependencyStatus.NoCredential, error: ex.Message);
            }
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await client.SendAsync(request, ct);
            stopwatch.Stop();
            var status = (int) response.StatusCode switch
            {
                >= 200 and < 300 => DependencyStatus.Healthy,
                401 or 403 => DependencyStatus.Unauthorized,
                _ => DependencyStatus.Degraded,
            };
            var error = status == DependencyStatus.Healthy ? null : $"{target.ProbePath} returned {(int) response.StatusCode}";
            return Result(target, status, stopwatch.Elapsed.TotalMilliseconds, error);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            stopwatch.Stop();
            return Result(target, DependencyStatus.Down, stopwatch.Elapsed.TotalMilliseconds, ex.Message);
        }
    }

    private DependencyDto Result(DependencyTarget target, DependencyStatus status, double? latencyMs = null, string? error = null)
    {
        telemetry.Record(target.Name, status, latencyMs);
        return new DependencyDto
        {
            Name = target.Name,
            Status = status,
            LatencyMs = latencyMs,
            Error = error,
            CheckedUtc = DateTimeOffset.UtcNow,
        };
    }
}
