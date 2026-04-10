using System;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// Checks for hazards when interacting with salvage sites.
/// Probability modified by color affinity, tech gap, and survey quality.
/// Pure C#, deterministic with seeded RNG.
/// </summary>
public static class HazardChecker
{
    public struct HazardResult
    {
        public bool Triggered;
        public HazardType Type;
        public float Severity; // 0-1
    }

    /// <summary>
    /// Roll for a hazard at a salvage site.
    /// </summary>
    /// <param name="siteHazardLevel">Base hazard level (0-1).</param>
    /// <param name="hasAffinity">Whether the empire's affinity matches the site color.</param>
    /// <param name="techGap">Tiers difference between empire tech and site tier (positive = behind).</param>
    /// <param name="surveyQuality">Survey detail level (0-100). Higher reveals hazards beforehand.</param>
    /// <param name="rng">Seeded random for determinism.</param>
    public static HazardResult Check(
        float siteHazardLevel,
        bool hasAffinity,
        int techGap,
        int surveyQuality,
        GameRandom rng)
    {
        // Base trigger probability
        float probability = siteHazardLevel;

        // Affinity reduces risk by 30%
        if (hasAffinity)
            probability *= 0.7f;

        // Tech gap increases risk
        if (techGap > 0)
            probability += techGap * 0.05f;
        else if (techGap < 0)
            probability *= 0.8f; // Ahead of the site's tech = safer

        // High survey quality reduces risk (knowledge = preparation)
        probability *= 1f - (surveyQuality / 200f); // 100 survey = 50% reduction

        probability = Math.Clamp(probability, 0.01f, 0.95f);

        if (!rng.Chance(probability))
            return new HazardResult { Triggered = false, Type = HazardType.None };

        // Determine hazard type
        float[] weights = { 0.25f, 0.25f, 0.20f, 0.15f, 0.15f };
        int typeIdx = rng.WeightedChoice(weights);
        var type = typeIdx switch
        {
            0 => HazardType.AutomatedDefense,
            1 => HazardType.Trap,
            2 => HazardType.Contamination,
            3 => HazardType.GuardianActivation,
            4 => HazardType.StructuralCollapse,
            _ => HazardType.Trap
        };

        float severity = rng.RangeFloat(0.2f, 1.0f);

        return new HazardResult { Triggered = true, Type = type, Severity = severity };
    }
}
