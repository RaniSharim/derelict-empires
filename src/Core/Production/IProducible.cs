namespace DerlictEmpires.Core.Production;

/// <summary>
/// Interface for anything that can be built via a production queue
/// (buildings, ships, station modules, etc.).
/// </summary>
public interface IProducible
{
    string Id { get; }
    string DisplayName { get; }
    int ProductionCost { get; }
}
