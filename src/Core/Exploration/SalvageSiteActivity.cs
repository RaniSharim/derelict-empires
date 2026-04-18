using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// Per-empire activity state on a single salvage site POI.
/// Stored by SalvageSystem, keyed by (empireId, poiId).
/// ScanProgress is preserved across start/stop so re-starting a scan resumes
/// where it left off. ExtractionProgress is not tracked here — depletion lives
/// on SalvageSiteData.RemainingYield.
/// </summary>
public class SalvageSiteActivity
{
    public int EmpireId { get; set; }
    public int POIId { get; set; }
    public SiteActivity Activity { get; set; }
    public float ScanProgress { get; set; }
}
