using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Settlements;

/// <summary>
/// Auto-allocates pops based on colony priority. Can also be used to manually
/// set allocations. Pure C#.
/// </summary>
public static class PopAllocationManager
{
    /// <summary>
    /// Auto-allocate all pops in a colony based on its priority setting.
    /// Replaces existing allocations entirely.
    /// </summary>
    public static void AutoAllocate(Colony colony)
    {
        int total = colony.TotalPopulation;
        if (total == 0) return;

        // Clear existing allocations
        colony.PopGroups.Clear();

        // Determine allocation weights based on priority
        var weights = GetPriorityWeights(colony.Priority);

        float totalWeight = weights.Values.Sum();
        var allocated = new Dictionary<WorkPool, int>();
        int remaining = total;

        // Allocate proportionally, ensuring at least 1 to food if pop > 1
        foreach (var (pool, weight) in weights.OrderByDescending(kv => kv.Value))
        {
            int count = (int)(total * (weight / totalWeight));
            count = System.Math.Min(count, remaining);
            allocated[pool] = count;
            remaining -= count;
        }

        // Distribute remainder to highest-weight pool
        if (remaining > 0)
        {
            var topPool = weights.OrderByDescending(kv => kv.Value).First().Key;
            allocated[topPool] = allocated.GetValueOrDefault(topPool) + remaining;
        }

        // Ensure at least 1 food worker if population > 1
        if (total > 1 && allocated.GetValueOrDefault(WorkPool.Food) == 0)
        {
            var topPool = allocated.OrderByDescending(kv => kv.Value).First().Key;
            if (topPool != WorkPool.Food && allocated[topPool] > 1)
            {
                allocated[topPool]--;
                allocated[WorkPool.Food] = allocated.GetValueOrDefault(WorkPool.Food) + 1;
            }
        }

        // Create pop groups
        foreach (var (pool, count) in allocated)
        {
            if (count > 0)
                colony.PopGroups.Add(new PopGroup { Count = count, Allocation = pool });
        }
    }

    private static Dictionary<WorkPool, float> GetPriorityWeights(ColonyPriority priority) => priority switch
    {
        ColonyPriority.ProductionFocus => new()
        {
            [WorkPool.Production] = 0.50f,
            [WorkPool.Food] = 0.20f,
            [WorkPool.Research] = 0.15f,
            [WorkPool.Mining] = 0.15f,
        },
        ColonyPriority.ResearchFocus => new()
        {
            [WorkPool.Research] = 0.50f,
            [WorkPool.Food] = 0.20f,
            [WorkPool.Production] = 0.15f,
            [WorkPool.Mining] = 0.15f,
        },
        ColonyPriority.GrowthFocus => new()
        {
            [WorkPool.Food] = 0.50f,
            [WorkPool.Production] = 0.20f,
            [WorkPool.Research] = 0.15f,
            [WorkPool.Mining] = 0.15f,
        },
        ColonyPriority.MiningFocus => new()
        {
            [WorkPool.Mining] = 0.50f,
            [WorkPool.Food] = 0.20f,
            [WorkPool.Production] = 0.15f,
            [WorkPool.Research] = 0.15f,
        },
        _ => new() // Balanced
        {
            [WorkPool.Production] = 0.25f,
            [WorkPool.Food] = 0.25f,
            [WorkPool.Research] = 0.25f,
            [WorkPool.Mining] = 0.25f,
        },
    };
}
