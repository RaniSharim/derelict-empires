using Xunit;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Tests.Economy;

public class ResourceExtractionTests
{
    private static (GalaxyData galaxy, EmpireData empire, POIData poi) SetupSimpleScenario()
    {
        var poi = new POIData
        {
            Id = 0,
            Name = "Test POI",
            Type = POIType.AsteroidField,
            Deposits = new List<ResourceDeposit>
            {
                new()
                {
                    Color = PrecursorColor.Red,
                    Type = ResourceType.SimpleParts,
                    TotalAmount = 100f,
                    RemainingAmount = 100f,
                    BaseExtractionRate = 5f
                }
            }
        };

        var system = new StarSystemData { Id = 0, Name = "Test System", POIs = new List<POIData> { poi } };
        var galaxy = new GalaxyData { Systems = new List<StarSystemData> { system }, Lanes = new List<LaneData>() };
        var empire = new EmpireData { Id = 0, Name = "Test Empire" };

        return (galaxy, empire, poi);
    }

    [Fact]
    public void Extraction_IncreasesStockpile()
    {
        var (galaxy, empire, poi) = SetupSimpleScenario();
        var system = new ResourceExtractionSystem();
        system.RegisterGalaxy(galaxy);

        system.AddAssignment(new ExtractionAssignment
        {
            Id = 0, OwnerEmpireId = 0, SystemId = 0, POIId = 0,
            DepositIndex = 0, EfficiencyMultiplier = 1.0f, WorkerCount = 1
        });

        float before = empire.GetResource(PrecursorColor.Red, ResourceType.SimpleParts);
        system.ProcessTick(1.0f, new List<EmpireData> { empire });
        float after = empire.GetResource(PrecursorColor.Red, ResourceType.SimpleParts);

        Assert.True(after > before, $"Stockpile should increase: was {before}, now {after}");
    }

    [Fact]
    public void Extraction_Rate_MatchesDeposit()
    {
        var (galaxy, empire, poi) = SetupSimpleScenario();
        var system = new ResourceExtractionSystem();
        system.RegisterGalaxy(galaxy);

        system.AddAssignment(new ExtractionAssignment
        {
            Id = 0, OwnerEmpireId = 0, SystemId = 0, POIId = 0,
            DepositIndex = 0, EfficiencyMultiplier = 1.0f, WorkerCount = 1
        });

        system.ProcessTick(1.0f, new List<EmpireData> { empire });

        float extracted = empire.GetResource(PrecursorColor.Red, ResourceType.SimpleParts);
        // BaseRate=5, efficiency=1, workers=1, delta=1 → expect 5
        Assert.Equal(5f, extracted);
    }

    [Fact]
    public void Extraction_DecreasesDeposit()
    {
        var (galaxy, empire, poi) = SetupSimpleScenario();
        var system = new ResourceExtractionSystem();
        system.RegisterGalaxy(galaxy);

        system.AddAssignment(new ExtractionAssignment
        {
            Id = 0, OwnerEmpireId = 0, SystemId = 0, POIId = 0,
            DepositIndex = 0, EfficiencyMultiplier = 1.0f, WorkerCount = 1
        });

        system.ProcessTick(1.0f, new List<EmpireData> { empire });

        Assert.Equal(95f, poi.Deposits[0].RemainingAmount);
    }

    [Fact]
    public void Extraction_ClampsToRemaining()
    {
        var (galaxy, empire, poi) = SetupSimpleScenario();
        poi.Deposits[0].RemainingAmount = 2f; // Only 2 left, extraction rate is 5

        var system = new ResourceExtractionSystem();
        system.RegisterGalaxy(galaxy);

        system.AddAssignment(new ExtractionAssignment
        {
            Id = 0, OwnerEmpireId = 0, SystemId = 0, POIId = 0,
            DepositIndex = 0, EfficiencyMultiplier = 1.0f, WorkerCount = 1
        });

        system.ProcessTick(1.0f, new List<EmpireData> { empire });

        Assert.Equal(2f, empire.GetResource(PrecursorColor.Red, ResourceType.SimpleParts));
        Assert.Equal(0f, poi.Deposits[0].RemainingAmount);
    }

    [Fact]
    public void Depletion_FiresEvent_RemovesAssignment()
    {
        var (galaxy, empire, poi) = SetupSimpleScenario();
        poi.Deposits[0].RemainingAmount = 3f;

        var system = new ResourceExtractionSystem();
        system.RegisterGalaxy(galaxy);

        bool depleted = false;
        system.DepositDepleted += (_, _) => depleted = true;

        system.AddAssignment(new ExtractionAssignment
        {
            Id = 0, OwnerEmpireId = 0, SystemId = 0, POIId = 0,
            DepositIndex = 0, EfficiencyMultiplier = 1.0f, WorkerCount = 1
        });

        system.ProcessTick(1.0f, new List<EmpireData> { empire });

        Assert.True(depleted);
        Assert.Empty(system.AllAssignments);
    }

