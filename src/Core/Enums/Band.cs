namespace DerlictEmpires.Core.Enums;

/// <summary>
/// Orbital band classification within a star system. Every POI is assigned to exactly
/// one band at generation and does not migrate. See design/in_system_design.md §3.
/// </summary>
public enum Band
{
    /// <summary>Habitable planets, hostile planets, near-star bodies. Clear sensor bias.</summary>
    Inner,
    /// <summary>Asteroid belts, debris fields, minor anomalies. Partial coverage.</summary>
    Mid,
    /// <summary>Graveyards, abandoned stations, megastructures, drifting fleets. Dark.</summary>
    Outer
}
