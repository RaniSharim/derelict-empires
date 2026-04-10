using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace DerlictEmpires.E2E;

/// <summary>
/// Tests that verify scene tree structure and node counts after loading state.
/// </summary>
[Collection("Godot")]
[Trait("Category", "Headless")]
public class SceneIntegrityTests : E2ETestBase
{
    public SceneIntegrityTests(GodotFixture fixture, ITestOutputHelper output) : base(fixture, output) { }

    [Fact]
    public async Task FindNodes_StarSystemNodeCount_MatchesGalaxy()
    {
        // Verify that the scene tree has one StarSystemNode per system in the galaxy
        if (ShouldSkip()) return;

        var path = FixturePath("minimal_game.json");
        await Bridge.LoadStateFromFileAsync(path);

        // The fixture has 3 systems, so we expect 3 StarSystemNode instances
        var resp = await Bridge.FindNodesAsync("Area3D");
        Assert.True(resp.GetProperty("ok").GetBoolean());

        // StarSystemNodes are Area3D — count them under the SystemNodes container
        var tree = await Bridge.GetSceneTreeAsync();
        var treeJson = tree.GetProperty("tree").GetRawText();

        // Each system gets a node named System_{id}
        Assert.Contains("System_0", treeJson);
        Assert.Contains("System_1", treeJson);
        Assert.Contains("System_2", treeJson);
    }

    [Fact]
    public async Task FleetNodeCount_MatchesFleetData()
    {
        // After loading, the number of fleet visual nodes should equal the number of fleets
        if (ShouldSkip()) return;

        var path = FixturePath("minimal_game.json");
        await Bridge.LoadStateFromFileAsync(path);

        var tree = await Bridge.GetSceneTreeAsync();
        var treeJson = tree.GetProperty("tree").GetRawText();

        // minimal_game.json has 2 fleets: "1st Fleet" and "AI Fleet"
        // FleetNode names include the fleet name
        int fleetCount = CountOccurrences(treeJson, "Fleet_");
        Assert.Equal(2, fleetCount);
    }

    [Fact]
    public async Task EmptyGame_LoadsWithNoFleetsOrColonies()
    {
        // An empty game (no fleets, colonies, stations) should load cleanly
        if (ShouldSkip()) return;

        var path = FixturePath("empty_game.json");
        var resp = await Bridge.LoadStateFromFileAsync(path);
        Assert.True(resp.GetProperty("ok").GetBoolean());
        Assert.Equal(1, resp.GetProperty("empires").GetInt32());
        Assert.Equal(0, resp.GetProperty("fleets").GetInt32());
        Assert.Equal(1, resp.GetProperty("systems").GetInt32());

        // Scene tree should have GalaxyMap but no fleet nodes
        var tree = await Bridge.GetSceneTreeAsync();
        var treeJson = tree.GetProperty("tree").GetRawText();

        Assert.Contains("GalaxyMap", treeJson);
        Assert.Contains("System_0", treeJson);
        Assert.DoesNotContain("Fleet_", treeJson);

        // No runtime errors
        var logs = await Bridge.GetLogsAsync();
        foreach (var entry in logs.GetProperty("entries").EnumerateArray())
            Assert.NotEqual("Error", entry.GetProperty("level").GetString());
    }

    [Fact]
    public async Task ReloadSameState_NoDuplicateNodes()
    {
        // Loading the same state twice should clean up old nodes (no duplicates)
        if (ShouldSkip()) return;

        var path = FixturePath("minimal_game.json");

        // Load twice
        await Bridge.LoadStateFromFileAsync(path);
        await Bridge.LoadStateFromFileAsync(path);

        var tree = await Bridge.GetSceneTreeAsync();
        var treeJson = tree.GetProperty("tree").GetRawText();

        // Should still only have 2 fleet nodes, not 4
        int fleetCount = CountOccurrences(treeJson, "Fleet_");
        Assert.Equal(2, fleetCount);

        // Should still only have 3 system nodes
        Assert.Contains("System_0", treeJson);
        Assert.Contains("System_1", treeJson);
        Assert.Contains("System_2", treeJson);
    }

    [Fact]
    public async Task SequentialLoads_SecondStateReplaceFirst()
    {
        // Loading a different state should completely replace the first one
        if (ShouldSkip()) return;

        // First: load minimal (3 systems, 2 fleets)
        await Bridge.LoadStateFromFileAsync(FixturePath("minimal_game.json"));

        // Second: load empty (1 system, 0 fleets)
        await Bridge.LoadStateFromFileAsync(FixturePath("empty_game.json"));

        var tree = await Bridge.GetSceneTreeAsync();
        var treeJson = tree.GetProperty("tree").GetRawText();

        // Old fleets should be gone
        Assert.DoesNotContain("Fleet_", treeJson);
        Assert.DoesNotContain("1st Fleet", treeJson);

        // Only the single system from empty_game should exist
        Assert.Contains("System_0", treeJson);
        Assert.DoesNotContain("System_1", treeJson);
        Assert.DoesNotContain("System_2", treeJson);

        // Verify via save that the state is actually the empty one
        var saveResp = await Bridge.SaveStateAsync();
        var savedJson = saveResp.GetProperty("json").GetString()!;
        var doc = JsonDocument.Parse(savedJson);
        Assert.Equal(1, doc.RootElement.GetProperty("masterSeed").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("galaxy").GetProperty("systems").GetArrayLength());
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int idx = 0;
        while ((idx = text.IndexOf(pattern, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += pattern.Length;
        }
        return count;
    }
}
