using Xunit;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Tests;

public class GameSetupTests
{
    private static GalaxyData MakeGalaxy(int seed = 42)
    {
        return GalaxyGenerator.Generate(new GalaxyGenerationConfig
        {
            Seed = seed,
            TotalSystems = 100,
            ArmCount = 4,
            GalaxyRadius = 200f,
            MaxLaneLength = 60f,
            MinNeighbors = 2,
            MaxNeighbors = 4,
            HiddenLaneRatio = 0.15f
        });
    }

    [Fact]
    public void CreatePlayerEmpire_SetsBasicProperties()
    {
        var galaxy = MakeGalaxy();
        var setup = new GameSetupManager();
        var result = new GameSetupManager.SetupResult();
        var rng = new GameRandom(42);

        var empire = setup.CreatePlayerEmpire("Test", PrecursorColor.Red, Origin.Warriors, galaxy, result, rng);

        Assert.Equal("Test", empire.Name);
        Assert.True(empire.IsHuman);
        Assert.Equal(PrecursorColor.Red, empire.Affinity);
        Assert.Equal(Origin.Warriors, empire.Origin);
        Assert.True(empire.HomeSystemId >= 0);
    }

    [Fact]
    public void CreatePlayerEmpire_SpawnsColonyAtHome()
    {
        var galaxy = MakeGalaxy();
        var setup = new GameSetupManager();
        var result = new GameSetupManager.SetupResult();
        var rng = new GameRandom(42);

        setup.CreatePlayerEmpire("Test", PrecursorColor.Blue, Origin.Servitors, galaxy, result, rng);

        Assert.Single(result.Colonies);
        Assert.Equal(0, result.Colonies[0].OwnerEmpireId);
        Assert.True(result.Colonies[0].Population > 0);
    }

    [Fact]
    public void CreatePlayerEmpire_SpawnsStationWithShipyard()
    {
        var galaxy = MakeGalaxy();
        var setup = new GameSetupManager();
        var result = new GameSetupManager.SetupResult();
        var rng = new GameRandom(42);

        setup.CreatePlayerEmpire("Test", PrecursorColor.Green, Origin.Haulers, galaxy, result, rng);

        Assert.Single(result.Stations);
        Assert.True(result.Stations[0].HasShipyard);
    }

    [Theory]
    [InlineData(Origin.Warriors, 5)]    // 4 base + 1 extra Fighter
    [InlineData(Origin.Servitors, 5)]   // 4 base + 1 extra Salvager
    [InlineData(Origin.Haulers, 5)]     // 4 base + 1 extra Scout
    [InlineData(Origin.Chroniclers, 5)] // 4 base + 1 extra Scout
    [InlineData(Origin.FreeRace, 5)]    // 4 base + 1 extra Builder
    public void CreatePlayerEmpire_SpawnsCorrectShipCount(Origin origin, int expectedShips)
    {
        var galaxy = MakeGalaxy();
        var setup = new GameSetupManager();
        var result = new GameSetupManager.SetupResult();
        var rng = new GameRandom(42);

        setup.CreatePlayerEmpire("Test", PrecursorColor.Red, origin, galaxy, result, rng);

        Assert.Equal(expectedShips, result.Ships.Count);
        Assert.Single(result.Fleets);
        Assert.Equal(expectedShips, result.Fleets[0].ShipIds.Count);
    }

    [Fact]
    public void CreatePlayerEmpire_GivesStartingResources()
    {
        var galaxy = MakeGalaxy();
        var setup = new GameSetupManager();
        var result = new GameSetupManager.SetupResult();
        var rng = new GameRandom(42);

        setup.CreatePlayerEmpire("Test", PrecursorColor.Red, Origin.Warriors, galaxy, result, rng);

        var empire = result.Empires[0];
        Assert.True(empire.GetResource(PrecursorColor.Red, ResourceType.SimpleEnergy) > 0);
        Assert.True(empire.GetResource(PrecursorColor.Red, ResourceType.BasicComponent) > 0);
        Assert.True(empire.Credits > 0);
    }

    [Fact]
    public void FreeRace_HasNoAffinity()
    {
        var galaxy = MakeGalaxy();
        var setup = new GameSetupManager();
        var result = new GameSetupManager.SetupResult();
        var rng = new GameRandom(42);

        setup.CreatePlayerEmpire("Free", null, Origin.FreeRace, galaxy, result, rng);

        Assert.Null(result.Empires[0].Affinity);
    }

    [Fact]
    public void CreateMultipleEmpires_GetDifferentHomeSystems()
    {
        var galaxy = MakeGalaxy();
        var setup = new GameSetupManager();
        var result = new GameSetupManager.SetupResult();
        var rng = new GameRandom(42);

        setup.CreatePlayerEmpire("Player", PrecursorColor.Red, Origin.Warriors, galaxy, result, rng.DeriveChild(0));
        setup.CreateAIEmpire(galaxy, result, rng.DeriveChild(1));
        setup.CreateAIEmpire(galaxy, result, rng.DeriveChild(2));
        setup.CreateAIEmpire(galaxy, result, rng.DeriveChild(3));

        var homeIds = result.Empires.Select(e => e.HomeSystemId).ToList();
        Assert.Equal(homeIds.Count, homeIds.Distinct().Count()); // All unique
    }

    [Fact]
    public void StartingConditions_AllOrigins_HaveBaseShips()
    {
        foreach (var origin in Enum.GetValues<Origin>())
        {
            var assets = StartingConditions.GetForOrigin(origin);
            Assert.True(assets.Ships.Count >= 5,
                $"Origin {origin} has only {assets.Ships.Count} ships, expected at least 5");

            // Must have at least Scout, Fighter, Salvager, Builder
            var roles = assets.Ships.Select(s => s.Role).ToList();
            Assert.Contains("Scout", roles);
            Assert.Contains("Fighter", roles);
            Assert.Contains("Salvager", roles);
            Assert.Contains("Builder", roles);
        }
    }

    [Fact]
    public void StartingConditions_Haulers_CanSeeHiddenLanes()
    {
        var assets = StartingConditions.GetForOrigin(Origin.Haulers);
        Assert.True(assets.CanSeeHiddenLanes);
        Assert.Equal(2, assets.StartingVisibilityHops);
    }

    [Fact]
    public void StartingConditions_Warriors_HaveCombatBonus()
    {
        var assets = StartingConditions.GetForOrigin(Origin.Warriors);
        Assert.True(assets.CombatBonus > 0);
    }

    [Fact]
    public void Setup_Deterministic_SameSeed()
    {
        var galaxy = MakeGalaxy();

        var setup1 = new GameSetupManager();
        var result1 = new GameSetupManager.SetupResult();
        setup1.CreatePlayerEmpire("P", PrecursorColor.Red, Origin.Warriors, galaxy, result1, new GameRandom(99));

        var setup2 = new GameSetupManager();
        var result2 = new GameSetupManager.SetupResult();
        setup2.CreatePlayerEmpire("P", PrecursorColor.Red, Origin.Warriors, galaxy, result2, new GameRandom(99));

        Assert.Equal(result1.Empires[0].HomeSystemId, result2.Empires[0].HomeSystemId);
        Assert.Equal(result1.Ships.Count, result2.Ships.Count);
        for (int i = 0; i < result1.Ships.Count; i++)
        {
            Assert.Equal(result1.Ships[i].Name, result2.Ships[i].Name);
            Assert.Equal(result1.Ships[i].SizeClass, result2.Ships[i].SizeClass);
        }
    }
}
