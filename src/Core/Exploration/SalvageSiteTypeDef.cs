using System;
using System.Collections.Generic;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// Authored definition of a salvage site type, loaded from
/// <c>resources/data/salvage_types.json</c>. Pure C# — no Godot dependencies.
/// Consumed by <see cref="SalvageSiteGenerator"/> to roll concrete sites at
/// galaxy-gen time. Field names are JSON keys (case-insensitive on load).
/// </summary>
public class SalvageSiteTypeDef
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";

    /// <summary>Templates filled by the site generator. Tokens: {Adj}, {Name}, {Number}.</summary>
    public string[] NameTemplates { get; set; } = Array.Empty<string>();

    /// <summary>POIType-name → relative weight. Used to roll the type for a given POI.</summary>
    public Dictionary<string, float> EligiblePOIWeights { get; set; } = new();

    /// <summary>Base scan points required to reveal a single layer.</summary>
    public float BaseScanPerLayer { get; set; } = 100f;

    public int LayerCountMin { get; set; } = 1;
    public int LayerCountMax { get; set; } = 1;

    public float LayerYieldMin { get; set; } = 30f;
    public float LayerYieldMax { get; set; } = 80f;

    /// <summary>Probability that a yield pick rolls a Component (Basic/Advanced) vs ore/energy.</summary>
    public float ComponentBias { get; set; } = 0.3f;

    /// <summary>
    /// Per-layer chance to unlock research on scan completion. Index = layer depth (0-4).
    /// Trailing entries used for sites with fewer layers; absent entries default to last value.
    /// </summary>
    public float[] ResearchChancePerLayer { get; set; } = new[] { 0.10f, 0.15f, 0.20f, 0.25f, 0.30f };

    /// <summary>Pool of danger ids the generator picks from per-layer.</summary>
    public string[] DangerTypeIds { get; set; } = new[] { "damage" };

    /// <summary>Optional outcome unlocked once all layers are revealed.</summary>
    public string? SpecialOutcomeId { get; set; }
}
