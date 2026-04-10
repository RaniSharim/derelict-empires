using System;
using System.Collections.Generic;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Tech;

/// <summary>
/// Calculates tech efficiency based on empire affinity vs tech color.
/// Specialized (own color) = 1.0, Adjacent = 0.7, Distant = 0.4.
/// Pure C#.
/// </summary>
public static class EfficiencyCalculator
{
    // Color adjacency wheel: Red-Blue-Green-Gold-Purple-Red
    private static readonly Dictionary<PrecursorColor, PrecursorColor[]> Adjacent = new()
    {
        [PrecursorColor.Red] = new[] { PrecursorColor.Blue, PrecursorColor.Purple },
        [PrecursorColor.Blue] = new[] { PrecursorColor.Red, PrecursorColor.Green },
        [PrecursorColor.Green] = new[] { PrecursorColor.Blue, PrecursorColor.Gold },
        [PrecursorColor.Gold] = new[] { PrecursorColor.Green, PrecursorColor.Purple },
        [PrecursorColor.Purple] = new[] { PrecursorColor.Gold, PrecursorColor.Red },
    };

    /// <summary>
    /// Get efficiency multiplier for an empire's affinity working with a tech color.
    /// Returns 1.0 (specialized), 0.7 (adjacent), or 0.4 (distant).
    /// Null affinity (Free Race) returns 0.6 for all colors.
    /// </summary>
    public static float GetEfficiency(PrecursorColor? empireAffinity, PrecursorColor techColor)
    {
        if (!empireAffinity.HasValue) return 0.6f; // Free Race

        if (empireAffinity.Value == techColor) return 1.0f;

        if (Array.IndexOf(Adjacent[empireAffinity.Value], techColor) >= 0)
            return 0.7f;

        return 0.4f;
    }

    /// <summary>Check if two colors are adjacent on the wheel.</summary>
    public static bool AreAdjacent(PrecursorColor a, PrecursorColor b) =>
        Array.IndexOf(Adjacent[a], b) >= 0;
}
