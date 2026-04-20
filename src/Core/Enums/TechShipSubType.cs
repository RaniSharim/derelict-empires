namespace DerlictEmpires.Core.Enums;

/// <summary>
/// Second-level classification for tech modules where <see cref="TechModuleType"/> is Ship.
/// Drives hull-slot compatibility and picker grouping — a shield goes in a defense slot,
/// a reactor in a power slot, etc. Each TechModuleType has its own sub-type enum; this
/// one is ship-specific.
/// </summary>
public enum TechShipSubType
{
    Shield,
    Armor,
    ECM,
    Rail,
    Laser,
    Engine,
    Reactor,
    Support,
}
