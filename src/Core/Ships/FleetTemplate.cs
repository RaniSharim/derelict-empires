using System.Collections.Generic;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Ships;

/// <summary>
/// A named fleet composition template: (design, count, role) tuples plus per-role disposition defaults.
/// The unit of strategic thinking — players design at the fleet level, not the ship level.
/// </summary>
public class FleetTemplate
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>Ship-type entries that make up this template.</summary>
    public List<FleetTemplateEntry> Entries { get; set; } = new();

    /// <summary>Default disposition for each fleet role when fleets at this template engage combat.</summary>
    public Dictionary<FleetRole, Disposition> RoleDefaults { get; set; } = new();
}

/// <summary>
/// One ship-type slot in a fleet template: which design, how many, which role they play.
/// </summary>
public class FleetTemplateEntry
{
    public string DesignId { get; set; } = "";
    public int Count { get; set; } = 1;

    /// <summary>Role override if the ship's default role (from ShipDesign) is not what this template wants.</summary>
    public FleetRole? RoleOverride { get; set; }
}

/// <summary>Combat disposition for a fleet role — sets the behavior tree used in CombatSimulator.</summary>
public enum Disposition
{
    Charge,
    Hold,
    StandBack,
    Retreat
}
