namespace DerlictEmpires.Core.Enums;

/// <summary>Work pools that pops can be allocated to.</summary>
public enum WorkPool
{
    Production,
    Research,
    Food,
    Mining,
    Expert,
    /// <summary>Idle pop bucket — not assigned to any building. Source/sink for manual slot edits
    /// in System View. Priority auto-allocator may pull from here but never sends to here.</summary>
    Unassigned
}
