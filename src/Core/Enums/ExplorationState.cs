namespace DerlictEmpires.Core.Enums;

public enum ExplorationState
{
    Undiscovered,
    Discovered,
    Surveyed
}

public enum DerelictAction
{
    SalvageForParts,
    UseAsIs,
    JuryRig,
    Repair,
    Replicate
}

public enum HazardType
{
    None,
    AutomatedDefense,
    Trap,
    Contamination,
    GuardianActivation,
    StructuralCollapse
}
