using System.Collections.Generic;

namespace DerlictEmpires.Core.Models;

/// <summary>
/// A command issued to a fleet. Currently only MoveTo; future phases add Patrol, Explore, etc.
/// </summary>
public class FleetOrder
{
    public FleetOrderType Type { get; set; }

    /// <summary>Path of system IDs to traverse (excluding current system).</summary>
    public List<int> Path { get; set; } = new();

    /// <summary>Index of the next system in Path the fleet is heading toward.</summary>
    public int PathIndex { get; set; }

    /// <summary>Progress along the current lane segment [0, 1].</summary>
    public float LaneProgress { get; set; }

    /// <summary>The system being transited from.</summary>
    public int TransitFromSystemId { get; set; } = -1;

    public bool IsComplete => PathIndex >= Path.Count;
    public int NextSystemId => PathIndex < Path.Count ? Path[PathIndex] : -1;
}

public enum FleetOrderType
{
    MoveTo,
    Patrol,
    Explore
}
