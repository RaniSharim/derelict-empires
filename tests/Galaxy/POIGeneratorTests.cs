using Xunit;
using DerlictEmpires.Core.Systems;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Tests.Galaxy;

public class POIGeneratorTests
{
    private static GalaxyGenerationConfig DefaultConfig() => new()
    {
        Seed = 42,
        TotalSystems = 100,
        ArmCount = 4,
        GalaxyRadius = 200f,
        MaxLaneLength = 60f,
        MinNeighbors = 2,
        MaxNeighbors = 4,
        HiddenLaneRatio = 0.15f
    };

    [Fact]
    public void EachSystem_Has3To5POIs()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());

        foreach (var sys in galaxy.Systems)
        {
            // After guarantee patches, a system might have up to 6
            Assert.InRange(sys.POIs.Count, 3, 6);
        }
    }

    [Fact]
    public void CoreSystems_HaveMoreDebrisAndGraveyards()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());

        int coreDebris = galaxy.Systems
            .Where(s => s.IsCore)
            .SelectMany(s => s.POIs)
            .Count(p => p.Type == POIType.DebrisField || p.Type == POIType.ShipGraveyard);

        int rimDebris = galaxy.Systems
            .Where(s => !s.IsCore && s.RadialPosition > 0.7f)
            .SelectMany(s => s.POIs)
            .Count(p => p.Type == POIType.DebrisField || p.Type == POIType.ShipGraveyard);

        int coreSystems = galaxy.Systems.Count(s => s.IsCore);
        int rimSystems = galaxy.Systems.Count(s => !s.IsCore && s.RadialPosition > 0.7f);

        if (coreSystems == 0 || rimSystems == 0) return;

        float coreRate = (float)coreDebris / coreSystems;
        float rimRate = (float)rimDebris / rimSystems;

        // Core should have a higher density of debris/graveyards
        Assert.True(coreRate > rimRate,
            $"Core debris rate ({coreRate:F2}) should exceed rim rate ({rimRate:F2})");
    }

    [Fact]
    public void RimSystems_HaveMoreAsteroidsAndHabitables()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());

        int rimGood = galaxy.Systems
            .Where(s => !s.IsCore && s.RadialPosition > 0.7f)
            .SelectMany(s => s.POIs)
            .Count(p => p.Type == POIType.AsteroidField || p.Type == POIType.HabitablePlanet);

        int coreGood = galaxy.Systems
            .Where(s => s.IsCore)
            .SelectMany(s => s.POIs)
            .Count(p => p.Type == POIType.AsteroidField || p.Type == POIType.HabitablePlanet);

        int rimSystems = galaxy.Systems.Count(s => !s.IsCore && s.RadialPosition > 0.7f);
        int coreSystems = galaxy.Systems.Count(s => s.IsCore);

        if (rimSystems == 0 || coreSystems == 0) return;

        float rimRate = (float)rimGood / rimSystems;
        float coreRate = (float)coreGood / coreSystems;

        Assert.True(rimRate > coreRate,
            $"Rim asteroid+habitable rate ({rimRate:F2}) should exceed core rate ({coreRate:F2})");
    }

    [Fact]
    public void EachArm_HasAtLeastOneHabitablePlanet()
    {
        var config = DefaultConfig();
        var galaxy = GalaxyGenerator.Generate(config);

        for (int arm = 0; arm < config.ArmCount; arm++)
        {
            bool hasHabitable = galaxy.Systems
                .Where(s => s.ArmIndex == arm)
                .SelectMany(s => s.POIs)
                .Any(p => p.Type == POIType.HabitablePlanet);

            Assert.True(hasHabitable, $"Arm {arm} has no habitable planet");
        }
    }

    [Fact]
    public void POIs_HaveResourceDeposits()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());

        // At least some POIs should have deposits
        int poisWithDeposits = galaxy.Systems
            .SelectMany(s => s.POIs)
            .Count(p => p.Deposits.Count > 0);

        Assert.True(poisWithDeposits > 50,
            $"Only {poisWithDeposits} POIs have deposits, expected at least 50");
    }

    [Fact]
    public void AllPOIs_HaveNames()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());

        foreach (var sys in galaxy.Systems)
        foreach (var poi in sys.POIs)
        {
            Assert.False(string.IsNullOrWhiteSpace(poi.Name),
                $"POI {poi.Id} in system {sys.Name} has no name");
        }
    }

    [Fact]
    public void Deposits_HavePositiveValues()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());

        foreach (var sys in galaxy.Systems)
        foreach (var poi in sys.POIs)
        foreach (var dep in poi.Deposits)
        {
            Assert.True(dep.TotalAmount > 0, "Deposit has non-positive total");
            Assert.Equal(dep.TotalAmount, dep.RemainingAmount);
            Assert.True(dep.BaseExtractionRate > 0, "Deposit has non-positive extraction rate");
        }
    }
}
