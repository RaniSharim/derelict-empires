using Xunit;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Tests;

public class SmokeTests
{
    [Fact]
    public void AllEnumsExist()
    {
        Assert.Equal(5, Enum.GetValues<PrecursorColor>().Length);
        Assert.Equal(4, Enum.GetValues<ResourceType>().Length);
        Assert.Equal(2, Enum.GetValues<ResourceRarity>().Length);
        Assert.Equal(2, Enum.GetValues<ComponentTier>().Length);
        Assert.Equal(2, Enum.GetValues<LaneType>().Length);
        Assert.Equal(7, Enum.GetValues<POIType>().Length);
        Assert.Equal(5, Enum.GetValues<GameSpeed>().Length);
        Assert.Equal(4, Enum.GetValues<GameState>().Length);
        Assert.Equal(7, Enum.GetValues<ShipSizeClass>().Length);
        Assert.Equal(6, Enum.GetValues<FleetRole>().Length);
        Assert.Equal(5, Enum.GetValues<Origin>().Length);
        Assert.Equal(5, Enum.GetValues<TerrainModifier>().Length);
    }

    [Fact]
    public void ResourceDefinitions_Has20Entries()
    {
        Assert.Equal(20, ResourceDefinition.All.Length);
    }

    [Fact]
    public void ResourceDefinitions_AllUniqueIds()
    {
        var ids = ResourceDefinition.All.Select(r => r.Id).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void ResourceDefinitions_CoversAllColorTypeCombinations()
    {
        foreach (var color in Enum.GetValues<PrecursorColor>())
        foreach (var type in Enum.GetValues<ResourceType>())
        {
            var def = ResourceDefinition.Find(color, type);
            Assert.NotNull(def);
        }
    }

    [Fact]
    public void ComponentDefinitions_Has10Entries()
    {
        Assert.Equal(10, ComponentDefinition.All.Length);
    }

    [Fact]
    public void ComponentDefinitions_CoversAllColorTierCombinations()
    {
        foreach (var color in Enum.GetValues<PrecursorColor>())
        foreach (var tier in Enum.GetValues<ComponentTier>())
        {
            var def = ComponentDefinition.Find(color, tier);
            Assert.NotNull(def);
        }
    }

    [Fact]
    public void EmpireData_ResourceStockpile_Works()
    {
        var empire = new EmpireData { Id = 1, Name = "Test Empire" };
        empire.AddResource(PrecursorColor.Red, ResourceType.SimpleEnergy, 100f);
        Assert.Equal(100f, empire.GetResource(PrecursorColor.Red, ResourceType.SimpleEnergy));
        Assert.Equal(0f, empire.GetResource(PrecursorColor.Blue, ResourceType.SimpleEnergy));

        empire.AddResource(PrecursorColor.Red, ResourceType.SimpleEnergy, 50f);
        Assert.Equal(150f, empire.GetResource(PrecursorColor.Red, ResourceType.SimpleEnergy));
    }

    [Fact]
    public void GalaxyData_GetNeighbors_Works()
    {
        var galaxy = new GalaxyData
        {
            Systems = new()
            {
                new StarSystemData { Id = 0, Name = "Alpha" },
                new StarSystemData { Id = 1, Name = "Beta" },
                new StarSystemData { Id = 2, Name = "Gamma" },
            },
            Lanes = new()
            {
                new LaneData { SystemA = 0, SystemB = 1, Distance = 10f },
                new LaneData { SystemA = 1, SystemB = 2, Distance = 15f },
            }
        };

        var neighbors0 = galaxy.GetNeighbors(0).ToList();
        Assert.Single(neighbors0);
        Assert.Equal(1, neighbors0[0]);

        var neighbors1 = galaxy.GetNeighbors(1).ToList();
        Assert.Equal(2, neighbors1.Count);
        Assert.Contains(0, neighbors1);
        Assert.Contains(2, neighbors1);
    }
}
