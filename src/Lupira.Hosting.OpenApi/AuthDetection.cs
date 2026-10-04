namespace Lupira.Hosting.OpenApi;

/// <summary>How an operation is judged to require authentication.</summary>
public enum AuthDetection
{
    /// <summary>The endpoint carries authorize data and no allow-anonymous.</summary>
    AuthorizeData,

    /// <summary>Closed by default: auth comes from the fallback policy, which leaves no authorize data to find.</summary>
    NotAllowAnonymous,
}
