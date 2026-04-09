using System.Collections.Generic;

namespace DerlictEmpires.Core.Models;

/// <summary>
/// Runtime data for a space station at a POI.
/// Stations have modular slots for shipyard, defense, logistics, etc.
/// </summary>
public class StationData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int OwnerEmpireId { get; set; }
    public int SystemId { get; set; }
    public int POIId { get; set; }

    /// <summary>Station size tier (1-5). Determines available module slots.</summary>
    public int SizeTier { get; set; } = 1;

    /// <summary>Module slots available at this size tier.</summary>
    public int MaxModules => SizeTier + 1;

    /// <summary>Installed module type names (placeholder until full module system).</summary>
    public List<string> InstalledModules { get; set; } = new();

    /// <summary>Whether this station has a shipyard module.</summary>
    public bool HasShipyard => InstalledModules.Contains("Shipyard");
}
