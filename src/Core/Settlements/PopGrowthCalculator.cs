using System;

namespace DerlictEmpires.Core.Settlements;

/// <summary>
/// Calculates population growth for colonies. Pure C#.
/// Growth is driven by food surplus and happiness. Colonies only (not outposts).
/// </summary>
public static class PopGrowthCalculator
{
    /// <summary>Food surplus needed to trigger one population growth.</summary>
    public const float GrowthThreshold = 30f;

    /// <summary>
    /// Process one slow tick of growth for a colony.
    /// Accumulates food surplus; when threshold is reached, adds 1 pop.
    /// Returns true if population grew.
    /// </summary>
    public static bool ProcessTick(Colony colony, float tickDelta)
    {
        if (colony.TotalPopulation >= colony.PopCap) return false;

        float foodOutput = colony.EffectiveFoodOutput;
        float foodConsumed = colony.TotalPopulation * 1.0f; // 1 food per pop per tick
        float surplus = (foodOutput - foodConsumed) * tickDelta;

        if (surplus <= 0f)
        {
            // Starvation: slowly drain surplus buffer
            colony.FoodSurplus = Math.Max(0f, colony.FoodSurplus + surplus);

            // If surplus hits zero and food is negative, population dies
            if (colony.FoodSurplus <= 0f && surplus < -0.5f)
            {
                return RemovePop(colony);
            }
            return false;
        }

        // Happiness scaling: growth rate scales with happiness
        float happinessMultiplier = colony.Happiness >= 50f
            ? 0.5f + (colony.Happiness / 100f)
            : colony.Happiness / 100f;

        colony.FoodSurplus += surplus * happinessMultiplier;

        if (colony.FoodSurplus >= GrowthThreshold)
        {
            colony.FoodSurplus -= GrowthThreshold;
            AddPop(colony);
            return true;
        }

        return false;
    }

    private static void AddPop(Colony colony)
    {
        // Add to the largest pop group, or create a new one
        if (colony.PopGroups.Count > 0)
        {
            var largest = colony.PopGroups[0];
            foreach (var pg in colony.PopGroups)
                if (pg.Count > largest.Count) largest = pg;
            largest.Count++;
        }
        else
        {
            colony.PopGroups.Add(new PopGroup
            {
                Count = 1,
                Allocation = DerlictEmpires.Core.Enums.WorkPool.Food
            });
        }
    }

    private static bool RemovePop(Colony colony)
    {
        if (colony.TotalPopulation <= 1) return false;

        // Remove from the largest pop group
        if (colony.PopGroups.Count > 0)
        {
            var largest = colony.PopGroups[0];
            foreach (var pg in colony.PopGroups)
                if (pg.Count > largest.Count) largest = pg;
            largest.Count--;
            if (largest.Count <= 0)
                colony.PopGroups.Remove(largest);
            return true;
        }
        return false;
    }
}
