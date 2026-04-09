namespace DerlictEmpires.Core.Systems;

/// <summary>Configuration parameters for galaxy generation.</summary>
public class GalaxyGenerationConfig
{
    public int Seed { get; set; } = 42;
    public int TotalSystems { get; set; } = 100;
    public int ArmCount { get; set; } = 4;
    public float GalaxyRadius { get; set; } = 200f;

    // Lane generation
    public float MaxLaneLength { get; set; } = 60f;
    public int MinNeighbors { get; set; } = 2;
    public int MaxNeighbors { get; set; } = 4;
    public float HiddenLaneRatio { get; set; } = 0.15f;
}
