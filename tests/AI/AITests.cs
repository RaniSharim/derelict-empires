using Xunit;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.AI;

namespace DerlictEmpires.Tests.AI;

// Concrete test actions
public class BuildFleetAction : AIAction
{
    public override string Id => "build_fleet";
    public override ActionCategory Category => ActionCategory.Military;
    public override float Score(AIGameView state) =>
        state.ThreatLevel * (state.StrongestNeighborMilitary / System.Math.Max(state.MilitaryStrength, 1f));
    public override void Execute(AIGameView state) => state.OnActionExecuted?.Invoke(Id);
}

public class EstablishTradeAction : AIAction
{
    public override string Id => "establish_trade";
    public override ActionCategory Category => ActionCategory.Economy;
    public override float Score(AIGameView state) => state.TradeOpportunity;
    public override void Execute(AIGameView state) => state.OnActionExecuted?.Invoke(Id);
}

public class ExpandAction : AIAction
{
    public override string Id => "expand";
    public override ActionCategory Category => ActionCategory.Expansion;
    public override float Score(AIGameView state) =>
        state.AvailableExpansionSites * 0.5f;
    public override void Execute(AIGameView state) => state.OnActionExecuted?.Invoke(Id);
}

public class UtilityBrainTests
{
    private static List<AIAction> AllActions() => new()
    {
        new BuildFleetAction(),
        new EstablishTradeAction(),
        new ExpandAction()
    };

    [Fact]
    public void RedWarrior_PrioritizesMilitary()
    {
        var brain = new UtilityBrain(PersonalityPresets.RedWarrior(), AllActions(), DifficultySettings.Normal);

        var state = new AIGameView
        {
            EmpireId = 0,
            ThreatLevel = 3f,
            MilitaryStrength = 10f,
            StrongestNeighborMilitary = 30f,
            TradeOpportunity = 3f,
            AvailableExpansionSites = 3
        };

        var executed = new List<string>();
        state.OnActionExecuted = id => executed.Add(id);

        var results = brain.Evaluate(state);

        // Military should score highest for Red Warrior with high threat
        Assert.Equal("build_fleet", results[0].actionId);
    }

    [Fact]
    public void GoldHauler_PrioritizesTrade()
    {
        var brain = new UtilityBrain(PersonalityPresets.GoldHauler(), AllActions(), DifficultySettings.Normal);

        var state = new AIGameView
        {
            EmpireId = 0,
            ThreatLevel = 1f,
            MilitaryStrength = 20f,
            StrongestNeighborMilitary = 15f,
            TradeOpportunity = 5f,
            AvailableExpansionSites = 2
        };

        var executed = new List<string>();
        state.OnActionExecuted = id => executed.Add(id);

        var results = brain.Evaluate(state);

        Assert.Equal("establish_trade", results[0].actionId);
    }

    [Fact]
    public void Difficulty_LimitsActions()
    {
        var brain = new UtilityBrain(PersonalityPresets.RedWarrior(), AllActions(), DifficultySettings.Easy);

        var state = new AIGameView
        {
            ThreatLevel = 2f, MilitaryStrength = 10f, StrongestNeighborMilitary = 20f,
            TradeOpportunity = 3f, AvailableExpansionSites = 5
        };

        var results = brain.Evaluate(state);
        Assert.True(results.Count <= DifficultySettings.Easy.ActionsPerTick);
    }

    [Fact]
    public void ZeroScoreActions_NotExecuted()
    {
        var brain = new UtilityBrain(PersonalityPresets.RedWarrior(), AllActions(), DifficultySettings.Normal);

        var state = new AIGameView
        {
            ThreatLevel = 0f, // No threat → military scores 0
            MilitaryStrength = 0f,
            StrongestNeighborMilitary = 0f,
            TradeOpportunity = 0f, // No trade opportunity
            AvailableExpansionSites = 0 // No expansion
        };

        var results = brain.Evaluate(state);
        Assert.Empty(results); // All scores are 0
    }
}
