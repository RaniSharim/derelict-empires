using DerlictEmpires.Core.Production;

namespace DerlictEmpires.Core.Stations;

/// <summary>Tracks builder ship constructing a new station. Implements IProducible.</summary>
public class StationConstructionJob : IProducible
{
    public string Id => $"station_build_{StationId}";
    public string DisplayName => $"Build Station";
    public int ProductionCost { get; set; } = 200;

    public int StationId { get; set; }
    public int SystemId { get; set; }
    public int POIId { get; set; }
    public int BuilderShipId { get; set; }
}

/// <summary>Tracks installation of a module into an existing station.</summary>
public class ModuleInstallJob : IProducible
{
    public string Id => $"module_install_{ModuleType}";
    public string DisplayName { get; set; } = "Install Module";
    public int ProductionCost { get; set; } = 80;

    public int StationId { get; set; }
    public string ModuleType { get; set; } = "";

    /// <summary>The module to install when complete.</summary>
    public StationModule? Module { get; set; }
}
