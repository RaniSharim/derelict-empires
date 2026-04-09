using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Settlements;

/// <summary>
/// A group of pops of the same species allocated to a work pool.
/// Population is tracked as integer counts, not individual agents.
/// </summary>
public class PopGroup
{
    public string SpeciesId { get; set; } = "default";
    public int Count { get; set; }
    public WorkPool Allocation { get; set; }
}
