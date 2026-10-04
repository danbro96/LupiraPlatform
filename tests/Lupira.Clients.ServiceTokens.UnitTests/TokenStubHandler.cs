using System.Net;

namespace Lupira.Clients.ServiceTokens.UnitTests;

/// <summary>Records every token-endpoint request and answers with <see cref="Respond"/>.</summary>
internal sealed class TokenStubHandler : HttpMessageHandler
{
    public List<Dictionary<string, string>> Forms { get; } = [];

    public List<Uri?> Uris { get; } = [];

    public Func<Dictionary<string, string>, (HttpStatusCode Status, string Body)> Respond { get; set; } =
        form => (HttpStatusCode.OK, $$"""{"access_token":"tok-{{form.GetValueOrDefault("audience") ?? form.GetValueOrDefault("scope") ?? form["client_id"]}}","expires_in":300}""");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var form = ParseForm(await request.Content!.ReadAsStringAsync(ct));
        lock (Forms)
        {
            Forms.Add(form);
            Uris.Add(request.RequestUri);
        }

        var (status, body) = Respond(form);
        return new HttpResponseMessage(status) { Content = new StringContent(body) };
    }

    private static Dictionary<string, string> ParseForm(string raw) =>
        raw.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(kv => Decode(kv[0]), kv => kv.Length > 1 ? Decode(kv[1]) : string.Empty);

    private static string Decode(string s) => Uri.UnescapeDataString(s.Replace('+', ' '));
}
