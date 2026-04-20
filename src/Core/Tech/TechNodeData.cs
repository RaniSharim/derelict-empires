using System.Collections.Generic;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Tech;

/// <summary>
/// A tech tree node: one tier in one category for one color.
/// Contains 3 subsystem IDs. 150 total nodes (5 colors × 5 categories × 6 tiers).
/// </summary>
public class TechNodeData
{
    public string Id { get; set; } = "";
    public PrecursorColor Color { get; set; }
    public TechCategory Category { get; set; }
    public int Tier { get; set; } // 1-6
    public int ResearchCost { get; set; }

    /// <summary>Three subsystem IDs revealed when this tier is unlocked.</summary>
    public List<string> SubsystemIds { get; set; } = new();
}

/// <summary>
/// A specific subsystem unlocked from a tech node.
/// Represents a concrete capability (weapon type, building, ship module, etc.).
/// </summary>
public class SubsystemData
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public PrecursorColor Color { get; set; }
    public TechCategory Category { get; set; }
    public int Tier { get; set; }
    public int ResearchCost { get; set; }

    /// <summary>Where this module slots in — ship hull, station, colony structure, or empire-wide effect.</summary>
    public TechModuleType Type { get; set; } = TechModuleType.Global;

    /// <summary>Sub-type within Ship modules (shield / armor / ecm / rail / laser / engine / reactor / support).
    /// Null for non-Ship modules. Station / Structure / Global will grow their own sub-type enums later.</summary>
    public TechShipSubType? ShipSubType { get; set; }
}

/// <summary>Synergy tech unlocked when two colors reach required tier levels.</summary>
public class SynergyTechData
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public PrecursorColor ColorA { get; set; }
    public PrecursorColor ColorB { get; set; }
    public int RequiredTierA { get; set; }
    public int RequiredTierB { get; set; }
    public int ResearchCost { get; set; }
}
