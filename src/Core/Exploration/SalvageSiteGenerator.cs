using System;
using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// Generates salvage sites at galaxy-gen time using the JSON-loaded
/// <see cref="SalvageRegistry"/>. Replaces the legacy
/// <c>SalvagePlacement</c> + <c>SalvageYieldTable</c> pair.
/// Pure C#, deterministic given the same seed.
/// </summary>
public static class SalvageSiteGenerator
{
    /// <summary>Fraction of eligible POIs that spawn a salvage site (mirrors legacy).</summary>
    public const float SalvageChance = 0.4f;

    /// <summary>Minimum tier rolled at site generation. Tweak here to widen the curve.</summary>
    public const int MinTier = 1;

    /// <summary>Maximum tier (recursive multi-color tier bonus also caps at this value).</summary>
    public const int MaxTier = 5;

    /// <summary>Hard cap on how many layers a site can hold; matched in registry validation.</summary>
    public const int MaxLayers = 5;

    /// <summary>Layer scan difficulty multiplier per depth = 1 + index * DepthDifficultyStep.</summary>
    public const float DepthDifficultyStep = 0.20f;

    /// <summary>Layer yield multiplier per depth = 1 + index * DepthYieldStep.</summary>
    public const float DepthYieldStep = 0.10f;

    public static List<SalvageSiteData> Populate(
        GalaxyData galaxy,
        SalvageRegistry registry,
        GameRandom rng)
    {
        var sites = new List<SalvageSiteData>();

        foreach (var system in galaxy.Systems)
        {
            var armPair = galaxy.GetArmPair(system.ArmIndex);

            foreach (var poi in system.POIs)
            {
                if (!IsEligible(poi.Type)) continue;
                if (!rng.Chance(SalvageChance)) continue;

                var typeDef = PickType(poi.Type, registry, rng);
                if (typeDef == null) continue; // no type accepts this POI

                var site = BuildSite(
                    siteId: sites.Count,
                    poi: poi,
                    typeDef: typeDef,
                    registry: registry,
                    armPair: armPair,
                    radialPosition: system.RadialPosition,
                    rng: rng);

                sites.Add(site);
                poi.SalvageSiteId = site.Id;
            }
        }
        return sites;
    }

    /// <summary>True if the POI type may host a salvage site.</summary>
    public static bool IsEligible(POIType type) => type switch
    {
        POIType.HabitablePlanet => false,
        POIType.BarrenPlanet    => false,
        _                       => true,
    };

    /// <summary>Pick a site type whose <c>EligiblePOIWeights</c> includes the POI type. Null if none.</summary>
    public static SalvageSiteTypeDef? PickType(
        POIType poiType, SalvageRegistry registry, GameRandom rng)
    {
        // Build a weighted candidate list: every type whose JSON declares weight > 0
        // for this POIType, weighted by that entry.
        var candidates = new List<SalvageSiteTypeDef>();
        var weights = new List<float>();
        foreach (var t in registry.Types.Values)
        {
            if (t.EligiblePOIWeights.TryGetValue(poiType.ToString(), out var w) && w > 0f)
            {
                candidates.Add(t);
                weights.Add(w);
            }
        }
        if (candidates.Count == 0) return null;
        int idx = rng.WeightedChoice(weights);
        return candidates[idx];
    }

    public static SalvageSiteData BuildSite(
        int siteId,
        POIData poi,
        SalvageSiteTypeDef typeDef,
        SalvageRegistry registry,
        ArmColorPair? armPair,
        float radialPosition,
        GameRandom rng)
    {
        // 1. Tier — uniform [MinTier, MaxTier], plus bonus from recursive multi-color rule.
        int baseTier = rng.RangeInt(MinTier, MaxTier + 1);
        var colorRoll = SalvageColorRoller.Roll(armPair, radialPosition, rng);
        int tier = Math.Clamp(baseTier + colorRoll.TierBonus, MinTier, MaxTier);

        // 2. Layer count (capped at MaxLayers).
        int layerCount = Math.Clamp(
            rng.RangeInt(typeDef.LayerCountMin, typeDef.LayerCountMax + 1), 1, MaxLayers);

        // 3. Build layers.
        var layers = new List<SalvageLayer>(layerCount);
        for (int i = 0; i < layerCount; i++)
        {
            var layer = BuildLayer(i, layerCount, typeDef, registry, colorRoll.Colors, tier, rng);
            layers.Add(layer);
        }

        // 4. Site name.
        string name = ComposeName(typeDef, poi, rng);

        return new SalvageSiteData
        {
            Id = siteId,
            POIId = poi.Id,
            TypeId = typeDef.Id,
            Name = name,
            Tier = tier,
            Colors = colorRoll.Colors,
            Layers = layers,
            Visibility = typeDef.BaseScanPerLayer * tier,
            SpecialOutcomeId = typeDef.SpecialOutcomeId,
            DepletionCurveExponent = 0.5f,
        };
    }

