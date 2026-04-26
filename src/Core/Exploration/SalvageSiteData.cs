using System.Collections.Generic;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// A salvage site placed on a POI. Layered model: scanning is per-layer;
/// scavenging drains the active layer's yield; researchable artifacts and
/// typed dangers are per-layer.
///
/// Backward-compat aggregates (<see cref="TotalYield"/>, <see cref="RemainingYield"/>,
/// <see cref="ScanDifficulty"/>, <see cref="Color"/>) are computed read-only views
/// over <see cref="Layers"/> so legacy UI panels keep rendering until they're
/// refreshed for the layered loop.
/// </summary>
public class SalvageSiteData
{
    public int Id { get; set; }
    public int POIId { get; set; } = -1;

    /// <summary>Authored type id from <c>resources/data/salvage_types.json</c>.</summary>
    public string TypeId { get; set; } = "";

    /// <summary>Display name composed at gen time ("Battle of Vega-3", etc.).</summary>
    public string Name { get; set; } = "";

    /// <summary>Site tier 1..5 (rolled uniformly at gen time, +1 per recursive multi-color hit).</summary>
    public int Tier { get; set; } = 1;

    /// <summary>One or more precursor colors. Index 0 is the primary.</summary>
    public List<PrecursorColor> Colors { get; set; } = new();

    /// <summary>Layered structure (1..5 entries).</summary>
    public List<SalvageLayer> Layers { get; set; } = new();

    /// <summary>
    /// Detection threshold — minimum scan strength a fleet needs to perceive this
    /// site at long range. Wired into the visibility system in a later ticket;
    /// stored here at gen time so the data is already correct.
    /// </summary>
    public float Visibility { get; set; }

    /// <summary>Optional fix-it outcome id (RepairStation / RecoverDerelict).</summary>
    public string? SpecialOutcomeId { get; set; }

    /// <summary>Per-layer extraction depletion curve. Constant for now.</summary>
    public float DepletionCurveExponent { get; set; } = 0.5f;

    // ── Convenience views (read-only) ────────────────────────────────

    /// <summary>Primary color — first entry in <see cref="Colors"/>.</summary>
    public PrecursorColor Color => Colors.Count > 0 ? Colors[0] : PrecursorColor.Red;

    /// <summary>Aggregate scan difficulty across all layers (legacy UI helper).</summary>
    public float ScanDifficulty
    {
        get
        {
            float t = 0f;
            foreach (var l in Layers) t += l.ScanDifficulty;
            return t;
        }
    }

    /// <summary>Aggregate authored yield across all layers (legacy UI helper).</summary>
    public Dictionary<string, float> TotalYield
    {
        get
        {
            var sum = new Dictionary<string, float>();
            foreach (var layer in Layers)
                foreach (var kv in layer.Yield)
                    sum[kv.Key] = sum.GetValueOrDefault(kv.Key) + kv.Value;
            return sum;
        }
    }

    /// <summary>Aggregate remaining yield across all layers (legacy UI helper).</summary>
    public Dictionary<string, float> RemainingYield
    {
        get
        {
            var sum = new Dictionary<string, float>();
            foreach (var layer in Layers)
                foreach (var kv in layer.RemainingYield)
                    sum[kv.Key] = sum.GetValueOrDefault(kv.Key) + kv.Value;
            return sum;
        }
    }
}
