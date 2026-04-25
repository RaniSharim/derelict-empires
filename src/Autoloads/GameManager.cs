using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Autoloads;

/// <summary>
/// Global game state container. Does not own logic — systems read from it.
/// Registered as an autoload — access via GameManager.Instance.
/// </summary>
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; } = null!;

    public override void _Ready()
    {
        Instance = this;
        GD.Print("[GameManager] Ready");
    }

    // === Game state ===
    public Core.Enums.GameState CurrentState { get; set; } = Core.Enums.GameState.MainMenu;

    private GameSpeed _currentSpeed = GameSpeed.Normal;
    public GameSpeed CurrentSpeed
    {
        get => _currentSpeed;
        set
        {
            if (_currentSpeed == value) return;
            var old = _currentSpeed;
            _currentSpeed = value;

            if (value == GameSpeed.Paused && old != GameSpeed.Paused)
                EventBus.Instance?.FireGamePaused();
            else if (value != GameSpeed.Paused && old == GameSpeed.Paused)
                EventBus.Instance?.FireGameResumed();

            EventBus.Instance?.FireSpeedChanged(value);
        }
    }

    // === Galaxy ===
    public GalaxyData? Galaxy { get; set; }

    // === Game data (collections owned here; consumers read directly) ===
    public List<EmpireData> Empires { get; private set; } = new();
    public List<FleetData> Fleets { get; private set; } = new();
    public List<ShipInstanceData> Ships { get; private set; } = new();
    public List<ColonyData> Colonies { get; private set; } = new();
    public List<StationData> StationDatas { get; private set; } = new();

    public Dictionary<int, EmpireData> EmpiresById { get; private set; } = new();
    public Dictionary<int, ShipInstanceData> ShipsById { get; private set; } = new();

    public EmpireData? LocalPlayerEmpire => Empires.Find(e => e.IsHuman);

    /// <summary>Bulk-load game state. Used by new-game setup and save-game load paths.
    /// Replaces all collections and rebuilds index dictionaries.</summary>
    public void LoadState(
        List<EmpireData> empires,
        List<FleetData> fleets,
        List<ShipInstanceData> ships,
        List<ColonyData> colonies,
        List<StationData> stationDatas)
    {
        Empires = empires;
        Fleets = fleets;
        Ships = ships;
        Colonies = colonies;
        StationDatas = stationDatas;
        EmpiresById = empires.ToDictionary(e => e.Id);
        ShipsById = ships.ToDictionary(s => s.Id);
    }

    /// <summary>Add a new empire and update the id index.</summary>
    public void RegisterEmpire(EmpireData empire)
    {
        Empires.Add(empire);
        EmpiresById[empire.Id] = empire;
    }

    /// <summary>Add a new fleet and its ships to the live collections, updating id indexes.
    /// Visualization is the caller's responsibility (MainScene spawns FleetNode).</summary>
    public void AddFleetData(FleetData fleet, IEnumerable<ShipInstanceData> ships)
    {
        foreach (var ship in ships)
        {
            Ships.Add(ship);
            ShipsById[ship.Id] = ship;
        }
        Fleets.Add(fleet);
    }

    // === Time ===
    /// <summary>Total elapsed game-seconds since game start.</summary>
    public double GameTime { get; set; }

    // === Seed ===
    /// <summary>Master seed for all game randomization. Set at game creation.</summary>
    public int MasterSeed { get; set; }
}
