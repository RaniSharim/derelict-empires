namespace DerlictEmpires.Core.Enums;

/// <summary>
/// How an empire came to have a given tech subsystem available for use.
/// Drives source badges in pickers and decides whether a grant persists if the
/// underlying agreement lapses (e.g. diplomacy-rented modules revert; researched ones stay).
/// </summary>
public enum TechAvailabilitySource
{
    Research,
    Diplomacy,
}
