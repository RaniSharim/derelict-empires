using System.Collections.Generic;

namespace DerlictEmpires.Core.Models;

/// <summary>
/// Runtime data for a fleet (group of ships) at the galaxy map level.
/// </summary>
public class FleetData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int OwnerEmpireId { get; set; }

    /// <summary>Current system location (-1 if in transit).</summary>
    public int CurrentSystemId { get; set; } = -1;

    /// <summary>Ship instance IDs in this fleet.</summary>
    public List<int> ShipIds { get; set; } = new();

    /// <summary>Movement speed (units per game-second along lanes).</summary>
    public float Speed { get; set; } = 10f;
}
