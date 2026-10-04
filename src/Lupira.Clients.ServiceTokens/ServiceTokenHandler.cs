namespace Lupira.Clients.ServiceTokens;

/// <summary>Applies <see cref="ServiceTokenProvider"/> to every request on the client. A failed mint surfaces as
/// <see cref="HttpRequestException"/> so the caller's transport-failure handling covers it.</summary>
public sealed class ServiceTokenHandler(ServiceTokenProvider tokens, IOutboundHopOptions hop) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            await tokens.ApplyAsync(request, hop, cancellationToken);
        }
        catch (TokenEndpointException ex)
        {
            throw new HttpRequestException(ex.Message, ex);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
