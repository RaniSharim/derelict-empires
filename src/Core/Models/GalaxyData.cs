using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Exploration;

namespace DerlictEmpires.Core.Models;

/// <summary>
/// The complete galaxy state produced by the generator.
/// Contains all star systems, lanes, and metadata.
/// </summary>
public class GalaxyData
{
    public int Seed { get; set; }
    public List<StarSystemData> Systems { get; set; } = new();
    public List<LaneData> Lanes { get; set; } = new();
    public List<SalvageSiteData> SalvageSites { get; set; } = new();

    /// <summary>Number of spiral arms.</summary>
    public int ArmCount { get; set; }

    /// <summary>Find a salvage site by id. Returns null if not found.</summary>
    public SalvageSiteData? GetSalvageSite(int id) =>
        id >= 0 && id < SalvageSites.Count ? SalvageSites[id] : null;

    /// <summary>Find a system by ID.</summary>
    public StarSystemData? GetSystem(int id) =>
        id >= 0 && id < Systems.Count ? Systems[id] : null;

    /// <summary>Get all lanes connected to a system.</summary>
    public IEnumerable<LaneData> GetLanesForSystem(int systemId) =>
        Lanes.Where(l => l.Connects(systemId));

    /// <summary>Get IDs of systems directly connected to this one.</summary>
    public IEnumerable<int> GetNeighbors(int systemId) =>
        GetLanesForSystem(systemId).Select(l => l.GetOtherSystem(systemId));
}
