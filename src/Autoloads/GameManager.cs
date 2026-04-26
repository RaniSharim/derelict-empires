using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Core;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Services;
using DerlictEmpires.Core.Settlements;
using DerlictEmpires.Core.Stations;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Autoloads;

/// <summary>
/// Global game state container plus <see cref="IGameQuery"/> facade. Does not own logic —
/// systems read from it; UI panels read through it. Registered as an autoload — access via
/// <c>GameManager.Instance</c>.
/// </summary>
public partial class GameManager : Node, IGameQuery
{
    public static GameManager Instance { get; private set; } = null!;

    public override void _Ready()
    {
        Instance = this;
        McpLog.Info("[GameManager] Ready");
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

    // === IGameQuery facade ===
    // GameSystemsHost wires this on _Ready. UI reads through GameManager so test panels
    // can swap a fake IGameQuery without dragging GameSystems into their constructor.
    //
    // The forwarding methods below (GetFleetContributions, GetSiteActivity, etc.) delegate
    // to _systems on purpose — derived runtime state (active salvage activities, current
    // fleet orders, exploration progress) lives on the systems that mutate it, not on the
    // POCO models. Mirroring it onto FleetData/EmpireData would duplicate it into
    // GameSaveData JSON and guarantee drift between the save and the runtime map.
    // Dependency graph stays one-way: Models ← Systems ← GameManager (this facade) ← UI.

    private GameSystems? _systems;
    public void SetGameSystems(GameSystems systems) => _systems = systems;

    EmpireData? IGameQuery.PlayerEmpire => LocalPlayerEmpire;
    EmpireResearchState? IGameQuery.PlayerResearchState
    {
        get
        {
            var pid = LocalPlayerEmpire?.Id ?? -1;
            return pid >= 0 ? _systems?.GetResearchState(pid) : null;
        }
    }
    IReadOnlyList<FleetData> IGameQuery.Fleets => Fleets;
    IReadOnlyList<EmpireData> IGameQuery.Empires => Empires;
    IReadOnlyDictionary<int, ShipInstanceData> IGameQuery.ShipsById => ShipsById;
    GalaxyData? IGameQuery.Galaxy => Galaxy;

    public float GetSystemCapability(int poiId, SiteActivity type)
    {
        var player = LocalPlayerEmpire;
        if (player == null || _systems == null) return 0f;
        return _systems.GetSystemCapability(Galaxy, player.Id, poiId, type, Fleets, ShipsById);
    }

    public int GetSystemActiveCount(int poiId, SiteActivity type)
    {
        var player = LocalPlayerEmpire;
        if (player == null || _systems == null) return 0;
        return _systems.GetSystemActiveCount(Galaxy, player.Id, poiId, type);
    }

    public SalvageSiteData? GetSalvageSite(int siteId) => Galaxy?.GetSalvageSite(siteId);

    public POIData? FindPOI(int poiId, out int systemId)
    {
        systemId = -1;
        if (_systems == null) return null;
        return _systems.FindPOI(Galaxy, poiId, out systemId);
    }

    public SiteActivity GetSiteActivity(int empireId, int poiId) =>
        _systems?.Salvage?.GetActivity(empireId, poiId) ?? SiteActivity.None;

    public FleetOrder? GetFleetOrder(int fleetId) => _systems?.Movement?.GetOrder(fleetId);

    public ExplorationState GetExplorationState(int empireId, int poiId) =>
        _systems?.Exploration?.GetState(empireId, poiId) ?? ExplorationState.Undiscovered;

    public float GetScanProgress(int empireId, int poiId) =>
        _systems?.Exploration?.GetScanProgress(empireId, poiId) ?? 0f;

    private static readonly List<FleetData> _emptyFleets = new();
    public IReadOnlyList<FleetData> GetContributingFleets(int empireId, int poiId) =>
        _systems?.Salvage?.GetContributingFleets(empireId, poiId, Fleets, ShipsById) ?? _emptyFleets;

    private static readonly List<int> _emptyInts = new();
    public (IReadOnlyList<int> scanning, IReadOnlyList<int> extracting) GetFleetContributions(int fleetId)
    {
        var fleet = Fleets.FirstOrDefault(f => f.Id == fleetId);
        if (fleet == null || _systems?.Salvage == null) return (_emptyInts, _emptyInts);
        var (s, e) = _systems.Salvage.GetFleetContributions(fleet, ShipsById);
        return (s, e);
    }

    private static readonly List<Colony> _emptyColonies = new();
    private static readonly List<Outpost> _emptyOutposts = new();
    private static readonly List<Station> _emptyStations = new();
    public IReadOnlyList<Colony> LiveColonies => _systems?.Settlements?.Colonies ?? _emptyColonies;
    public IReadOnlyList<Outpost> LiveOutposts => _systems?.Settlements?.Outposts ?? _emptyOutposts;
    public IReadOnlyList<Station> LiveStations => _systems?.Stations?.Stations ?? _emptyStations;

    public TechTreeRegistry? TechRegistry => _systems?.TechRegistry;
    public EmpireResearchState? GetResearchState(int empireId) => _systems?.GetResearchState(empireId);
}
