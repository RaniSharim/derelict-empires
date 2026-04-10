using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace DerlictEmpires.E2E;

/// <summary>
/// Non-visual E2E tests that verify save/load state via the McpBridge.
/// These run headless — no screenshots needed.
/// </summary>
[Collection("Godot")]
[Trait("Category", "Headless")]
public class StateLoadTests : E2ETestBase
{
    public StateLoadTests(GodotFixture fixture, ITestOutputHelper output) : base(fixture, output) { }

    [Fact]
    public async Task LoadState_MinimalGame_Succeeds()
    {
        if (ShouldSkip()) return;

        var path = FixturePath("minimal_game.json");
        var resp = await Bridge.LoadStateFromFileAsync(path);

        Assert.True(resp.GetProperty("ok").GetBoolean(), $"load_state failed: {resp}");
        Assert.Equal(2, resp.GetProperty("empires").GetInt32());
        Assert.Equal(2, resp.GetProperty("fleets").GetInt32());
        Assert.Equal(3, resp.GetProperty("systems").GetInt32());
    }

    [Fact]
    public async Task LoadState_SceneTreeHasExpectedStructure()
    {
        if (ShouldSkip()) return;

        var path = FixturePath("minimal_game.json");
        await Bridge.LoadStateFromFileAsync(path);

        var resp = await Bridge.GetSceneTreeAsync();
        Assert.True(resp.GetProperty("ok").GetBoolean());

        var treeJson = resp.GetProperty("tree").GetRawText();

        Assert.Contains("MainScene", treeJson);
        Assert.Contains("GalaxyMap", treeJson);
        Assert.Contains("Fleets", treeJson);
        Assert.Contains("CameraRig", treeJson);
    }

    [Fact]
    public async Task LoadState_FleetNodesCreated()
    {
        if (ShouldSkip()) return;

        var path = FixturePath("minimal_game.json");
        await Bridge.LoadStateFromFileAsync(path);

        var resp = await Bridge.GetSceneTreeAsync();
        Assert.True(resp.GetProperty("ok").GetBoolean());

        var treeJson = resp.GetProperty("tree").GetRawText();
        Assert.Contains("1st Fleet", treeJson);
        Assert.Contains("AI Fleet", treeJson);
    }

    [Fact]
    public async Task LoadState_NoRuntimeErrors()
    {
        if (ShouldSkip()) return;

        var path = FixturePath("minimal_game.json");
        await Bridge.LoadStateFromFileAsync(path);

        var resp = await Bridge.GetLogsAsync();
        Assert.True(resp.GetProperty("ok").GetBoolean());

        var entries = resp.GetProperty("entries");
        foreach (var entry in entries.EnumerateArray())
        {
            var level = entry.GetProperty("level").GetString();
            Assert.NotEqual("Error", level);
        }
    }

    [Fact]
    public async Task SaveState_RoundTrips()
    {
        if (ShouldSkip()) return;

        var path = FixturePath("minimal_game.json");
        await Bridge.LoadStateFromFileAsync(path);

        var saveResp = await Bridge.SaveStateAsync();
        Assert.True(saveResp.GetProperty("ok").GetBoolean());

        var savedJson = saveResp.GetProperty("json").GetString();
        Assert.NotNull(savedJson);

        var doc = JsonDocument.Parse(savedJson!);
        var root = doc.RootElement;

        Assert.Equal(99, root.GetProperty("masterSeed").GetInt32());
        Assert.Equal(2, root.GetProperty("empires").GetArrayLength());
        Assert.Equal(3, root.GetProperty("galaxy").GetProperty("systems").GetArrayLength());
        Assert.Equal(2, root.GetProperty("fleets").GetArrayLength());
        Assert.Equal(3, root.GetProperty("ships").GetArrayLength());
    }

    [Fact]
    public async Task LoadState_EmpireDataPreserved()
    {
        if (ShouldSkip()) return;

        var path = FixturePath("minimal_game.json");
        await Bridge.LoadStateFromFileAsync(path);

        var saveResp = await Bridge.SaveStateAsync();
        Assert.True(saveResp.GetProperty("ok").GetBoolean());

        var savedJson = saveResp.GetProperty("json").GetString()!;
        var doc = JsonDocument.Parse(savedJson);
        var empires = doc.RootElement.GetProperty("empires");

        var humanEmpire = empires[0];
        Assert.Equal("Human Empire", humanEmpire.GetProperty("name").GetString());
        Assert.True(humanEmpire.GetProperty("isHuman").GetBoolean());
        Assert.Equal(0, humanEmpire.GetProperty("homeSystemId").GetInt32());

        var aiEmpire = empires[1];
        Assert.Equal("AI Empire", aiEmpire.GetProperty("name").GetString());
        Assert.False(aiEmpire.GetProperty("isHuman").GetBoolean());
    }

    [Fact]
    public async Task LoadState_GalaxyTopologyPreserved()
    {
        if (ShouldSkip()) return;

        var path = FixturePath("minimal_game.json");
        await Bridge.LoadStateFromFileAsync(path);

        var saveResp = await Bridge.SaveStateAsync();
        var savedJson = saveResp.GetProperty("json").GetString()!;
        var doc = JsonDocument.Parse(savedJson);
        var galaxy = doc.RootElement.GetProperty("galaxy");

        Assert.Equal(3, galaxy.GetProperty("systems").GetArrayLength());
        Assert.Equal(2, galaxy.GetProperty("lanes").GetArrayLength());

        var systems = galaxy.GetProperty("systems");
        Assert.Equal("Alpha", systems[0].GetProperty("name").GetString());
        Assert.Equal("Beta", systems[1].GetProperty("name").GetString());
        Assert.Equal("Gamma", systems[2].GetProperty("name").GetString());
    }
}
