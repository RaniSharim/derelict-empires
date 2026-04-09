using System.Collections.Generic;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Models;

/// <summary>A point of interest within a star system.</summary>
public class POIData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public POIType Type { get; set; }
    public PrecursorColor? DominantColor { get; set; }
    public TerrainModifier Terrain { get; set; } = TerrainModifier.None;

    /// <summary>Planet size category, only relevant for HabitablePlanet/BarrenPlanet.</summary>
    public PlanetSize PlanetSize { get; set; } = PlanetSize.None;

    /// <summary>Resource deposits available at this POI. Populated during galaxy gen.</summary>
    public List<ResourceDeposit> Deposits { get; set; } = new();
}

public enum PlanetSize
{
    None,        // Not a planet
    Small,       // 5-7 pop cap
    Medium,      // 7-10 pop cap
    Large,       // 12-15 pop cap
    Prime,       // 15-25 pop cap
    Exceptional  // Up to 30 pop cap (rare)
}

/// <summary>A resource deposit at a POI that can be extracted.</summary>
public class ResourceDeposit
{
    public PrecursorColor Color { get; set; }
    public ResourceType Type { get; set; }
    public float TotalAmount { get; set; }
    public float RemainingAmount { get; set; }
    public float BaseExtractionRate { get; set; }
}
