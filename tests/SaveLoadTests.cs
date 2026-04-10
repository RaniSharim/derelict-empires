using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Systems;
using Xunit;

namespace DerlictEmpires.Tests;

public class SaveLoadTests
{
    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var original = CreateTestSaveData();

        var json = SaveLoadManager.ToJson(original);
        var restored = SaveLoadManager.FromJson(json);

        Assert.Equal(original.Version, restored.Version);
        Assert.Equal(original.MasterSeed, restored.MasterSeed);
        Assert.Equal(original.GameTime, restored.GameTime);
        Assert.Equal(original.GameSpeed, restored.GameSpeed);
    }

    [Fact]
    public void RoundTrip_PreservesGalaxy()
    {
        var original = CreateTestSaveData();

        var json = SaveLoadManager.ToJson(original);
        var restored = SaveLoadManager.FromJson(json);

        Assert.Equal(original.Galaxy.Systems.Count, restored.Galaxy.Systems.Count);
        Assert.Equal(original.Galaxy.Lanes.Count, restored.Galaxy.Lanes.Count);
        Assert.Equal(original.Galaxy.Seed, restored.Galaxy.Seed);

        Assert.Equal("Alpha", restored.Galaxy.Systems[0].Name);
        Assert.Equal(0f, restored.Galaxy.Systems[0].PositionX);
        Assert.Equal(POIType.HabitablePlanet, restored.Galaxy.Systems[0].POIs[0].Type);
    }

    [Fact]
    public void RoundTrip_PreservesEmpires()
    {
        var original = CreateTestSaveData();

        var json = SaveLoadManager.ToJson(original);
        var restored = SaveLoadManager.FromJson(json);

        Assert.Equal(2, restored.Empires.Count);
        Assert.Equal("Player", restored.Empires[0].Name);
        Assert.True(restored.Empires[0].IsHuman);
        Assert.Equal(PrecursorColor.Red, restored.Empires[0].Affinity);
        Assert.Equal(Origin.Warriors, restored.Empires[0].Origin);
        Assert.Equal(1000, restored.Empires[0].Credits);
    }

    [Fact]
    public void RoundTrip_PreservesFleetsAndShips()
    {
        var original = CreateTestSaveData();

        var json = SaveLoadManager.ToJson(original);
        var restored = SaveLoadManager.FromJson(json);

        Assert.Single(restored.Fleets);
        Assert.Equal("1st Fleet", restored.Fleets[0].Name);
        Assert.Equal(2, restored.Fleets[0].ShipIds.Count);

        Assert.Equal(2, restored.Ships.Count);
        Assert.Equal("Scout", restored.Ships[0].Role);
        Assert.Equal(ShipSizeClass.Corvette, restored.Ships[0].SizeClass);
    }

    [Fact]
    public void RoundTrip_PreservesFleetOrders()
    {
        var original = CreateTestSaveData();

        var json = SaveLoadManager.ToJson(original);
        var restored = SaveLoadManager.FromJson(json);

        Assert.Single(restored.FleetOrders);
        var order = restored.FleetOrders[0];
        Assert.Equal(0, order.FleetId);
        Assert.Equal(FleetOrderType.MoveTo, order.Type);
        Assert.Equal(new List<int> { 1 }, order.Path);
        Assert.Equal(0.5f, order.LaneProgress);
    }

    [Fact]
    public void RoundTrip_PreservesColoniesAndStations()
    {
        var original = CreateTestSaveData();

        var json = SaveLoadManager.ToJson(original);
        var restored = SaveLoadManager.FromJson(json);

        Assert.Single(restored.Colonies);
        Assert.Equal("Alpha Colony", restored.Colonies[0].Name);
        Assert.Equal(PlanetSize.Medium, restored.Colonies[0].PlanetSize);
        Assert.Equal(3, restored.Colonies[0].Population);

        Assert.Single(restored.Stations);
        Assert.Contains("Shipyard", restored.Stations[0].InstalledModules);
    }

    [Fact]
    public void CompactMode_ProducesSmallerOutput()
    {
        var data = CreateTestSaveData();

        var pretty = SaveLoadManager.ToJson(data, compact: false);
        var compact = SaveLoadManager.ToJson(data, compact: true);

        Assert.True(compact.Length < pretty.Length);
    }

    [Fact]
    public void FromJson_NullInput_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() => SaveLoadManager.FromJson(null!));
    }

    private static GameSaveData CreateTestSaveData()
    {
        return new GameSaveData
        {
            Version = 1,
            MasterSeed = 42,
            GameTime = 100.5,
            GameSpeed = GameSpeed.Normal,
            Galaxy = new GalaxyData
            {
                Seed = 42,
                ArmCount = 1,
                Systems = new List<StarSystemData>
                {
                    new()
                    {
                        Id = 0, Name = "Alpha", PositionX = 0, PositionZ = 0,
                        ArmIndex = 0, DominantColor = PrecursorColor.Red,
                        POIs = new List<POIData>
                        {
                            new()
                            {
                                Id = 0, Name = "Alpha Prime", Type = POIType.HabitablePlanet,
                                PlanetSize = PlanetSize.Medium,
                                Deposits = new List<ResourceDeposit>
                                {
                                    new() { Color = PrecursorColor.Red, Type = ResourceType.SimpleEnergy, TotalAmount = 1000, RemainingAmount = 1000, BaseExtractionRate = 2 }
                                }
                            }
                        },
                        ConnectedLaneIndices = new List<int> { 0 }
                    },
                    new()
                    {
                        Id = 1, Name = "Beta", PositionX = 30, PositionZ = 0,
                        ArmIndex = 0, DominantColor = PrecursorColor.Blue,
                        POIs = new List<POIData>(),
                        ConnectedLaneIndices = new List<int> { 0 }
                    }
                },
                Lanes = new List<LaneData>
                {
                    new() { SystemA = 0, SystemB = 1, Type = LaneType.Visible, Distance = 30 }
                }
            },
            Empires = new List<EmpireData>
            {
                new() { Id = 0, Name = "Player", IsHuman = true, Affinity = PrecursorColor.Red, Origin = Origin.Warriors, HomeSystemId = 0, Credits = 1000 },
                new() { Id = 1, Name = "AI", IsHuman = false, Affinity = PrecursorColor.Blue, Origin = Origin.Servitors, HomeSystemId = 1, Credits = 500 }
            },
            Fleets = new List<FleetData>
            {
                new() { Id = 0, Name = "1st Fleet", OwnerEmpireId = 0, CurrentSystemId = -1, ShipIds = new List<int> { 0, 1 }, Speed = 10 }
            },
            Ships = new List<ShipInstanceData>
            {
                new() { Id = 0, Name = "Scout", OwnerEmpireId = 0, SizeClass = ShipSizeClass.Corvette, Role = "Scout", MaxHp = 100, CurrentHp = 100, FleetId = 0 },
                new() { Id = 1, Name = "Fighter", OwnerEmpireId = 0, SizeClass = ShipSizeClass.Frigate, Role = "Fighter", MaxHp = 200, CurrentHp = 200, FleetId = 0 }
            },
            FleetOrders = new List<FleetOrderSaveData>
            {
                new() { FleetId = 0, Type = FleetOrderType.MoveTo, Path = new List<int> { 1 }, PathIndex = 0, LaneProgress = 0.5f, TransitFromSystemId = 0 }
            },
            Colonies = new List<ColonyData>
            {
                new() { Id = 0, Name = "Alpha Colony", OwnerEmpireId = 0, SystemId = 0, POIId = 0, PlanetSize = PlanetSize.Medium, Population = 3, Happiness = 70 }
            },
            Stations = new List<StationData>
            {
                new() { Id = 0, Name = "Alpha Station", OwnerEmpireId = 0, SystemId = 0, POIId = 0, SizeTier = 1, InstalledModules = new List<string> { "Shipyard" } }
            },
            Extractions = new List<ExtractionAssignment>
            {
                new() { Id = 0, OwnerEmpireId = 0, SystemId = 0, POIId = 0, DepositIndex = 0, EfficiencyMultiplier = 1.0f, WorkerCount = 1 }
            }
        };
    }
}
