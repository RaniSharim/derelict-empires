using System;

namespace DerlictEmpires.Core.Exploration;

/// <summary>An active excavation at a salvage site by an empire.</summary>
public class ExcavationJob
{
    public int EmpireId { get; set; }
    public int SiteId { get; set; }
    public float Progress { get; set; }
    public float TotalWork { get; set; }
    public float SalvageCapacity { get; set; } = 1f;

    public bool IsComplete => Progress >= TotalWork;
    public float ProgressPercent => TotalWork > 0 ? MathF.Min(Progress / TotalWork, 1f) : 1f;
}
