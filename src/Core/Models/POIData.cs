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

    /// <summary>If this POI hosts a salvage site, its id in GalaxyData.SalvageSites. Null otherwise.</summary>
    public int? SalvageSiteId { get; set; }

    /// <summary>
    /// System View band assignment, derived from <see cref="Type"/>. Habitable/barren planets →
    /// Inner, belts/debris → Mid, abandoned/graveyard/megastructure → Outer. See
    /// design/in_system_design.md §3.
    /// </summary>
    public Band Band => BandOf(Type);

    public static Band BandOf(POIType type) => type switch
    {
        POIType.HabitablePlanet  => Band.Inner,
        POIType.BarrenPlanet     => Band.Inner,
        POIType.AsteroidField    => Band.Mid,
        POIType.DebrisField      => Band.Mid,
        POIType.AbandonedStation => Band.Outer,
        POIType.ShipGraveyard    => Band.Outer,
        POIType.Megastructure    => Band.Outer,
        _                        => Band.Mid,
    };
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
