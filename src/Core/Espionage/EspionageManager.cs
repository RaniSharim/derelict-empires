using System;
using System.Collections.Generic;
using System.Linq;

namespace DerlictEmpires.Core.Espionage;

public enum IntelCategory { Map, Activity, Technology, Resource, Construction, Salvage }

/// <summary>
/// Abstracted espionage system. Empires invest currency to gain intelligence
/// on other empires. Knowledge grows over time based on investment vs counter-intel.
/// Pure C#.
/// </summary>
public class EspionageManager
{
    // Key: "observer_target"
    private readonly Dictionary<string, Dictionary<IntelCategory, float>> _knowledge = new();
    private readonly Dictionary<int, float> _counterIntelBudget = new();

    public event Action<int, int, IntelCategory, float>? IntelChanged; // observer, target, category, newLevel

    private static string Key(int observer, int target) => $"{observer}_{target}";

    /// <summary>Get knowledge level (0.0-1.0) in a category for one empire about another.</summary>
    public float GetKnowledge(int observerEmpireId, int targetEmpireId, IntelCategory category)
    {
        var key = Key(observerEmpireId, targetEmpireId);
        if (!_knowledge.TryGetValue(key, out var cats)) return 0f;
        return cats.GetValueOrDefault(category);
    }

    /// <summary>Set counter-intelligence budget for an empire.</summary>
    public void SetCounterIntelBudget(int empireId, float budget)
    {
        _counterIntelBudget[empireId] = budget;
    }

    /// <summary>
    /// Process one tick of espionage. Investment increases knowledge;
    /// counter-intelligence reduces it.
    /// </summary>
    public void ProcessTick(int observerEmpireId, int targetEmpireId,
        IntelCategory category, float investment, float observerBlueTech, float targetBlueTech)
    {
        float counterIntel = _counterIntelBudget.GetValueOrDefault(targetEmpireId);

        // Effectiveness = (investment * blueTechBonus) / (1 + counterIntel * targetBlueTech)
        float attackPower = investment * (1f + observerBlueTech * 0.1f);
        float defensePower = 1f + counterIntel * (1f + targetBlueTech * 0.1f) * 0.01f;
        float gain = attackPower / defensePower * 0.01f; // Scale down for per-tick

        gain = MathF.Max(gain, 0f);

        var key = Key(observerEmpireId, targetEmpireId);
        if (!_knowledge.ContainsKey(key))
            _knowledge[key] = new Dictionary<IntelCategory, float>();

        float current = _knowledge[key].GetValueOrDefault(category);
        float newLevel = MathF.Min(current + gain, 1.0f);
        _knowledge[key][category] = newLevel;

        if (MathF.Abs(newLevel - current) > 0.001f)
            IntelChanged?.Invoke(observerEmpireId, targetEmpireId, category, newLevel);
    }

    /// <summary>Apply passive intelligence from trade agreements.</summary>
    public void ApplyPassiveIntel(int empireA, int empireB, float tradeVolume)
    {
        float gain = tradeVolume * 0.001f;
        AddKnowledge(empireA, empireB, IntelCategory.Resource, gain);
        AddKnowledge(empireA, empireB, IntelCategory.Activity, gain * 0.5f);
        AddKnowledge(empireB, empireA, IntelCategory.Resource, gain);
        AddKnowledge(empireB, empireA, IntelCategory.Activity, gain * 0.5f);
    }

    private void AddKnowledge(int observer, int target, IntelCategory category, float amount)
    {
        var key = Key(observer, target);
        if (!_knowledge.ContainsKey(key))
            _knowledge[key] = new Dictionary<IntelCategory, float>();

        float current = _knowledge[key].GetValueOrDefault(category);
        _knowledge[key][category] = MathF.Min(current + amount, 1.0f);
    }
}
