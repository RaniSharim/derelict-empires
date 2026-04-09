using Xunit;
using DerlictEmpires.Core.Systems;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Tests.Galaxy;

public class GalaxyGeneratorTests
{
    private static GalaxyGenerationConfig DefaultConfig(int seed = 42) => new()
    {
        Seed = seed,
        TotalSystems = 100,
        ArmCount = 4,
        GalaxyRadius = 200f,
        MaxLaneLength = 60f,
        MinNeighbors = 2,
        MaxNeighbors = 4,
        HiddenLaneRatio = 0.15f
    };

    [Fact]
    public void SameSeed_ProducesIdenticalGalaxy()
    {
        var galaxy1 = GalaxyGenerator.Generate(DefaultConfig(42));
        var galaxy2 = GalaxyGenerator.Generate(DefaultConfig(42));

        Assert.Equal(galaxy1.Systems.Count, galaxy2.Systems.Count);
        Assert.Equal(galaxy1.Lanes.Count, galaxy2.Lanes.Count);

        for (int i = 0; i < galaxy1.Systems.Count; i++)
        {
            Assert.Equal(galaxy1.Systems[i].Name, galaxy2.Systems[i].Name);
            Assert.Equal(galaxy1.Systems[i].PositionX, galaxy2.Systems[i].PositionX);
            Assert.Equal(galaxy1.Systems[i].PositionZ, galaxy2.Systems[i].PositionZ);
            Assert.Equal(galaxy1.Systems[i].ArmIndex, galaxy2.Systems[i].ArmIndex);
            Assert.Equal(galaxy1.Systems[i].IsCore, galaxy2.Systems[i].IsCore);
            Assert.Equal(galaxy1.Systems[i].POIs.Count, galaxy2.Systems[i].POIs.Count);
        }
    }

    [Fact]
    public void DifferentSeed_ProducesDifferentGalaxy()
    {
        var galaxy1 = GalaxyGenerator.Generate(DefaultConfig(42));
        var galaxy2 = GalaxyGenerator.Generate(DefaultConfig(999));

        // Names should differ (overwhelmingly likely with different seeds)
        bool anyDifferent = false;
        int checkCount = Math.Min(galaxy1.Systems.Count, galaxy2.Systems.Count);
        for (int i = 0; i < checkCount; i++)
        {
            if (galaxy1.Systems[i].Name != galaxy2.Systems[i].Name)
            {
                anyDifferent = true;
                break;
            }
        }
        Assert.True(anyDifferent);
    }

    [Fact]
    public void SystemCount_MatchesConfig()
    {
        var config = DefaultConfig();
        var galaxy = GalaxyGenerator.Generate(config);
        Assert.Equal(config.TotalSystems, galaxy.Systems.Count);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(150)]
    public void SystemCount_ScalesWithConfig(int total)
    {
        var config = DefaultConfig();
        config.TotalSystems = total;
        var galaxy = GalaxyGenerator.Generate(config);
        Assert.Equal(total, galaxy.Systems.Count);
    }

    [Fact]
    public void CoreSystems_AreAbout20Percent()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());
        int coreCount = galaxy.Systems.Count(s => s.IsCore);
        // Should be ~20 for 100 total systems
        Assert.InRange(coreCount, 10, 30);
    }

    [Fact]
    public void EachArm_HasSystems()
    {
        var config = DefaultConfig();
        var galaxy = GalaxyGenerator.Generate(config);

        for (int arm = 0; arm < config.ArmCount; arm++)
        {
            int armCount = galaxy.Systems.Count(s => s.ArmIndex == arm);
            Assert.True(armCount > 0, $"Arm {arm} has no systems");
        }
    }

    [Fact]
    public void AllSystems_HaveUniqueIds()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());
        var ids = galaxy.Systems.Select(s => s.Id).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void AllSystems_HaveNames()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());
        foreach (var sys in galaxy.Systems)
        {
            Assert.False(string.IsNullOrWhiteSpace(sys.Name), $"System {sys.Id} has no name");
        }
    }
}
