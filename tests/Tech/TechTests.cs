using Xunit;
using System;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Random;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Tests.Tech;

public class TechTreeRegistryTests
{
    [Fact]
    public void Registry_Has150Nodes()
    {
        var reg = new TechTreeRegistry();
        Assert.Equal(150, reg.Nodes.Count);
    }

    [Fact]
    public void Registry_Has450Subsystems()
    {
        var reg = new TechTreeRegistry();
        Assert.Equal(450, reg.Subsystems.Count); // 150 nodes × 3
    }

    [Fact]
    public void Registry_Has10Synergies()
    {
        var reg = new TechTreeRegistry();
        Assert.Equal(10, reg.Synergies.Count);
    }

    [Fact]
    public void EachNode_Has3Subsystems()
    {
        var reg = new TechTreeRegistry();
        foreach (var node in reg.Nodes)
            Assert.Equal(3, node.SubsystemIds.Count);
    }

    [Fact]
    public void AllNodeIds_Unique()
    {
        var reg = new TechTreeRegistry();
        var ids = reg.Nodes.Select(n => n.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void GetNode_ByColorCategoryTier()
    {
        var reg = new TechTreeRegistry();
        var node = reg.GetNode(PrecursorColor.Red, TechCategory.WeaponsEnergyPropulsion, 1);
        Assert.NotNull(node);
        Assert.Equal(PrecursorColor.Red, node.Color);
        Assert.Equal(1, node.Tier);
    }
}

public class EfficiencyCalculatorTests
{
    [Theory]
    [InlineData(PrecursorColor.Red, PrecursorColor.Red, 1.0f)]
    [InlineData(PrecursorColor.Blue, PrecursorColor.Blue, 1.0f)]
    [InlineData(PrecursorColor.Green, PrecursorColor.Green, 1.0f)]
    [InlineData(PrecursorColor.Gold, PrecursorColor.Gold, 1.0f)]
    [InlineData(PrecursorColor.Purple, PrecursorColor.Purple, 1.0f)]
    public void SameColor_Returns1(PrecursorColor affinity, PrecursorColor tech, float expected)
    {
        Assert.Equal(expected, EfficiencyCalculator.GetEfficiency(affinity, tech));
    }

    [Theory]
    [InlineData(PrecursorColor.Red, PrecursorColor.Blue, 0.7f)]
    [InlineData(PrecursorColor.Red, PrecursorColor.Purple, 0.7f)]
    [InlineData(PrecursorColor.Blue, PrecursorColor.Red, 0.7f)]
    [InlineData(PrecursorColor.Blue, PrecursorColor.Green, 0.7f)]
    [InlineData(PrecursorColor.Green, PrecursorColor.Blue, 0.7f)]
    [InlineData(PrecursorColor.Green, PrecursorColor.Gold, 0.7f)]
    [InlineData(PrecursorColor.Gold, PrecursorColor.Green, 0.7f)]
    [InlineData(PrecursorColor.Gold, PrecursorColor.Purple, 0.7f)]
    [InlineData(PrecursorColor.Purple, PrecursorColor.Gold, 0.7f)]
    [InlineData(PrecursorColor.Purple, PrecursorColor.Red, 0.7f)]
    public void AdjacentColor_Returns07(PrecursorColor affinity, PrecursorColor tech, float expected)
    {
        Assert.Equal(expected, EfficiencyCalculator.GetEfficiency(affinity, tech));
    }

    [Theory]
    [InlineData(PrecursorColor.Red, PrecursorColor.Green, 0.4f)]
    [InlineData(PrecursorColor.Red, PrecursorColor.Gold, 0.4f)]
    [InlineData(PrecursorColor.Blue, PrecursorColor.Gold, 0.4f)]
    [InlineData(PrecursorColor.Blue, PrecursorColor.Purple, 0.4f)]
    [InlineData(PrecursorColor.Green, PrecursorColor.Red, 0.4f)]
    [InlineData(PrecursorColor.Green, PrecursorColor.Purple, 0.4f)]
    [InlineData(PrecursorColor.Gold, PrecursorColor.Red, 0.4f)]
    [InlineData(PrecursorColor.Gold, PrecursorColor.Blue, 0.4f)]
    [InlineData(PrecursorColor.Purple, PrecursorColor.Blue, 0.4f)]
    [InlineData(PrecursorColor.Purple, PrecursorColor.Green, 0.4f)]
    public void DistantColor_Returns04(PrecursorColor affinity, PrecursorColor tech, float expected)
    {
        Assert.Equal(expected, EfficiencyCalculator.GetEfficiency(affinity, tech));
    }

    [Fact]
    public void FreeRace_Returns06()
    {
        foreach (var color in Enum.GetValues<PrecursorColor>())
            Assert.Equal(0.6f, EfficiencyCalculator.GetEfficiency(null, color));
    }

    [Fact]
    public void All25Pairs_Covered()
    {
        foreach (var a in Enum.GetValues<PrecursorColor>())
        foreach (var b in Enum.GetValues<PrecursorColor>())
        {
            float eff = EfficiencyCalculator.GetEfficiency(a, b);
            Assert.True(eff == 1.0f || eff == 0.7f || eff == 0.4f,
                $"Unexpected efficiency {eff} for {a}->{b}");
        }
    }
}

public class ResearchEngineTests
{
    [Fact]
    public void Research_AccumulatesPoints()
    {
        var reg = new TechTreeRegistry();
        var engine = new ResearchEngine(reg);
        var state = new EmpireResearchState { EmpireId = 0 };
        var rng = new GameRandom(42);

        // Unlock tier 1 and start researching a subsystem
        var node = reg.GetNode(PrecursorColor.Red, TechCategory.WeaponsEnergyPropulsion, 1)!;
        state.UnlockTier(PrecursorColor.Red, TechCategory.WeaponsEnergyPropulsion, 1, node, rng);

        var subId = state.AvailableSubsystems.First();
        state.CurrentProject = subId;
        state.CurrentProgress = 0f;

        engine.ProcessTick(state, PrecursorColor.Red, 10f, 1.0f, rng);

        Assert.True(state.CurrentProgress > 0);
    }

    [Fact]
    public void Research_CompletesSubsystem()
    {
        var reg = new TechTreeRegistry();
        var engine = new ResearchEngine(reg);
        var state = new EmpireResearchState { EmpireId = 0 };
        var rng = new GameRandom(42);

        var node = reg.GetNode(PrecursorColor.Red, TechCategory.WeaponsEnergyPropulsion, 1)!;
        state.UnlockTier(PrecursorColor.Red, TechCategory.WeaponsEnergyPropulsion, 1, node, rng);

        var subId = state.AvailableSubsystems.First();
        state.CurrentProject = subId;

        bool completed = false;
        engine.SubsystemResearched += (_, id) => { if (id == subId) completed = true; };

        // Pump enough research to complete (cost = 20 for tier 1, efficiency = 1.0)
        for (int i = 0; i < 100; i++)
            engine.ProcessTick(state, PrecursorColor.Red, 10f, 1.0f, rng);

        Assert.True(completed);
        Assert.True(state.HasSubsystem(subId));
    }

    [Fact]
    public void Research_QueueAdvances()
    {
        var reg = new TechTreeRegistry();
        var engine = new ResearchEngine(reg);
        var state = new EmpireResearchState { EmpireId = 0 };
        var rng = new GameRandom(42);

        var node = reg.GetNode(PrecursorColor.Red, TechCategory.WeaponsEnergyPropulsion, 1)!;
        state.UnlockTier(PrecursorColor.Red, TechCategory.WeaponsEnergyPropulsion, 1, node, rng);

        var subs = state.AvailableSubsystems.ToList();
        Assert.True(subs.Count >= 2, "Need at least 2 available subsystems");

        state.CurrentProject = subs[0];
        state.Queue.Add(subs[1]);

        var completedIds = new System.Collections.Generic.List<string>();
        engine.SubsystemResearched += (_, id) => completedIds.Add(id);

        // With high output, both projects should complete
        for (int i = 0; i < 100; i++)
            engine.ProcessTick(state, PrecursorColor.Red, 50f, 1.0f, rng);

        Assert.Contains(subs[0], completedIds);
        Assert.Contains(subs[1], completedIds);
        Assert.True(state.HasSubsystem(subs[0]));
        Assert.True(state.HasSubsystem(subs[1]));
    }
}

public class SynergyTests
{
    [Fact]
    public void Synergy_AvailableWhenBothTiersMet()
    {
        var reg = new TechTreeRegistry();
        var engine = new ResearchEngine(reg);
        var state = new EmpireResearchState { EmpireId = 0 };
        var rng = new GameRandom(42);

        // Green+Gold synergy requires tier 2 in each
        // Unlock tier 2 in Green and Gold (any category)
        var gNode1 = reg.GetNode(PrecursorColor.Green, TechCategory.IndustryMining, 1)!;
        var gNode2 = reg.GetNode(PrecursorColor.Green, TechCategory.IndustryMining, 2)!;
        state.UnlockTier(PrecursorColor.Green, TechCategory.IndustryMining, 1, gNode1, rng);
        state.UnlockTier(PrecursorColor.Green, TechCategory.IndustryMining, 2, gNode2, rng);

        var goNode1 = reg.GetNode(PrecursorColor.Gold, TechCategory.AdminLogistics, 1)!;
        var goNode2 = reg.GetNode(PrecursorColor.Gold, TechCategory.AdminLogistics, 2)!;
        state.UnlockTier(PrecursorColor.Gold, TechCategory.AdminLogistics, 1, goNode1, rng);
        state.UnlockTier(PrecursorColor.Gold, TechCategory.AdminLogistics, 2, goNode2, rng);

        engine.CheckSynergyUnlocks(state);

        Assert.Contains("synergy_green_gold", state.AvailableSynergies);
    }

    [Fact]
    public void Synergy_NotAvailableWhenOnlyOneTierMet()
    {
        var reg = new TechTreeRegistry();
        var engine = new ResearchEngine(reg);
        var state = new EmpireResearchState { EmpireId = 0 };
        var rng = new GameRandom(42);

        // Only unlock Green tier 2
        var gNode = reg.GetNode(PrecursorColor.Green, TechCategory.IndustryMining, 2)!;
        state.UnlockTier(PrecursorColor.Green, TechCategory.IndustryMining, 2, gNode, rng);

        engine.CheckSynergyUnlocks(state);

        Assert.DoesNotContain("synergy_green_gold", state.AvailableSynergies);
    }

    [Fact]
    public void AllSynergies_HaveValidColorPairs()
    {
        var reg = new TechTreeRegistry();
        foreach (var syn in reg.Synergies)
        {
            Assert.NotEqual(syn.ColorA, syn.ColorB);
            Assert.True(syn.RequiredTierA >= 1 && syn.RequiredTierA <= 6);
            Assert.True(syn.RequiredTierB >= 1 && syn.RequiredTierB <= 6);
        }
    }
}

public class SalvageResearchTests
{
    [Fact]
    public void SameTier_GivesSubsystemPoints()
    {
        var result = SalvageResearchProcessor.Process(PrecursorColor.Red, 2, 2, 100f);
        Assert.Equal(100f, result.SubsystemPoints);
        Assert.Equal(0f, result.TierPoints);
        Assert.False(result.TooAdvanced);
    }

    [Fact]
    public void OneTierAhead_GivesBothPoints()
    {
        var result = SalvageResearchProcessor.Process(PrecursorColor.Red, 3, 2, 100f);
        Assert.Equal(50f, result.SubsystemPoints);
        Assert.Equal(50f, result.TierPoints);
        Assert.False(result.TooAdvanced);
    }

    [Fact]
    public void TwoTiersAhead_GivesTierPointsOnly()
    {
        var result = SalvageResearchProcessor.Process(PrecursorColor.Red, 4, 2, 100f);
        Assert.Equal(0f, result.SubsystemPoints);
        Assert.Equal(70f, result.TierPoints);
        Assert.False(result.TooAdvanced);
    }

    [Fact]
    public void ThreePlusTiersAhead_YieldsComponentsOnly()
    {
        var result = SalvageResearchProcessor.Process(PrecursorColor.Red, 6, 2, 100f);
        Assert.Equal(0f, result.SubsystemPoints);
        Assert.Equal(0f, result.TierPoints);
        Assert.True(result.TooAdvanced);
        Assert.True(result.ComponentsYielded > 0);
    }

    [Fact]
    public void BelowCurrentTier_StillGivesSubsystemPoints()
    {
        var result = SalvageResearchProcessor.Process(PrecursorColor.Blue, 1, 3, 100f);
        Assert.Equal(100f, result.SubsystemPoints);
        Assert.False(result.TooAdvanced);
    }
}

public class ExpertiseTrackerTests
{
    [Fact]
    public void Usage_IncrementsCounter()
    {
        var tracker = new ExpertiseTracker();
        tracker.RecordUsage("weapon_mk1", PrecursorColor.Red, 5);
        Assert.Equal(5, tracker.GetUsageCount("weapon_mk1"));
    }

    [Fact]
    public void SubsystemBonus_ScalesWithUsage()
    {
        var tracker = new ExpertiseTracker();
        float noUse = tracker.GetSubsystemBonus("weapon_mk1");
        Assert.Equal(1.0f, noUse);

        tracker.RecordUsage("weapon_mk1", PrecursorColor.Red, 10);
        float bonus = tracker.GetSubsystemBonus("weapon_mk1");
        Assert.True(bonus > 1.0f);
        Assert.True(bonus < 1.5f); // Diminishing returns
    }

    [Fact]
    public void ColorExpertise_AccumulatesFromUsage()
    {
        var tracker = new ExpertiseTracker();
        tracker.RecordUsage("a", PrecursorColor.Red, 10);
        tracker.RecordUsage("b", PrecursorColor.Red, 10);

        float expertise = tracker.GetColorExpertise(PrecursorColor.Red);
        Assert.True(expertise > 0);

        float bonus = tracker.GetColorBonus(PrecursorColor.Red);
        Assert.True(bonus > 1.0f);
    }
}

public class TechAvailabilityTests
{
    [Fact]
    public void ShipSubsystems_GetShipSubType()
    {
        var reg = new TechTreeRegistry();
        foreach (var sub in reg.Subsystems.Where(s => s.Type == TechModuleType.Ship))
            Assert.NotNull(sub.ShipSubType);
    }

    [Fact]
    public void NonShipSubsystems_HaveNoShipSubType()
    {
        var reg = new TechTreeRegistry();
        foreach (var sub in reg.Subsystems.Where(s => s.Type != TechModuleType.Ship))
            Assert.Null(sub.ShipSubType);
    }

    [Fact]
    public void ResearchedSubsystem_IsAvailableFromResearch()
    {
        var state = new EmpireResearchState();
        state.ResearchedSubsystems.Add("foo");

        Assert.True(state.IsAvailable("foo"));
        Assert.Equal(TechAvailabilitySource.Research, state.GetAvailabilitySource("foo"));
    }

    [Fact]
    public void DiplomaticGrant_IsAvailableFromDiplomacy()
    {
        var state = new EmpireResearchState();
        state.GrantFromDiplomacy("bar");

        Assert.True(state.IsAvailable("bar"));
        Assert.Equal(TechAvailabilitySource.Diplomacy, state.GetAvailabilitySource("bar"));
    }

    [Fact]
    public void ResearchWinsOverDiplomacy_AsAvailabilitySource()
    {
        var state = new EmpireResearchState();
        state.ResearchedSubsystems.Add("baz");
        state.GrantFromDiplomacy("baz");

        // Both sources present — research is authoritative so the grant reverting wouldn't matter.
        Assert.Equal(TechAvailabilitySource.Research, state.GetAvailabilitySource("baz"));
    }

    [Fact]
    public void RevokeDiplomaticGrant_RemovesAvailabilityButPreservesResearch()
    {
        var state = new EmpireResearchState();
        state.GrantFromDiplomacy("rental");
        Assert.True(state.IsAvailable("rental"));

        state.RevokeDiplomaticGrant("rental");
        Assert.False(state.IsAvailable("rental"));
        Assert.Null(state.GetAvailabilitySource("rental"));
    }

    [Fact]
    public void GetProgress_IsOneWhenAvailable()
    {
        var state = new EmpireResearchState();
        state.ResearchedSubsystems.Add("done");
        Assert.Equal(1f, state.GetProgress("done", 100));
    }

    [Fact]
    public void GetProgress_ReportsFractionForCurrentProject()
    {
        var state = new EmpireResearchState
        {
            CurrentProject = "in_flight",
            CurrentProgress = 25f,
        };
        Assert.Equal(0.25f, state.GetProgress("in_flight", 100));
    }

    [Fact]
    public void GetProgress_ZeroForUnstartedTech()
    {
        var state = new EmpireResearchState();
        Assert.Equal(0f, state.GetProgress("untouched", 100));
    }

    [Fact]
    public void UnavailableSubsystem_HasNoSource()
    {
        var state = new EmpireResearchState();
        Assert.False(state.IsAvailable("nope"));
        Assert.Null(state.GetAvailabilitySource("nope"));
    }
}
