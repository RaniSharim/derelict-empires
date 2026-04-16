namespace DerlictEmpires.Core.Enums;

/// <summary>
/// The six resource types per precursor color (30 total across 5 colors).
/// Three layers: Ore (mined), Energy (refined/generated), Components (manufactured/salvaged).
/// </summary>
public enum ResourceType
{
    SimpleOre,
    AdvancedOre,
    SimpleEnergy,
    AdvancedEnergy,
    BasicComponent,
    AdvancedComponent
}
