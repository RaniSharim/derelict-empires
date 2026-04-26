using System.Collections.Generic;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// One uncoverable layer of a salvage site. Sites have 1-5 layers; the player
/// scans them sequentially, after each scan choosing to scavenge (drain Yield)
/// or skip. Each layer carries a research-unlock chance and a typed danger.
/// Pure data — runtime state lives on <see cref="SalvageSiteProgress"/>.
/// </summary>
public class SalvageLayer
{
    public int Index { get; set; }

    /// <summary>Color this layer's research roll targets when it lands.</summary>
    public PrecursorColor LayerColor { get; set; }

    /// <summary>Tier of the research target (matches the site's Tier).</summary>
    public int ResearchTargetTier { get; set; }

    /// <summary>Probability [0,1] that scanning this layer unlocks a research subsystem.</summary>
    public float ResearchUnlockChance { get; set; }

    /// <summary>Total yield to drain on scavenge, keyed by EmpireData.ResourceKey.</summary>
    public Dictionary<string, float> Yield { get; set; } = new();

    /// <summary>Remaining yield, depleted by scavenge ticks. Initialized = Yield.</summary>
    public Dictionary<string, float> RemainingYield { get; set; } = new();

    /// <summary>Scan points required to fully reveal this layer.</summary>
    public float ScanDifficulty { get; set; }

    /// <summary>Danger id (looked up in <c>SalvageRegistry.Dangers</c>).</summary>
    public string DangerTypeId { get; set; } = "damage";

    /// <summary>Probability [0,1] the danger triggers when scavenge starts. = Tier × 0.10 by default.</summary>
    public float DangerChance { get; set; }

    /// <summary>Resolved severity (BaseSeverity + Tier × PerTierBonus from the danger def).</summary>
    public float DangerSeverity { get; set; }
}
