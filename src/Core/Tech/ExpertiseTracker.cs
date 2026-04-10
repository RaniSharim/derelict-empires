using System;
using System.Collections.Generic;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Tech;

/// <summary>
/// Tracks per-subsystem usage and per-color general expertise.
/// Higher expertise provides diminishing-return bonuses.
/// Pure C#.
/// </summary>
public class ExpertiseTracker
{
    /// <summary>Usage count per subsystem ID.</summary>
    private readonly Dictionary<string, int> _subsystemUsage = new();

    /// <summary>Cumulative expertise per color.</summary>
    private readonly Dictionary<PrecursorColor, float> _colorExpertise = new();

    /// <summary>Record usage of a specific subsystem (e.g., building a ship with it).</summary>
    public void RecordUsage(string subsystemId, PrecursorColor color, int count = 1)
    {
        _subsystemUsage[subsystemId] = _subsystemUsage.GetValueOrDefault(subsystemId) + count;
        _colorExpertise[color] = _colorExpertise.GetValueOrDefault(color) + count * 0.5f;
    }

    /// <summary>Get usage count for a specific subsystem.</summary>
    public int GetUsageCount(string subsystemId) =>
        _subsystemUsage.GetValueOrDefault(subsystemId);

    /// <summary>Get cumulative expertise for a color.</summary>
    public float GetColorExpertise(PrecursorColor color) =>
        _colorExpertise.GetValueOrDefault(color);

    /// <summary>
    /// Get the expertise bonus multiplier for a subsystem.
    /// Diminishing returns: bonus = 1 + log2(1 + usage) * 0.05
    /// Max ~1.33 at usage=100.
    /// </summary>
    public float GetSubsystemBonus(string subsystemId)
    {
        int usage = GetUsageCount(subsystemId);
        if (usage <= 0) return 1.0f;
        return 1.0f + MathF.Log2(1 + usage) * 0.05f;
    }

    /// <summary>
    /// Get the general color expertise bonus.
    /// Diminishing returns: bonus = 1 + log2(1 + expertise) * 0.03
    /// </summary>
    public float GetColorBonus(PrecursorColor color)
    {
        float expertise = GetColorExpertise(color);
        if (expertise <= 0) return 1.0f;
        return 1.0f + MathF.Log2(1 + expertise) * 0.03f;
    }
}
