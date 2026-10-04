namespace Lupira.Bff.Proxy.UnitTests;

internal static class Fixture
{
    public static string Json(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", $"{name}.exposed.json"));

    public static ExposedSurface Surface(string name) => ExposedSurface.Parse(Json(name));
}
