using System;
using System.Linq;

namespace DerlictEmpires.Core.Settlements;

/// <summary>
/// Calculates colony happiness as a composite score. Pure C#.
/// Happiness range: 0-100. Affected by food supply, buildings, threats, overcrowding.
/// </summary>
public static class HappinessCalculator
{
    /// <summary>
    /// Calculate happiness for a colony. Returns value 0-100.
    /// </summary>
    public static float Calculate(Colony colony, float threatLevel = 0f)
    {
        float happiness = 50f; // Base happiness

        // Food surplus: +15 if well-fed, -20 if starving
        float foodOutput = colony.EffectiveFoodOutput;
        float foodNeeded = colony.TotalPopulation * 1.0f;
        if (foodOutput >= foodNeeded)
            happiness += 15f;
        else if (foodOutput < foodNeeded * 0.5f)
            happiness -= 20f;
        else
            happiness -= 10f * (1f - foodOutput / foodNeeded);

        // Building bonuses
        happiness += colony.Buildings.Sum(id => BuildingData.FindById(id)?.HappinessBonus ?? 0f);

        // Overcrowding penalty
        if (colony.TotalPopulation > colony.PopCap)
            happiness -= (colony.TotalPopulation - colony.PopCap) * 5f;

        // Threat penalty (nearby enemy fleets, etc.)
        happiness -= threatLevel * 10f;

        // Clamp
        return Math.Clamp(happiness, 0f, 100f);
    }
}
