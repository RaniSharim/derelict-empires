using System;
using Godot;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Autoloads;

/// <summary>
/// Global event bus for cross-tree communication.
/// Uses C# events (Action delegates) for compile-time safety.
/// Registered as an autoload — access via EventBus.Instance.
/// </summary>
public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; } = null!;

    public override void _Ready()
    {
        Instance = this;
        GD.Print("[EventBus] Ready");
    }

    // === Galaxy & Map ===
    public event Action<StarSystemData>? SystemSelected;
    public event Action? SystemDeselected;
    public event Action<StarSystemData>? SystemHovered;
    public event Action? SystemUnhovered;

    // === Game Loop ===
    public event Action<float>? FastTick;   // Arg: tick delta in game-seconds
    public event Action<float>? SlowTick;   // Arg: tick delta in game-seconds
    public event Action<GameSpeed>? SpeedChanged;
    public event Action? GamePaused;
    public event Action? GameResumed;

    // === Fleet ===
    public event Action<int>? FleetSelected;      // fleet ID
    public event Action? FleetDeselected;
    public event Action<int, int>? FleetArrivedAtSystem; // fleet ID, system ID

    // === Empire ===
    public event Action<int, PrecursorColor, ResourceType, float>? ResourceChanged; // empire, color, type, newAmt

    // === Research ===
    public event Action<int, string>? SubsystemResearched;   // empireId, subsystemId
    public event Action<int, string>? ResearchStarted;       // empireId, projectId
    public event Action<int, PrecursorColor, TechCategory, int>? TierUnlocked; // empireId, color, category, tier

    // === Stations ===
    public event Action<int, int>? StationModuleInstalled;   // stationId, empireId
    public event Action<int, string>? ShipProduced;          // empireId, shipName

    // Fire methods — centralizes null-check pattern
    public void FireSystemSelected(StarSystemData system) => SystemSelected?.Invoke(system);
    public void FireSystemDeselected() => SystemDeselected?.Invoke();
    public void FireSystemHovered(StarSystemData system) => SystemHovered?.Invoke(system);
    public void FireSystemUnhovered() => SystemUnhovered?.Invoke();

    public void FireFastTick(float delta) => FastTick?.Invoke(delta);
    public void FireSlowTick(float delta) => SlowTick?.Invoke(delta);
    public void FireSpeedChanged(GameSpeed speed) => SpeedChanged?.Invoke(speed);
    public void FireGamePaused() => GamePaused?.Invoke();
    public void FireGameResumed() => GameResumed?.Invoke();

    public void FireFleetSelected(int fleetId) => FleetSelected?.Invoke(fleetId);
    public void FireFleetDeselected() => FleetDeselected?.Invoke();
    public void FireFleetArrivedAtSystem(int fleetId, int systemId) => FleetArrivedAtSystem?.Invoke(fleetId, systemId);

    public void FireResourceChanged(int empireId, PrecursorColor color, ResourceType type, float newAmount) =>
        ResourceChanged?.Invoke(empireId, color, type, newAmount);

    public void FireSubsystemResearched(int empireId, string subsystemId) =>
        SubsystemResearched?.Invoke(empireId, subsystemId);
    public void FireResearchStarted(int empireId, string projectId) =>
        ResearchStarted?.Invoke(empireId, projectId);
    public void FireTierUnlocked(int empireId, PrecursorColor color, TechCategory category, int tier) =>
        TierUnlocked?.Invoke(empireId, color, category, tier);
    public void FireStationModuleInstalled(int stationId, int empireId) =>
        StationModuleInstalled?.Invoke(stationId, empireId);
    public void FireShipProduced(int empireId, string shipName) =>
        ShipProduced?.Invoke(empireId, shipName);
}