    private static SalvageLayer BuildLayer(
        int index, int layerCount,
        SalvageSiteTypeDef typeDef,
        SalvageRegistry registry,
        IReadOnlyList<PrecursorColor> siteColors,
        int tier,
        GameRandom rng)
    {
        // Layer color: uniform pick from the site's colors.
        var layerColor = siteColors[rng.RangeInt(siteColors.Count)];

        float depthScan = typeDef.BaseScanPerLayer * (1f + index * DepthDifficultyStep);
        float depthYield = (1f + index * DepthYieldStep) * tier;

        float minY = typeDef.LayerYieldMin * depthYield;
        float maxY = typeDef.LayerYieldMax * depthYield;
        float totalAmount = rng.RangeFloat(minY, maxY);

        var yield = BuildYield(layerColor, totalAmount, typeDef.ComponentBias, rng);

        // Research unlock chance: clamped lookup. Trailing entries reused if the JSON array
        // is shorter than the layer index.
        float researchChance = typeDef.ResearchChancePerLayer.Length > 0
            ? typeDef.ResearchChancePerLayer[Math.Min(index, typeDef.ResearchChancePerLayer.Length - 1)]
            : 0f;

        // Danger.
        string dangerId = typeDef.DangerTypeIds.Length > 0
            ? typeDef.DangerTypeIds[rng.RangeInt(typeDef.DangerTypeIds.Length)]
            : "damage";
        var dangerDef = registry.GetDanger(dangerId);
        float dangerSeverity = dangerDef != null
            ? dangerDef.BaseSeverity + tier * dangerDef.PerTierBonus
            : 0f;

        return new SalvageLayer
        {
            Index = index,
            LayerColor = layerColor,
            ResearchTargetTier = tier,
            ResearchUnlockChance = researchChance,
            Yield = yield,
            RemainingYield = new Dictionary<string, float>(yield),
            ScanDifficulty = depthScan,
            DangerTypeId = dangerId,
            DangerChance = Math.Clamp(tier * 0.10f, 0f, 1f),
            DangerSeverity = dangerSeverity,
        };
    }

    /// <summary>
    /// Roll 1-2 (color, type) yield picks under the layer's color and type's
    /// component bias, distributing <paramref name="totalAmount"/> among them
    /// proportional to random weights. Mirrors the old yield-table behavior
    /// minus the legacy "pure color" knob (color is fixed by the layer).
    /// </summary>
    private static Dictionary<string, float> BuildYield(
        PrecursorColor color, float totalAmount, float componentBias, GameRandom rng)
    {
        int picks = rng.RangeInt(1, 3); // 1..2
        var entries = new (PrecursorColor color, ResourceType type)[picks];
        var weights = new float[picks];
        float wSum = 0f;
        for (int i = 0; i < picks; i++)
        {
            ResourceType t = rng.NextFloat() < componentBias
                ? (rng.NextFloat() < 0.5f ? ResourceType.BasicComponent : ResourceType.AdvancedComponent)
                : PickNonComponent(rng);
            entries[i] = (color, t);
            weights[i] = rng.RangeFloat(0.5f, 1.5f);
            wSum += weights[i];
        }

        var result = new Dictionary<string, float>();
        for (int i = 0; i < picks; i++)
        {
            string key = EmpireData.ResourceKey(entries[i].color, entries[i].type);
            result[key] = result.GetValueOrDefault(key) + totalAmount * weights[i] / wSum;
        }
        return result;
    }

    private static ResourceType PickNonComponent(GameRandom rng)
    {
        float r = rng.NextFloat();
        if (r < 0.45f) return ResourceType.SimpleOre;
        if (r < 0.80f) return ResourceType.SimpleEnergy;
        if (r < 0.92f) return ResourceType.AdvancedOre;
        return ResourceType.AdvancedEnergy;
    }

    // ── Naming ───────────────────────────────────────────────────

    private static readonly string[] Adjectives =
    {
        "Ashen", "Cold", "Cracked", "Dim", "Forgotten", "Gilded", "Hollow",
        "Iron", "Lonesome", "Pale", "Riven", "Shattered", "Silent", "Twin",
        "Veiled", "Withered"
    };

    private static readonly string[] Cognomens =
    {
        "Cassander", "Drovin", "Eluria", "Halix", "Iskandar", "Jorell",
        "Korath", "Liora", "Marenna", "Nekos", "Orven", "Petros",
        "Quill", "Rhea", "Suren", "Talos", "Ulla", "Varos", "Wynd", "Xera"
    };

    private static string ComposeName(SalvageSiteTypeDef typeDef, POIData poi, GameRandom rng)
    {
        if (typeDef.NameTemplates.Length == 0)
            return $"{typeDef.DisplayName} #{poi.Id}";

        string template = typeDef.NameTemplates[rng.RangeInt(typeDef.NameTemplates.Length)];
        string adj = Adjectives[rng.RangeInt(Adjectives.Length)];
        string nameTok = Cognomens[rng.RangeInt(Cognomens.Length)];
        string number = (rng.RangeInt(10, 100)).ToString();
        return template.Replace("{Adj}", adj).Replace("{Name}", nameTok).Replace("{Number}", number);
    }
}
