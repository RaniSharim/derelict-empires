using System.Collections.Generic;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Models;

/// <summary>Runtime data for a star system in the galaxy.</summary>
public class StarSystemData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    /// <summary>World position (X, Z plane; Y is up).</summary>
    public float PositionX { get; set; }
    public float PositionZ { get; set; }

    /// <summary>Which spiral arm this system belongs to (-1 for core).</summary>
    public int ArmIndex { get; set; } = -1;

    /// <summary>The dominant precursor color of this system's region.</summary>
    public PrecursorColor? DominantColor { get; set; }

    /// <summary>Whether this system is in the core region vs a spiral arm.</summary>
    public bool IsCore { get; set; }

    /// <summary>Normalized distance from galaxy center [0=center, 1=rim].</summary>
    public float RadialPosition { get; set; }

    /// <summary>Points of interest in this system (3-5 typically).</summary>
    public List<POIData> POIs { get; set; } = new();

    /// <summary>IDs of lanes connected to this system.</summary>
    public List<int> ConnectedLaneIndices { get; set; } = new();
}
