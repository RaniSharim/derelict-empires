using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Exploration;

/// <summary>Data for a salvage site at a POI.</summary>
public class SalvageSiteData
{
    public int Id { get; set; }
    public SalvageSiteType Type { get; set; }
    public PrecursorColor Color { get; set; }
    public int TechTier { get; set; }
    public float TotalYield { get; set; }
    public float RemainingYield { get; set; }
    public float HazardLevel { get; set; }
    public int ExcavationLayers { get; set; } = 1;
}
