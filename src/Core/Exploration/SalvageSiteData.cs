using System.Collections.Generic;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Exploration;

/// <summary>Data for a salvage site at a POI.</summary>
public class SalvageSiteData
{
    public int Id { get; set; }
    public int POIId { get; set; } = -1;
    public SalvageSiteType Type { get; set; }
    public PrecursorColor Color { get; set; }
    public int TechTier { get; set; }

    /// <summary>Total scan points required to fully reveal the site.</summary>
    public float ScanDifficulty { get; set; }

    /// <summary>
    /// Total yield per resource, keyed by EmpireData.ResourceKey(color, type).
    /// Frozen after generation.
    /// </summary>
    public Dictionary<string, float> TotalYield { get; set; } = new();

    /// <summary>Remaining yield per resource, depleted as extract orders run.</summary>
    public Dictionary<string, float> RemainingYield { get; set; } = new();

    /// <summary>Exponent on remaining-fraction in the extraction formula. 0.5 = easy then hard.</summary>
    public float DepletionCurveExponent { get; set; } = 0.5f;

    public float HazardLevel { get; set; }
    public int ExcavationLayers { get; set; } = 1;
}
