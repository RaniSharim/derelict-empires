using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Settlements;

/// <summary>
/// Base class for Colony and Outpost. Holds owner, location, population groups,
/// and calculates aggregate output from pop allocations.
/// </summary>
public abstract class Settlement
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int OwnerEmpireId { get; set; }
    public int SystemId { get; set; }
    public int POIId { get; set; }

    public List<PopGroup> PopGroups { get; set; } = new();

    public int TotalPopulation => PopGroups.Sum(p => p.Count);

    public abstract int PopCap { get; }

    /// <summary>Get total pops assigned to a specific work pool.</summary>
    public int GetWorkersIn(WorkPool pool) =>
        PopGroups.Where(p => p.Allocation == pool).Sum(p => p.Count);
}
