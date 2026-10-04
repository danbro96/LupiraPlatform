using System.Security.Cryptography;
using System.Text;

namespace Lupira.Depz;

public static class ProbeKey
{
    public const string HeaderName = "X-Probe-Key";

    /// <summary>Constant-time; a blank configured key matches nothing.</summary>
    public static bool Matches(string? configured, string? presented) =>
        !string.IsNullOrEmpty(configured)
        && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(configured), Encoding.UTF8.GetBytes(presented ?? string.Empty));
}
