using System;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Tech;

/// <summary>
/// Processes salvage-driven research based on tech gap rules from DESIGN.md §5.2.2.
/// Pure C#.
/// </summary>
public static class SalvageResearchProcessor
{
    public struct SalvageResult
    {
        /// <summary>Research points toward a specific subsystem.</summary>
        public float SubsystemPoints;
        /// <summary>Research points toward the tier unlock.</summary>
        public float TierPoints;
        /// <summary>Components yielded (when gap is too large for learning).</summary>
        public int ComponentsYielded;
        /// <summary>Whether the salvage was too advanced to learn from.</summary>
        public bool TooAdvanced;
    }

    /// <summary>
    /// Calculate research reward from salvaging an artifact.
    /// </summary>
    /// <param name="artifactColor">Color of the salvaged artifact.</param>
    /// <param name="artifactTier">Tech tier of the artifact (1-6).</param>
    /// <param name="currentTier">Empire's current unlocked tier in this color (any category).</param>
    /// <param name="basePoints">Base research points from the artifact's value.</param>
    public static SalvageResult Process(
        PrecursorColor artifactColor,
        int artifactTier,
        int currentTier,
        float basePoints)
    {
        int gap = artifactTier - currentTier;

        if (gap <= 0)
        {
            // Same tier or below: points toward specific subsystem
            return new SalvageResult
            {
                SubsystemPoints = basePoints,
                TierPoints = 0f,
                ComponentsYielded = 0,
                TooAdvanced = false
            };
        }

        if (gap == 1)
        {
            // 1 tier ahead: points toward both tier unlock and subsystem
            return new SalvageResult
            {
                SubsystemPoints = basePoints * 0.5f,
                TierPoints = basePoints * 0.5f,
                ComponentsYielded = 0,
                TooAdvanced = false
            };
        }

        if (gap == 2)
        {
            // 2 tiers ahead: tier unlock points only
            return new SalvageResult
            {
                SubsystemPoints = 0f,
                TierPoints = basePoints * 0.7f,
                ComponentsYielded = 0,
                TooAdvanced = false
            };
        }

        // 3+ tiers ahead: too advanced, yields components only
        return new SalvageResult
        {
            SubsystemPoints = 0f,
            TierPoints = 0f,
            ComponentsYielded = Math.Max(1, (int)(basePoints / 10f)),
            TooAdvanced = true
        };
    }
}
