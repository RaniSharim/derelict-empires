using System;
using System.Collections.Generic;
using System.Linq;

namespace DerlictEmpires.Core.AI;

/// <summary>Category of AI actions for personality weighting.</summary>
public enum ActionCategory
{
    Military, Economy, Diplomacy, Espionage, Research, Exploration, Expansion
}

/// <summary>AI personality: weight multipliers per action category.</summary>
public class AIPersonality
{
    public string Name { get; set; } = "";
    public Dictionary<ActionCategory, float> Weights { get; set; } = new();

    public float GetWeight(ActionCategory category) =>
        Weights.GetValueOrDefault(category, 1.0f);
}

/// <summary>AI difficulty parameters.</summary>
public class DifficultySettings
{
    public string Name { get; set; } = "";
    public int ActionsPerTick { get; set; } = 3;
    public float ResourceBonus { get; set; } = 1.0f;
    public int EvaluationInterval { get; set; } = 10; // slow ticks between re-evaluations

    public static readonly DifficultySettings Easy = new() { Name = "Easy", ActionsPerTick = 2, ResourceBonus = 1.0f, EvaluationInterval = 30 };
    public static readonly DifficultySettings Normal = new() { Name = "Normal", ActionsPerTick = 3, ResourceBonus = 1.0f, EvaluationInterval = 15 };
    public static readonly DifficultySettings Hard = new() { Name = "Hard", ActionsPerTick = 4, ResourceBonus = 1.2f, EvaluationInterval = 8 };
    public static readonly DifficultySettings Expert = new() { Name = "Expert", ActionsPerTick = 5, ResourceBonus = 1.5f, EvaluationInterval = 5 };
}

/// <summary>An action the AI can take, scored by utility.</summary>
public abstract class AIAction
{
    public abstract string Id { get; }
    public abstract ActionCategory Category { get; }
    public abstract float Score(AIGameView state);
    public abstract void Execute(AIGameView state);
}

/// <summary>Read-only view of game state for AI evaluation.</summary>
public class AIGameView
{
    public int EmpireId { get; set; }
    public float MilitaryStrength { get; set; }
    public float StrongestNeighborMilitary { get; set; }
    public float ThreatLevel { get; set; }
    public int ColonyCount { get; set; }
    public int AvailableExpansionSites { get; set; }
    public float TechLevel { get; set; }
    public float NeighborAvgTechLevel { get; set; }
    public float ResourceSurplus { get; set; }
    public float TradeOpportunity { get; set; }
    public int UnexploredSystems { get; set; }
    public float DiplomaticClimate { get; set; } // -1 hostile to 1 friendly

    // AI executes actions by calling back into game systems
    public Action<string>? OnActionExecuted { get; set; }
}

/// <summary>
/// Utility-based AI brain. Scores all actions, selects top N, executes.
/// Pure C#.
/// </summary>
public class UtilityBrain
{
    private readonly AIPersonality _personality;
    private readonly List<AIAction> _actions;
    private readonly DifficultySettings _difficulty;

    // Reused scratch buffers — Evaluate is allocation-free per call.
    // The result list is reused too: callers consume it before the next Evaluate.
    private readonly (AIAction action, float score)[] _scored;
    private readonly List<(string, float)> _results = new();

    // Single shared comparer instance — Comparer.Create allocates each call otherwise.
    private static readonly IComparer<(AIAction action, float score)> _scoreDescending =
        Comparer<(AIAction action, float score)>.Create((x, y) => y.score.CompareTo(x.score));

    public UtilityBrain(AIPersonality personality, List<AIAction> actions, DifficultySettings difficulty)
    {
        _personality = personality;
        _actions = actions;
        _difficulty = difficulty;
        _scored = new (AIAction, float)[actions.Count];
    }

    /// <summary>Evaluate all actions, execute top N. Returned list is reused — copy if retaining.</summary>
    public List<(string actionId, float score)> Evaluate(AIGameView state)
    {
        // Score every action; track how many had positive scores in the prefix.
        int positiveCount = 0;
        for (int i = 0; i < _actions.Count; i++)
        {
            var a = _actions[i];
            float score = a.Score(state) * _personality.GetWeight(a.Category);
            if (score > 0f)
                _scored[positiveCount++] = (a, score);
        }

        // Sort the positive prefix in place by score descending using the cached comparer.
        Array.Sort(_scored, 0, positiveCount, _scoreDescending);

        int take = System.Math.Min(_difficulty.ActionsPerTick, positiveCount);
        _results.Clear();
        for (int i = 0; i < take; i++)
        {
            var (action, score) = _scored[i];
            action.Execute(state);
            _results.Add((action.Id, score));
        }

        return _results;
    }
}

/// <summary>Preset personalities from DESIGN.md §14.5.</summary>
public static class PersonalityPresets
{
    public static AIPersonality RedWarrior() => new()
    {
        Name = "Red Warrior",
        Weights = new()
        {
            [ActionCategory.Military] = 1.5f,
            [ActionCategory.Economy] = 0.7f,
            [ActionCategory.Diplomacy] = 0.6f,
            [ActionCategory.Espionage] = 0.8f,
            [ActionCategory.Research] = 1.0f,
            [ActionCategory.Exploration] = 0.9f,
            [ActionCategory.Expansion] = 1.2f,
        }
    };

    public static AIPersonality GoldHauler() => new()
    {
        Name = "Gold Hauler",
        Weights = new()
        {
            [ActionCategory.Military] = 0.7f,
            [ActionCategory.Economy] = 1.5f,
            [ActionCategory.Diplomacy] = 1.3f,
            [ActionCategory.Espionage] = 0.5f,
            [ActionCategory.Research] = 1.0f,
            [ActionCategory.Exploration] = 1.3f,
            [ActionCategory.Expansion] = 1.0f,
        }
    };

    public static AIPersonality BlueChronicler() => new()
    {
        Name = "Blue Chronicler",
        Weights = new()
        {
            [ActionCategory.Military] = 0.8f,
            [ActionCategory.Economy] = 0.9f,
            [ActionCategory.Diplomacy] = 1.0f,
            [ActionCategory.Espionage] = 1.5f,
            [ActionCategory.Research] = 1.4f,
            [ActionCategory.Exploration] = 1.2f,
            [ActionCategory.Expansion] = 0.8f,
        }
    };

    public static AIPersonality GreenServitor() => new()
    {
        Name = "Green Servitor",
        Weights = new()
        {
            [ActionCategory.Military] = 0.6f,
            [ActionCategory.Economy] = 1.0f,
            [ActionCategory.Diplomacy] = 1.2f,
            [ActionCategory.Espionage] = 0.5f,
            [ActionCategory.Research] = 1.5f,
            [ActionCategory.Exploration] = 1.0f,
            [ActionCategory.Expansion] = 1.2f,
        }
    };

    public static AIPersonality PurpleWarrior() => new()
    {
        Name = "Purple Warrior",
        Weights = new()
        {
            [ActionCategory.Military] = 1.3f,
            [ActionCategory.Economy] = 0.8f,
            [ActionCategory.Diplomacy] = 0.7f,
            [ActionCategory.Espionage] = 1.3f,
            [ActionCategory.Research] = 1.2f,
            [ActionCategory.Exploration] = 1.0f,
            [ActionCategory.Expansion] = 1.0f,
        }
    };
}
