using Xunit;
using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Random;
using DerlictEmpires.Core.Exploration;

namespace DerlictEmpires.Tests.Exploration;

public class ExplorationManagerTests
{
    [Fact]
    public void NewPOI_IsUndiscovered()
    {
        var mgr = new ExplorationManager();
        Assert.Equal(ExplorationState.Undiscovered, mgr.GetState(0, 42));
    }

    [Fact]
    public void DiscoverSystem_SetsDiscovered()
    {
        var mgr = new ExplorationManager();
        mgr.DiscoverSystem(0, 1, new List<int> { 10, 11, 12 });

        Assert.Equal(ExplorationState.Discovered, mgr.GetState(0, 10));
        Assert.Equal(ExplorationState.Discovered, mgr.GetState(0, 11));
        Assert.Equal(ExplorationState.Discovered, mgr.GetState(0, 12));
    }

    [Fact]
    public void SurveyPOI_SetsSurveyed()
    {
        var mgr = new ExplorationManager();
        mgr.DiscoverSystem(0, 1, new List<int> { 10 });
        mgr.SurveyPOI(0, 10, 75);

        Assert.Equal(ExplorationState.Surveyed, mgr.GetState(0, 10));
        Assert.Equal(75, mgr.GetSurveyDetail(0, 10));
    }

    [Fact]
    public void DifferentEmpires_IndependentState()
    {
        var mgr = new ExplorationManager();
        mgr.DiscoverSystem(0, 1, new List<int> { 10 });

        Assert.Equal(ExplorationState.Discovered, mgr.GetState(0, 10));
        Assert.Equal(ExplorationState.Undiscovered, mgr.GetState(1, 10));
    }
}

public class HazardCheckerTests
{
    [Fact]
    public void HighAffinity_ReducesTriggerChance()
    {
        int triggeredWithAffinity = 0;
        int triggeredWithout = 0;

        for (int seed = 0; seed < 1000; seed++)
        {
            var rng1 = new GameRandom(seed);
            var r1 = HazardChecker.Check(0.5f, true, 0, 0, rng1);
            if (r1.Triggered) triggeredWithAffinity++;

            var rng2 = new GameRandom(seed);
            var r2 = HazardChecker.Check(0.5f, false, 0, 0, rng2);
            if (r2.Triggered) triggeredWithout++;
        }

        Assert.True(triggeredWithAffinity < triggeredWithout,
            $"Affinity triggers {triggeredWithAffinity} should be < no affinity {triggeredWithout}");
    }

    [Fact]
    public void HighSurveyQuality_ReducesTriggerChance()
    {
        int triggeredLowSurvey = 0;
        int triggeredHighSurvey = 0;

        for (int seed = 0; seed < 1000; seed++)
        {
            var r1 = HazardChecker.Check(0.5f, false, 0, 10, new GameRandom(seed));
            if (r1.Triggered) triggeredLowSurvey++;

            var r2 = HazardChecker.Check(0.5f, false, 0, 90, new GameRandom(seed));
            if (r2.Triggered) triggeredHighSurvey++;
        }

        Assert.True(triggeredHighSurvey < triggeredLowSurvey);
    }

    [Fact]
    public void SameSeed_SameOutcome()
    {
        var r1 = HazardChecker.Check(0.4f, false, 1, 30, new GameRandom(42));
        var r2 = HazardChecker.Check(0.4f, false, 1, 30, new GameRandom(42));

        Assert.Equal(r1.Triggered, r2.Triggered);
        Assert.Equal(r1.Type, r2.Type);
    }
}

public class DerelictProcessorTests
{
    private static DerelictShip MakeDerelict() => new()
    {
        Id = 0, Name = "Wreck", Color = PrecursorColor.Red,
        TechTier = 3, SizeClass = ShipSizeClass.Cruiser, Condition = 70f
    };

    [Fact]
    public void SalvageForParts_YieldsComponents()
    {
        var result = DerelictProcessor.CalculateAction(MakeDerelict(), DerelictAction.SalvageForParts);
        Assert.True(result.BasicComponents > 0);
        Assert.True(result.AdvancedComponents > 0);
        Assert.True(result.ResearchPoints > 0);
        Assert.Equal(0, result.ProductionCost);
    }

    [Fact]
    public void UseAsIs_HasEfficiencyPenalty()
    {
        var result = DerelictProcessor.CalculateAction(MakeDerelict(), DerelictAction.UseAsIs);
        Assert.True(result.EfficiencyPenalty > 0);
        Assert.Equal(0, result.BasicComponents);
    }

    [Fact]
    public void Repair_CostsAdvancedComponents()
    {
        var result = DerelictProcessor.CalculateAction(MakeDerelict(), DerelictAction.Repair);
        Assert.True(result.AdvancedComponents < 0); // Negative = costs
        Assert.True(result.ProductionCost > 0);
        Assert.Equal(0f, result.EfficiencyPenalty);
    }

    [Fact]
    public void Replicate_MostExpensive()
    {
        var salvage = DerelictProcessor.CalculateAction(MakeDerelict(), DerelictAction.SalvageForParts);
        var repair = DerelictProcessor.CalculateAction(MakeDerelict(), DerelictAction.Repair);
        var replicate = DerelictProcessor.CalculateAction(MakeDerelict(), DerelictAction.Replicate);

        Assert.True(replicate.ProductionCost > repair.ProductionCost);
    }

    [Fact]
    public void AllActions_ReturnValidResult()
    {
        var derelict = MakeDerelict();
        foreach (var action in System.Enum.GetValues<DerelictAction>())
        {
            var result = DerelictProcessor.CalculateAction(derelict, action);
            Assert.NotNull(result.Description);
        }
    }
}
