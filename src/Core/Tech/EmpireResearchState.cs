using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Core.Tech;

/// <summary>
/// Per-empire research state: unlocked tiers, available/unlocked subsystems,
/// current research project, queue, and expertise.
/// </summary>
public class EmpireResearchState
{
    public int EmpireId { get; set; }

    /// <summary>Highest unlocked tier per (color, category). Default 0 = none.</summary>
    private readonly Dictionary<string, int> _unlockedTiers = new();

    /// <summary>Subsystem IDs that are available for research (revealed but not yet researched).</summary>
    public HashSet<string> AvailableSubsystems { get; } = new();

    /// <summary>Subsystem IDs that are fully researched.</summary>
    public HashSet<string> ResearchedSubsystems { get; } = new();

    /// <summary>Subsystem IDs that are locked (require salvage/trade/Creative to unlock).</summary>
    public HashSet<string> LockedSubsystems { get; } = new();

    /// <summary>Synergy tech IDs that are available for research.</summary>
    public HashSet<string> AvailableSynergies { get; } = new();

    /// <summary>Synergy tech IDs that are researched.</summary>
    public HashSet<string> ResearchedSynergies { get; } = new();

    /// <summary>Current research project (subsystem or synergy ID). Null if idle.</summary>
    public string? CurrentProject { get; set; }

    /// <summary>Accumulated research points on current project.</summary>
    public float CurrentProgress { get; set; }

    /// <summary>Research queue (after current project).</summary>
    public List<string> Queue { get; } = new();

    /// <summary>Per-empire expertise tracker.</summary>
    public ExpertiseTracker Expertise { get; } = new();

    /// <summary>Whether this empire has the Creative trait.</summary>
    public bool IsCreative { get; set; }

    // === Tier queries ===

    private static string TierKey(PrecursorColor color, TechCategory category) =>
        $"{color}_{category}";

    public int GetUnlockedTier(PrecursorColor color, TechCategory category) =>
        _unlockedTiers.GetValueOrDefault(TierKey(color, category));

    /// <summary>
    /// Unlock the next tier in a category for a color. Reveals 3 subsystems:
    /// 2 randomly available + 1 locked. Creative empires get all 3 available.
    /// </summary>
    public void UnlockTier(PrecursorColor color, TechCategory category, int tier,
        TechNodeData node, GameRandom rng)
    {
        var key = TierKey(color, category);
        _unlockedTiers[key] = Math.Max(_unlockedTiers.GetValueOrDefault(key), tier);

        if (IsCreative)
        {
            // Creative: all 3 subsystems available
            foreach (var subId in node.SubsystemIds)
                AvailableSubsystems.Add(subId);
        }
        else
        {
            // Normal: 2 random available, 1 locked
            var indices = new List<int> { 0, 1, 2 };
            rng.Shuffle(indices);

            AvailableSubsystems.Add(node.SubsystemIds[indices[0]]);
            AvailableSubsystems.Add(node.SubsystemIds[indices[1]]);
            LockedSubsystems.Add(node.SubsystemIds[indices[2]]);
        }
    }

    /// <summary>Unlock a locked subsystem (via salvage, trade, or Creative).</summary>
    public bool UnlockSubsystem(string subsystemId)
    {
        if (!LockedSubsystems.Remove(subsystemId)) return false;
        AvailableSubsystems.Add(subsystemId);
        return true;
    }

    /// <summary>Mark a subsystem as fully researched.</summary>
    public bool CompleteSubsystem(string subsystemId)
    {
        if (!AvailableSubsystems.Remove(subsystemId)) return false;
        ResearchedSubsystems.Add(subsystemId);
        return true;
    }

    /// <summary>Check if a subsystem is researched.</summary>
    public bool HasSubsystem(string subsystemId) =>
        ResearchedSubsystems.Contains(subsystemId);

    /// <summary>Export unlocked tiers for serialization.</summary>
    public Dictionary<string, int> ExportUnlockedTiers() => new(_unlockedTiers);

    /// <summary>Import unlocked tiers from serialized data.</summary>
    public void ImportUnlockedTiers(Dictionary<string, int> tiers)
    {
        _unlockedTiers.Clear();
        foreach (var kvp in tiers)
            _unlockedTiers[kvp.Key] = kvp.Value;
    }
}
