using System.Collections.Generic;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// Per-empire per-site runtime state for the layered salvage loop. Replaces
/// the old <c>SalvageSiteActivity</c>. Stored by <see cref="SalvageSystem"/>,
/// keyed by <c>(empireId, poiId)</c>.
///
/// Lifecycle:
///   - Created on first <c>RequestScan</c> for the site.
///   - <c>Activity</c> is the SITE-level activity at any given moment
///     (Scanning, Scavenging, or None).
///   - <c>ActiveLayerIndex</c> advances when the current layer is scavenged
///     or skipped, never on bare scan completion.
/// </summary>
public class SalvageSiteProgress
{
    public int EmpireId { get; set; }
    public int POIId { get; set; }

    public DerlictEmpires.Core.Enums.SiteActivity Activity { get; set; }
        = DerlictEmpires.Core.Enums.SiteActivity.None;

    /// <summary>Current layer index. Equals <see cref="LayerCount"/> when all are terminal.</summary>
    public int ActiveLayerIndex { get; set; }

    public int LayerCount { get; set; }

    public float[] LayerScanProgress { get; set; } = System.Array.Empty<float>();
    public bool[]  LayerScanned     { get; set; } = System.Array.Empty<bool>();
    public bool[]  LayerScavenged   { get; set; } = System.Array.Empty<bool>();
    public bool[]  LayerSkipped     { get; set; } = System.Array.Empty<bool>();
    public bool[]  ResearchUnlocked { get; set; } = System.Array.Empty<bool>();
    public string?[] ResearchSubsystemId { get; set; } = System.Array.Empty<string?>();
    public bool[]  DangerTriggered  { get; set; } = System.Array.Empty<bool>();

    public bool SpecialOutcomeAvailable { get; set; }
    public bool SpecialOutcomeConsumed  { get; set; }

    public static SalvageSiteProgress ForSite(int empireId, int poiId, int layerCount) =>
        new()
        {
            EmpireId = empireId,
            POIId = poiId,
            LayerCount = layerCount,
            LayerScanProgress = new float[layerCount],
            LayerScanned     = new bool[layerCount],
            LayerScavenged   = new bool[layerCount],
            LayerSkipped     = new bool[layerCount],
            ResearchUnlocked = new bool[layerCount],
            ResearchSubsystemId = new string?[layerCount],
            DangerTriggered  = new bool[layerCount],
        };

    public bool LayerTerminal(int i) =>
        i >= 0 && i < LayerCount && (LayerScavenged[i] || LayerSkipped[i]);

    public bool AllLayersTerminal()
    {
        for (int i = 0; i < LayerCount; i++)
            if (!LayerTerminal(i)) return false;
        return true;
    }
}
