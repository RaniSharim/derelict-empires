using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace DerlictEmpires.E2E;

/// <summary>
/// Tests that verify complex game state round-trips correctly through save/load.
/// </summary>
[Collection("Godot")]
[Trait("Category", "Headless")]
public class GameStateTests : E2ETestBase
{
    public GameStateTests(GodotFixture fixture, ITestOutputHelper output) : base(fixture, output) { }

    [Fact]
    public async Task FleetOrders_SurviveRoundTrip()
    {
        // Fleet orders (in-transit fleets) should serialize and deserialize correctly
        if (ShouldSkip()) return;

        await Bridge.LoadStateFromFileAsync(FixturePath("fleet_orders_game.json"));

        var saveResp = await Bridge.SaveStateAsync();
        Assert.True(saveResp.GetProperty("ok").GetBoolean());

        var savedJson = saveResp.GetProperty("json").GetString()!;
        var doc = JsonDocument.Parse(savedJson);
        var root = doc.RootElement;

        // The fleet should be in transit (currentSystemId = -1)
        var fleets = root.GetProperty("fleets");
        Assert.Equal(1, fleets.GetArrayLength());
        Assert.Equal(-1, fleets[0].GetProperty("currentSystemId").GetInt32());
        Assert.Equal("Strike Force", fleets[0].GetProperty("name").GetString());

        // Fleet orders should be preserved
        var orders = root.GetProperty("fleetOrders");
        Assert.Equal(1, orders.GetArrayLength());

        var order = orders[0];
        Assert.Equal(0, order.GetProperty("fleetId").GetInt32());
        Assert.Equal(2, order.GetProperty("path").GetArrayLength());
        Assert.Equal(1, order.GetProperty("path")[0].GetInt32());
        Assert.Equal(2, order.GetProperty("path")[1].GetInt32());
        Assert.Equal(0, order.GetProperty("pathIndex").GetInt32());
        Assert.Equal(0, order.GetProperty("transitFromSystemId").GetInt32());

        // Lane progress should be close to 0.4 (may have slight float imprecision)
        var progress = order.GetProperty("laneProgress").GetSingle();
        Assert.InRange(progress, 0.39f, 0.41f);
    }

    [Fact]
    public async Task ResourceStockpiles_SurviveRoundTrip()
    {
        // Empire resource stockpiles with specific amounts should round-trip
        if (ShouldSkip()) return;

        await Bridge.LoadStateFromFileAsync(FixturePath("fleet_orders_game.json"));

        var saveResp = await Bridge.SaveStateAsync();
        var savedJson = saveResp.GetProperty("json").GetString()!;
        var doc = JsonDocument.Parse(savedJson);

        var empire = doc.RootElement.GetProperty("empires")[0];
        Assert.Equal("Player", empire.GetProperty("name").GetString());
        Assert.Equal(2500, empire.GetProperty("credits").GetInt64());

        // Resource stockpile should have our specific values
        var stockpile = empire.GetProperty("resourceStockpile");

        // Check specific resources we set in the fixture
        Assert.True(stockpile.TryGetProperty("Red_SimpleEnergy", out var redEnergy));
        Assert.InRange(redEnergy.GetSingle(), 150.0f, 151.0f);

        Assert.True(stockpile.TryGetProperty("Red_SimpleParts", out var redParts));
        Assert.InRange(redParts.GetSingle(), 79.5f, 80.5f);

        Assert.True(stockpile.TryGetProperty("Blue_AdvancedEnergy", out var blueEnergy));
        Assert.InRange(blueEnergy.GetSingle(), 24.5f, 25.5f);

        // Component stockpile
        var components = empire.GetProperty("componentStockpile");
        Assert.True(components.TryGetProperty("Red_Tier1", out var redTier1));
        Assert.InRange(redTier1.GetSingle(), 9.5f, 10.5f);
    }

    [Fact]
    public async Task LogsContainLoadMessages()
    {
        // After loading state, the logs should contain informational messages about the load
        if (ShouldSkip()) return;

        await Bridge.LoadStateFromFileAsync(FixturePath("minimal_game.json"));

        var resp = await Bridge.GetLogsAsync();
        Assert.True(resp.GetProperty("ok").GetBoolean());

        var entries = resp.GetProperty("entries");
        var allMessages = new System.Collections.Generic.List<string>();
        foreach (var entry in entries.EnumerateArray())
        {
            allMessages.Add(entry.GetProperty("message").GetString() ?? "");
        }

        var combined = string.Join("\n", allMessages);
        Output.WriteLine($"Logs after load:\n{combined}");

        // Should see the load message from MainScene.LoadGame
        Assert.Contains(allMessages, m => m.Contains("Loading game state"));
        // Should see the completion message
        Assert.Contains(allMessages, m => m.Contains("Game loaded"));
    }
}
