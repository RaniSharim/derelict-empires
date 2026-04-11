using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace DerlictEmpires.E2E;

/// <summary>
/// E2E tests that exercise actual game systems by loading state,
/// firing ticks, and verifying results through saved state and logs.
/// </summary>
[Collection("Godot")]
[Trait("Category", "Headless")]
public class GameLogicTests : E2ETestBase
{
    public GameLogicTests(GodotFixture fixture, ITestOutputHelper output) : base(fixture, output) { }

    [Fact]
    public async Task Tick_AdvancesGameTime()
    {
        if (ShouldSkip()) return;

        await Bridge.LoadStateFromFileAsync(FixturePath("minimal_game.json"));

        // Game starts at time 0.0
        var tickResp = await Bridge.TickAsync(fast: 10, slow: 2);
        Assert.True(tickResp.GetProperty("ok").GetBoolean());

        // 10 fast ticks = 1.0s, 2 slow ticks = 2.0s → total 3.0s
        var gameTime = tickResp.GetProperty("gameTime").GetDouble();
        Assert.InRange(gameTime, 2.9, 3.1);
    }

    [Fact]
    public async Task FleetMovement_FleetArrivesAfterTicks()
    {
        // Fleet at 40% through a 25-unit lane at speed 10.
        // Travel time for full lane = 25/10 = 2.5s.
        // Remaining = 60% → 1.5s → 15 fast ticks (0.1s each).
        // Fire 20 ticks to be safe (overshoot), fleet should arrive at system 1.
        if (ShouldSkip()) return;

        await Bridge.LoadStateFromFileAsync(FixturePath("fleet_orders_game.json"));

        // Verify fleet starts in transit
        var beforeSave = await Bridge.SaveStateAsync();
        var beforeJson = JsonDocument.Parse(beforeSave.GetProperty("json").GetString()!);
        Assert.Equal(-1, beforeJson.RootElement.GetProperty("fleets")[0].GetProperty("currentSystemId").GetInt32());

        // Fire enough fast ticks for the fleet to arrive at the next waypoint
        await Bridge.TickAsync(fast: 20);

        // Check state — fleet should have arrived at system 1 (first waypoint)
        var afterSave = await Bridge.SaveStateAsync();
        var afterJson = JsonDocument.Parse(afterSave.GetProperty("json").GetString()!);
        var fleet = afterJson.RootElement.GetProperty("fleets")[0];

        var currentSystem = fleet.GetProperty("currentSystemId").GetInt32();
        Output.WriteLine($"Fleet currentSystemId after 20 fast ticks: {currentSystem}");

        // Fleet should no longer be at -1 (in transit from system 0)
        // It should be at system 1 or further (possibly system 2 if it moved fast enough)
        Assert.NotEqual(-1, currentSystem);
        Assert.True(currentSystem == 1 || currentSystem == 2,
            $"Fleet should be at system 1 or 2, but was at {currentSystem}");
    }

    [Fact]
    public async Task FleetArrival_LoggedInMcpLogs()
    {
        if (ShouldSkip()) return;

        await Bridge.LoadStateFromFileAsync(FixturePath("fleet_orders_game.json"));

        // Drain any logs from the load
        await Bridge.GetLogsAsync();

        // Tick until the fleet arrives
        await Bridge.TickAsync(fast: 20);

        // Check logs for arrival message
        var logs = await Bridge.GetLogsAsync();
        var entries = logs.GetProperty("entries");
        var messages = new List<string>();
        foreach (var entry in entries.EnumerateArray())
            messages.Add(entry.GetProperty("message").GetString() ?? "");

        var combined = string.Join("\n", messages);
        Output.WriteLine($"Logs after ticks:\n{combined}");

        // MainScene logs "[Fleet] {name} arrived at {system}" on arrival
        Assert.Contains(messages, m => m.Contains("arrived at"));
    }

    [Fact]
    public async Task ResourceExtraction_IncreasesStockpile()
    {
        // minimal_game.json has extraction assignment: empire 0, POI 0, deposit 0
        // Deposit: Red SimpleEnergy, rate 2.0, remaining 1000
        // After slow ticks, empire 0 should have more resources
        if (ShouldSkip()) return;

        await Bridge.LoadStateFromFileAsync(FixturePath("minimal_game.json"));

        // Get initial stockpile (should be empty/zero)
        var beforeSave = await Bridge.SaveStateAsync();
        var beforeJson = JsonDocument.Parse(beforeSave.GetProperty("json").GetString()!);
        var beforeStockpile = beforeJson.RootElement.GetProperty("empires")[0].GetProperty("resourceStockpile");

        float beforeAmount = 0;
        if (beforeStockpile.TryGetProperty("Red_SimpleEnergy", out var val))
            beforeAmount = val.GetSingle();

        Output.WriteLine($"Before ticks: Red_SimpleEnergy = {beforeAmount}");

        // Fire 5 slow ticks (5 seconds of game time)
        // Extraction rate = 2.0 * 1.0 (efficiency) * 1 (workers) * 1.0 (tick delta) = 2.0 per tick
        // After 5 ticks: should gain ~10.0 units
        await Bridge.TickAsync(slow: 5);

        var afterSave = await Bridge.SaveStateAsync();
        var afterJson = JsonDocument.Parse(afterSave.GetProperty("json").GetString()!);
        var afterStockpile = afterJson.RootElement.GetProperty("empires")[0].GetProperty("resourceStockpile");

        float afterAmount = 0;
        if (afterStockpile.TryGetProperty("Red_SimpleEnergy", out var afterVal))
            afterAmount = afterVal.GetSingle();

        Output.WriteLine($"After 5 slow ticks: Red_SimpleEnergy = {afterAmount}");

        // Should have gained resources
        Assert.True(afterAmount > beforeAmount,
            $"Expected resources to increase. Before: {beforeAmount}, After: {afterAmount}");

        // Should have gained approximately 10.0 (2.0 rate × 5 ticks × 1.0 delta)
        float gained = afterAmount - beforeAmount;
        Assert.InRange(gained, 8.0f, 12.0f);
    }

    [Fact]
    public async Task FleetMovement_CompletesFullPath()
    {
        // fleet_orders_game has a fleet with path [1, 2] (two hops).
        // Lane 0→1 = 25 units, Lane 1→2 = 27 units. Speed = 10.
        // Total travel time ≈ (25*0.6 + 27) / 10 = 4.2s → 42 fast ticks
        // Give it 60 ticks to be safe — fleet should end up at system 2.
        if (ShouldSkip()) return;

        await Bridge.LoadStateFromFileAsync(FixturePath("fleet_orders_game.json"));
        await Bridge.TickAsync(fast: 60);

        var saveResp = await Bridge.SaveStateAsync();
        var doc = JsonDocument.Parse(saveResp.GetProperty("json").GetString()!);
        var fleet = doc.RootElement.GetProperty("fleets")[0];

        var currentSystem = fleet.GetProperty("currentSystemId").GetInt32();
        Output.WriteLine($"Fleet at system {currentSystem} after 60 fast ticks");

        // Fleet should have completed its path and be at system 2
        Assert.Equal(2, currentSystem);

        // Fleet orders should be empty (order completed)
        var orders = doc.RootElement.GetProperty("fleetOrders");
        Assert.Equal(0, orders.GetArrayLength());
    }
}
