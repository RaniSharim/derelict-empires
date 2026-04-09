using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Models;

/// <summary>
/// A single ship instance at runtime. References a design template.
/// </summary>
public class ShipInstanceData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int OwnerEmpireId { get; set; }
    public ShipSizeClass SizeClass { get; set; }

    /// <summary>Functional role hint (e.g., "Scout", "Fighter", "Salvager", "Builder").</summary>
    public string Role { get; set; } = "";

    public int MaxHp { get; set; } = 100;
    public int CurrentHp { get; set; } = 100;

    /// <summary>Fleet this ship belongs to (-1 if unassigned).</summary>
    public int FleetId { get; set; } = -1;
}
