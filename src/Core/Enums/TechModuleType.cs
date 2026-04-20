namespace DerlictEmpires.Core.Enums;

/// <summary>
/// What kind of thing a researched tech subsystem fits into.
/// Drives filtering in the ship designer, station editor, colony build queue, etc.
/// </summary>
public enum TechModuleType
{
    Ship,
    Station,
    Structure,
    Global,
}
