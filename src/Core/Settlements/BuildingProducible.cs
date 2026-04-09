using DerlictEmpires.Core.Production;

namespace DerlictEmpires.Core.Settlements;

/// <summary>Adapts BuildingData to IProducible for the production queue.</summary>
public class BuildingProducible : IProducible
{
    private readonly BuildingData _data;

    public BuildingProducible(BuildingData data) => _data = data;

    public string Id => _data.Id;
    public string DisplayName => _data.DisplayName;
    public int ProductionCost => _data.ProductionCost;
    public BuildingData Data => _data;
}
