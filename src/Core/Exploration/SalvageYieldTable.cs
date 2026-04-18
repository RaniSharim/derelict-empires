using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// Per-SalvageSiteType scan difficulty + yield ranges. Pure C#, data-driven.
/// Values match the MVP tuning table (plan §8.1).
/// </summary>
public static class SalvageYieldTable
{
    public readonly struct Profile
    {
        public readonly float ScanDifficulty;
        public readonly float MinTotalYield;
        public readonly float MaxTotalYield;
        public readonly int MinResources;
        public readonly int MaxResources;
        public readonly float PureColorBias;  // 0..1 — chance that each resource rolls the primary color
        public readonly float ComponentWeight; // 0..1 — skew toward BasicComponent/AdvancedComponent

        public Profile(float scan, float minYield, float maxYield, int minRes, int maxRes, float pure, float comp)
        {
            ScanDifficulty = scan;
            MinTotalYield = minYield;
            MaxTotalYield = maxYield;
            MinResources = minRes;
            MaxResources = maxRes;
            PureColorBias = pure;
            ComponentWeight = comp;
        }
    }

    public static Profile Get(SalvageSiteType type) => type switch
    {
        SalvageSiteType.MinorDerelict        => new Profile(150f,  30f,  80f, 1, 2, 1.0f, 0.3f),
        SalvageSiteType.DebrisField          => new Profile(250f,  60f, 140f, 2, 4, 0.4f, 0.2f),
        SalvageSiteType.ShipGraveyard        => new Profile(500f, 200f, 400f, 2, 3, 1.0f, 0.4f),
        SalvageSiteType.MajorPrecursorSite   => new Profile(450f, 180f, 350f, 2, 3, 1.0f, 0.5f),
        SalvageSiteType.PrecursorIntersection => new Profile(350f, 120f, 240f, 3, 4, 0.5f, 0.3f),
        SalvageSiteType.FailedSalvagerWreck  => new Profile(200f,  50f, 120f, 1, 2, 0.3f, 0.4f),
        SalvageSiteType.DesperationProject   => new Profile(300f, 100f, 200f, 2, 3, 0.7f, 0.5f),
        _                                    => new Profile(250f,  60f, 140f, 2, 3, 0.7f, 0.3f),
    };

    /// <summary>
    /// Generate a yield dictionary for a site. Key format matches EmpireData.ResourceKey.
    /// </summary>
    public static Dictionary<string, float> GenerateYield(
        SalvageSiteType type, PrecursorColor primaryColor, GameRandom rng)
    {
        var p = Get(type);
        float totalAmount = rng.RangeFloat(p.MinTotalYield, p.MaxTotalYield);
        int resourceCount = rng.RangeInt(p.MinResources, p.MaxResources + 1);

        // Pick resource (color, type) pairs.
        var picks = new List<(PrecursorColor color, ResourceType type)>();
        for (int i = 0; i < resourceCount; i++)
        {
            var color = rng.NextFloat() < p.PureColorBias
                ? primaryColor
                : (PrecursorColor)rng.RangeInt(5);

            var rtype = rng.NextFloat() < p.ComponentWeight
                ? (rng.NextFloat() < 0.5f ? ResourceType.BasicComponent : ResourceType.AdvancedComponent)
                : PickNonComponent(rng);

            picks.Add((color, rtype));
        }

        // Distribute total amount proportional to random weights per pick.
        var weights = new float[picks.Count];
        float wSum = 0f;
        for (int i = 0; i < picks.Count; i++)
        {
            weights[i] = rng.RangeFloat(0.5f, 1.5f);
            wSum += weights[i];
        }

        var result = new Dictionary<string, float>();
        for (int i = 0; i < picks.Count; i++)
        {
            var key = EmpireData.ResourceKey(picks[i].color, picks[i].type);
            float amount = totalAmount * weights[i] / wSum;
            result[key] = result.GetValueOrDefault(key) + amount;
        }
        return result;
    }

    private static ResourceType PickNonComponent(GameRandom rng)
    {
        // MVP simple-tier bias: most yields are SimpleOre / SimpleEnergy; advanced is rarer.
        float r = rng.NextFloat();
        if (r < 0.45f) return ResourceType.SimpleOre;
        if (r < 0.80f) return ResourceType.SimpleEnergy;
        if (r < 0.92f) return ResourceType.AdvancedOre;
        return ResourceType.AdvancedEnergy;
    }
}
