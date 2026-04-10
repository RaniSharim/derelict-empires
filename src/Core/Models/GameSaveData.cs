using System.Collections.Generic;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Models;

/// <summary>
/// Complete serializable game state. JSON for dev, binary later.
/// All data needed to fully restore a game in progress.
/// </summary>
public class GameSaveData
{
    /// <summary>Save format version for forward compatibility.</summary>
    public int Version { get; set; } = 1;

    // === Core state ===
    public int MasterSeed { get; set; }
    public double GameTime { get; set; }
    public GameSpeed GameSpeed { get; set; } = GameSpeed.Paused;

    // === Galaxy ===
    public GalaxyData Galaxy { get; set; } = new();

    // === Empires ===
    public List<EmpireData> Empires { get; set; } = new();

    // === Fleets & Ships ===
    public List<FleetData> Fleets { get; set; } = new();
    public List<ShipInstanceData> Ships { get; set; } = new();
    public List<FleetOrderSaveData> FleetOrders { get; set; } = new();

    // === Settlements ===
    public List<ColonyData> Colonies { get; set; } = new();
    public List<StationData> Stations { get; set; } = new();

    // === Economy ===
    public List<ExtractionAssignment> Extractions { get; set; } = new();
}

/// <summary>
/// Serializable fleet order — pairs a fleet ID with its current order.
/// </summary>
public class FleetOrderSaveData
{
    public int FleetId { get; set; }
    public FleetOrderType Type { get; set; }
    public List<int> Path { get; set; } = new();
    public int PathIndex { get; set; }
    public float LaneProgress { get; set; }
    public int TransitFromSystemId { get; set; } = -1;
}
