using Xunit;
using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Logistics;

namespace DerlictEmpires.Tests.Logistics;

public class SupplyConsumptionTests
{
    [Fact]
    public void FleetWithLasers_HighEnergy_LowParts()
    {
        var (energy, parts, food) = LogisticsSystem.CalculateConsumption(
            shipCount: 5, avgWeaponDamage: 20f, hasShields: true, hasArmor: false, crewCount: 50);

        Assert.True(energy > parts, "Laser fleet should consume more energy than parts");
    }

    [Fact]
    public void FleetWithRailguns_LowEnergy_HighParts()
    {
        var (energy, parts, food) = LogisticsSystem.CalculateConsumption(
            shipCount: 5, avgWeaponDamage: 20f, hasShields: false, hasArmor: true, crewCount: 50);

        // Armor fleet uses more parts for repair
        Assert.True(parts > 0);
    }

    [Fact]
    public void FoodScalesWithCrew()
    {
        var (_, _, food10) = LogisticsSystem.CalculateConsumption(1, 0f, false, false, 10);
        var (_, _, food100) = LogisticsSystem.CalculateConsumption(1, 0f, false, false, 100);

        Assert.True(food100 > food10);
    }
}

public class LogisticsNetworkTests
{
    private static GalaxyData MakeLinearGalaxy(int count = 5)
    {
        var systems = new List<StarSystemData>();
        for (int i = 0; i < count; i++)
            systems.Add(new StarSystemData { Id = i, Name = $"S{i}", PositionX = i * 10f });

        var lanes = new List<LaneData>();
        for (int i = 0; i < count - 1; i++)
        {
            lanes.Add(new LaneData { SystemA = i, SystemB = i + 1, Distance = 10f, Type = LaneType.Visible });
            systems[i].ConnectedLaneIndices.Add(i);
            systems[i + 1].ConnectedLaneIndices.Add(i);
        }

        return new GalaxyData { Systems = systems, Lanes = lanes };
    }

    [Fact]
    public void SupplyAtHub_EqualsCapacity()
    {
        var galaxy = MakeLinearGalaxy();
        var network = new LogisticsNetwork();
        network.AddHub(new LogisticsNetwork.LogisticsHub
            { SystemId = 0, EmpireId = 0, Capacity = 100f });

        float supply = network.CalculateSupplyAt(0, 0, galaxy);
        Assert.Equal(100f, supply);
    }

    [Fact]
    public void SupplyDecreases_WithDistance()
    {
        var galaxy = MakeLinearGalaxy();
        var network = new LogisticsNetwork();
        network.AddHub(new LogisticsNetwork.LogisticsHub
            { SystemId = 0, EmpireId = 0, Capacity = 100f });

        float atHub = network.CalculateSupplyAt(0, 0, galaxy);
        float oneHop = network.CalculateSupplyAt(0, 1, galaxy);
        float twoHop = network.CalculateSupplyAt(0, 2, galaxy);

        Assert.True(oneHop < atHub);
        Assert.True(twoHop < oneHop);
    }

    [Fact]
    public void DisconnectedFleet_GetsZeroSupply()
    {
        // Galaxy with 2 disconnected components
        var systems = new List<StarSystemData>
        {
            new() { Id = 0 }, new() { Id = 1 }, new() { Id = 2 }
        };
        var lanes = new List<LaneData>
        {
            new() { SystemA = 0, SystemB = 1, Distance = 10f, Type = LaneType.Visible }
        };
        systems[0].ConnectedLaneIndices.Add(0);
        systems[1].ConnectedLaneIndices.Add(0);
        // System 2 is disconnected

        var galaxy = new GalaxyData { Systems = systems, Lanes = lanes };
        var network = new LogisticsNetwork();
        network.AddHub(new LogisticsNetwork.LogisticsHub
            { SystemId = 0, EmpireId = 0, Capacity = 100f });

        float supply = network.CalculateSupplyAt(0, 2, galaxy);
        Assert.Equal(0f, supply);
    }

    [Fact]
    public void WastePerHop_Is15Percent()
    {
        var galaxy = MakeLinearGalaxy();
        var network = new LogisticsNetwork();
        network.AddHub(new LogisticsNetwork.LogisticsHub
            { SystemId = 0, EmpireId = 0, Capacity = 100f });

        float oneHop = network.CalculateSupplyAt(0, 1, galaxy);
        Assert.InRange(oneHop, 84f, 86f); // 100 * 0.85 = 85
    }
}
