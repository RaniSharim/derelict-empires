using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Models;

/// <summary>
/// Links an extractor (colony outpost or ship) to a resource deposit at a POI.
/// </summary>
public class ExtractionAssignment
{
    public int Id { get; set; }
    public int OwnerEmpireId { get; set; }
    public int SystemId { get; set; }
    public int POIId { get; set; }

    /// <summary>Index into POIData.Deposits.</summary>
    public int DepositIndex { get; set; }

    /// <summary>Multiplier from technology, workers, etc. Default 1.0.</summary>
    public float EfficiencyMultiplier { get; set; } = 1.0f;

    /// <summary>Number of workers/ships assigned. More = faster extraction.</summary>
    public int WorkerCount { get; set; } = 1;
}
