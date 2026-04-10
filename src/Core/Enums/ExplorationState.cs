namespace DerlictEmpires.Core.Enums;

public enum ExplorationState
{
    Undiscovered,
    Discovered,
    Surveyed
}

public enum SalvageSiteType
{
    MinorDerelict,
    MajorPrecursorSite,
    PrecursorIntersection,
    ShipGraveyard,
    FailedSalvagerWreck,
    DesperationProject,
    DebrisField
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
