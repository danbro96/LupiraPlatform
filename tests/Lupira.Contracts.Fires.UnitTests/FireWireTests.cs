using System.Text.Json;
using Xunit;

namespace Lupira.Contracts.Fires.UnitTests;

public sealed class FireWireTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Enums_serialize_by_name_without_host_converters()
    {
        var json = JsonSerializer.Serialize(SampleFire(RefKind.Event, OutputKind.RecordEdit), Web);

        Assert.Contains("\"calendarClass\":\"System\"", json, StringComparison.Ordinal);
        Assert.Contains("\"calendarKind\":\"LlmPrompts\"", json, StringComparison.Ordinal);
        Assert.Contains("\"intent\":\"EnrichRecord\"", json, StringComparison.Ordinal);
        Assert.Contains("\"tier\":\"Small\"", json, StringComparison.Ordinal);
        Assert.Contains("\"onMiss\":\"Retry\"", json, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"AllDayAt\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Place_target_and_output_round_trip()
    {
        var json = JsonSerializer.Serialize(SampleFire(RefKind.Place, OutputKind.Place), Web);
        var back = JsonSerializer.Deserialize<FireRequest>(json, Web)!;

        Assert.Equal(RefKind.Place, back.Prompt!.Target!.Kind);
        Assert.Equal(OutputKind.Place, back.Prompt.Output);
        Assert.Equal(new TimeOnly(9, 0), back.Prompt.Fire.AllDayAt);
    }

    [Fact]
    public void Persisted_pascal_case_action_reads_back()
    {
        const string stored = """
            {"Kind":"SendCheckIn","Target":{"Kind":"External","Id":null,"Url":"https://x"},"ParamsJson":"{}","Fire":{"Kind":"Offset","OffsetMinutes":-30,"AllDayAt":null},"Enabled":true}
            """;

        var action = JsonSerializer.Deserialize<ItemAction>(stored)!;

        Assert.Equal(
            new ItemAction(ActionKind.SendCheckIn, new Ref(RefKind.External, null, "https://x"), "{}", new PromptFire(PromptFireKind.Offset, -30, null), true),
            action);
        Assert.Equal(stored.Trim(), JsonSerializer.Serialize(action));
    }

    [Fact]
    public void Accepted_response_round_trips()
    {
        var json = """{"inboundItemId":"33333333-3333-3333-3333-333333333333","duplicate":true}""";

        var body = JsonSerializer.Deserialize<FireAcceptedResponse>(json, Web)!;

        Assert.True(body.Duplicate);
        Assert.Equal(json, JsonSerializer.Serialize(body, Web));
    }

    private static FireRequest SampleFire(RefKind targetKind, OutputKind output) => new()
    {
        PrincipalId = "p1",
        ItemId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        CalendarId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        CalendarClass = CalendarClass.System,
        CalendarKind = CalendarKind.LlmPrompts,
        OccurrenceAt = new DateTimeOffset(2026, 10, 4, 9, 0, 0, TimeSpan.Zero),
        DedupeKey = "k1",
        Prompt = new ItemPrompt(
            PromptIntent.EnrichRecord,
            new Ref(targetKind, Guid.Empty, null),
            "fill in",
            output,
            null,
            ModelTier.Small,
            FallbackMode.Retry,
            new PromptFire(PromptFireKind.AllDayAt, null, new TimeOnly(9, 0)),
            Enabled: true),
    };
}
