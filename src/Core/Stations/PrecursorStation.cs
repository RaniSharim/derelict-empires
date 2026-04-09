using System.Collections.Generic;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Stations;

/// <summary>
/// A precursor station found at a POI. Can be claimed, repaired, or scavenged.
/// Has pre-filled modules, a hazard level, and unique tech potential.
/// </summary>
public class PrecursorStation : Station
{
    public enum ClaimState { Unclaimed, Claimed, Repaired, Scavenged }

    public ClaimState State { get; set; } = ClaimState.Unclaimed;

    /// <summary>Precursor color alignment.</summary>
    public PrecursorColor Color { get; set; }

    /// <summary>Tech tier of the precursor tech (1-6).</summary>
    public int TechTier { get; set; } = 2;

    /// <summary>Hazard level 0-1. Higher = more dangerous to interact with.</summary>
    public float HazardLevel { get; set; } = 0.3f;

    /// <summary>Production cost to repair this station to full functionality.</summary>
    public int RepairCost { get; set; } = 300;

    /// <summary>Components yielded if scavenged instead of repaired.</summary>
    public int ScavengeYieldBasic { get; set; } = 20;
    public int ScavengeYieldAdvanced { get; set; } = 5;

    /// <summary>Whether a hazard check has been performed for this station.</summary>
    public bool HazardChecked { get; set; }

    /// <summary>
    /// Claim the station for an empire. Sets owner but station is not yet functional.
    /// Modules are visible but not active until repaired.
    /// </summary>
    public bool Claim(int empireId)
    {
        if (State != ClaimState.Unclaimed) return false;
        OwnerEmpireId = empireId;
        State = ClaimState.Claimed;
        IsConstructed = false;
        return true;
    }

    /// <summary>
    /// Complete repair. Station becomes fully functional with all precursor modules.
    /// </summary>
    public bool CompleteRepair()
    {
        if (State != ClaimState.Claimed) return false;
        State = ClaimState.Repaired;
        IsConstructed = true;
        return true;
    }

    /// <summary>
    /// Scavenge the station for components. Destroys the station but yields materials.
    /// Returns (basicComponents, advancedComponents).
    /// </summary>
    public (int basic, int advanced) Scavenge()
    {
        if (State == ClaimState.Scavenged) return (0, 0);
        State = ClaimState.Scavenged;
        IsConstructed = false;
        Modules.Clear();
        return (ScavengeYieldBasic, ScavengeYieldAdvanced);
    }
}
