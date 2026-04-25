using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace DerlictEmpires.E2E;

/// <summary>
/// E2E tests verifying research and station systems are wired and ticking.
/// </summary>
[Collection("Godot")]
[Trait("Category", "Headless")]
public class ResearchStationTests : E2ETestBase
{
    public ResearchStationTests(GodotFixture fixture, ITestOutputHelper output) : base(fixture, output) { }

    [Fact]
    public async Task Research_InitializesWithTier1Unlocked()
    {
        // After loading, empire should have tier 1 of affinity color unlocked
        // with available subsystems to research
        if (ShouldSkip()) return;

        await Bridge.LoadStateFromFileAsync(FixturePath("research_game.json"));

        var saveResp = await Bridge.SaveStateAsync();
        var doc = JsonDocument.Parse(saveResp.GetProperty("json").GetString()!);
        var researchStates = doc.RootElement.GetProperty("researchStates");

        Assert.Equal(1, researchStates.GetArrayLength());

        var state = researchStates[0];
        Assert.Equal(0, state.GetProperty("empireId").GetInt32());

        // Should have available subsystems (tier 1 reveals 2 per category × 5 categories = 10)
        var available = state.GetProperty("availableSubsystems").GetArrayLength();
        Output.WriteLine($"Available subsystems: {available}");
        Assert.True(available > 0, "Should have available subsystems after tier 1 unlock");

        // Should have a current project (auto-started)
        var currentProject = state.GetProperty("currentProject").GetString();
        Output.WriteLine($"Current project: {currentProject}");
        Assert.NotNull(currentProject);
    }

    [Fact]
    public async Task Research_ProgressesOverSlowTicks()
    {
        // Research progress should increase when slow ticks fire
        if (ShouldSkip()) return;

        await Bridge.LoadStateFromFileAsync(FixturePath("research_game.json"));

        // Get initial progress
        var before = await Bridge.SaveStateAsync();
        var beforeDoc = JsonDocument.Parse(before.GetProperty("json").GetString()!);
        var beforeProgress = beforeDoc.RootElement
            .GetProperty("researchStates")[0]
            .GetProperty("currentProgress").GetSingle();

        Output.WriteLine($"Progress before ticks: {beforeProgress}");

        // Fire slow ticks (research processes on slow tick)
        await Bridge.TickAsync(slow: 3);

        // Check progress increased
        var after = await Bridge.SaveStateAsync();
        var afterDoc = JsonDocument.Parse(after.GetProperty("json").GetString()!);
        var afterProgress = afterDoc.RootElement
            .GetProperty("researchStates")[0]
            .GetProperty("currentProgress").GetSingle();

        Output.WriteLine($"Progress after 3 slow ticks: {afterProgress}");
        Assert.True(afterProgress > beforeProgress,
            $"Research progress should increase. Before: {beforeProgress}, After: {afterProgress}");
    }

    [Fact]
    public async Task Research_CompletesSubsystem()
    {
        // After enough slow ticks, a subsystem should complete
        // Tier 1 subsystem costs 20, research output is 5 per tick
        // So 4 slow ticks should complete it (5 * 1.0 efficiency * 4 = 20)
        if (ShouldSkip()) return;

        await Bridge.LoadStateFromFileAsync(FixturePath("research_game.json"));

        // Drain initial logs
        await Bridge.GetLogsAsync();

        // Fire enough ticks to complete a subsystem (20 cost / 5 output = 4 ticks)
        // Give some extra to be safe
        await Bridge.TickAsync(slow: 6);

        // Check logs for completion
        var logs = await Bridge.GetLogsAsync();
        var messages = new List<string>();
        foreach (var entry in logs.GetProperty("entries").EnumerateArray())
            messages.Add(entry.GetProperty("message").GetString() ?? "");

        var combined = string.Join("\n", messages);
        Output.WriteLine($"Logs:\n{combined}");

        Assert.Contains(messages, m => m.Contains("completed subsystem"));
    }

    [Fact]
    public async Task Research_StateSurvivesRoundTrip()
    {
        // Load, tick to make progress, save, load again, verify progress preserved
        if (ShouldSkip()) return;

        await Bridge.LoadStateFromFileAsync(FixturePath("research_game.json"));
        await Bridge.TickAsync(slow: 2);

        // Save state
        var saveResp = await Bridge.SaveStateAsync();
        var savedJson = saveResp.GetProperty("json").GetString()!;

        // Get progress from saved state
        var doc1 = JsonDocument.Parse(savedJson);
        var progress1 = doc1.RootElement
            .GetProperty("researchStates")[0]
            .GetProperty("currentProgress").GetSingle();

        // Re-load the saved state
        await Bridge.LoadStateFromJsonAsync(savedJson);

        // Save again and compare
        var saveResp2 = await Bridge.SaveStateAsync();
        var doc2 = JsonDocument.Parse(saveResp2.GetProperty("json").GetString()!);
        var progress2 = doc2.RootElement
            .GetProperty("researchStates")[0]
            .GetProperty("currentProgress").GetSingle();

        Output.WriteLine($"Progress after first save: {progress1}, after reload+save: {progress2}");
        Assert.InRange(progress2, progress1 - 0.1f, progress1 + 0.1f);
    }

    [Fact]
    public async Task Station_LoadsWithModules()
    {
        // research_game.json has a station with Shipyard + Defense modules
        if (ShouldSkip()) return;

        await Bridge.LoadStateFromFileAsync(FixturePath("research_game.json"));

        var saveResp = await Bridge.SaveStateAsync();
        var doc = JsonDocument.Parse(saveResp.GetProperty("json").GetString()!);
        var stations = doc.RootElement.GetProperty("stations");

        Assert.Equal(1, stations.GetArrayLength());

        var station = stations[0];
        Assert.Equal("Research Station", station.GetProperty("name").GetString());
        Assert.Equal(2, station.GetProperty("sizeTier").GetInt32());

        var modules = station.GetProperty("installedModules");
        Assert.Equal(2, modules.GetArrayLength());
        // Module names should round-trip as type names
        var moduleNames = new List<string>();
        foreach (var m in modules.EnumerateArray())
            moduleNames.Add(m.GetString()!);
        Assert.Contains("Shipyard", moduleNames);
        Assert.Contains("Defense", moduleNames);
    }

}