    [Fact]
    public void Efficiency_MultipliesRate()
    {
        var (galaxy, empire, poi) = SetupSimpleScenario();
        var system = new ResourceExtractionSystem();
        system.RegisterGalaxy(galaxy);

        system.AddAssignment(new ExtractionAssignment
        {
            Id = 0, OwnerEmpireId = 0, SystemId = 0, POIId = 0,
            DepositIndex = 0, EfficiencyMultiplier = 2.0f, WorkerCount = 1
        });

        system.ProcessTick(1.0f, new List<EmpireData> { empire });

        // Rate=5, efficiency=2 → 10
        Assert.Equal(10f, empire.GetResource(PrecursorColor.Red, ResourceType.SimpleParts));
    }

    [Fact]
    public void Workers_MultiplyRate()
    {
        var (galaxy, empire, poi) = SetupSimpleScenario();
        var system = new ResourceExtractionSystem();
        system.RegisterGalaxy(galaxy);

        system.AddAssignment(new ExtractionAssignment
        {
            Id = 0, OwnerEmpireId = 0, SystemId = 0, POIId = 0,
            DepositIndex = 0, EfficiencyMultiplier = 1.0f, WorkerCount = 3
        });

        system.ProcessTick(1.0f, new List<EmpireData> { empire });

        // Rate=5, workers=3 → 15
        Assert.Equal(15f, empire.GetResource(PrecursorColor.Red, ResourceType.SimpleParts));
    }

    [Fact]
    public void MultipleDeposits_ExtractIndependently()
    {
        var poi = new POIData
        {
            Id = 0,
            Name = "Multi POI",
            Type = POIType.DebrisField,
            Deposits = new List<ResourceDeposit>
            {
                new() { Color = PrecursorColor.Red, Type = ResourceType.SimpleEnergy, TotalAmount = 100, RemainingAmount = 100, BaseExtractionRate = 3f },
                new() { Color = PrecursorColor.Blue, Type = ResourceType.SimpleParts, TotalAmount = 200, RemainingAmount = 200, BaseExtractionRate = 7f }
            }
        };

        var system0 = new StarSystemData { Id = 0, POIs = new List<POIData> { poi } };
        var galaxy = new GalaxyData { Systems = new List<StarSystemData> { system0 }, Lanes = new List<LaneData>() };
        var empire = new EmpireData { Id = 0, Name = "Test" };

        var system = new ResourceExtractionSystem();
        system.RegisterGalaxy(galaxy);

        system.AddAssignment(new ExtractionAssignment { Id = 0, OwnerEmpireId = 0, POIId = 0, DepositIndex = 0, WorkerCount = 1 });
        system.AddAssignment(new ExtractionAssignment { Id = 1, OwnerEmpireId = 0, POIId = 0, DepositIndex = 1, WorkerCount = 1 });

        system.ProcessTick(1.0f, new List<EmpireData> { empire });

        Assert.Equal(3f, empire.GetResource(PrecursorColor.Red, ResourceType.SimpleEnergy));
        Assert.Equal(7f, empire.GetResource(PrecursorColor.Blue, ResourceType.SimpleParts));
    }

    [Fact]
    public void CalculateIncome_ReturnsCorrectRates()
    {
        var (galaxy, empire, poi) = SetupSimpleScenario();
        var system = new ResourceExtractionSystem();
        system.RegisterGalaxy(galaxy);

        system.AddAssignment(new ExtractionAssignment
        {
            Id = 0, OwnerEmpireId = 0, POIId = 0, DepositIndex = 0,
            EfficiencyMultiplier = 1.0f, WorkerCount = 2
        });

        var income = system.CalculateIncome(0, 1.0f);
        var key = EmpireData.ResourceKey(PrecursorColor.Red, ResourceType.SimpleParts);

        Assert.True(income.ContainsKey(key));
        Assert.Equal(10f, income[key]); // Rate=5, workers=2
    }

    [Fact]
    public void HomeExtractions_CreateAssignmentsForAllDeposits()
    {
        var galaxy = GalaxyGenerator.Generate(new GalaxyGenerationConfig { Seed = 42, TotalSystems = 50, ArmCount = 4 });
        var homeSystem = galaxy.Systems[0];

        int totalDeposits = homeSystem.POIs.Sum(p => p.Deposits.Count);
        var assignments = ResourceDistributionHelper.CreateHomeExtractions(0, homeSystem);

        Assert.Equal(totalDeposits, assignments.Count);
        Assert.All(assignments, a => Assert.Equal(0, a.OwnerEmpireId));
    }
}
