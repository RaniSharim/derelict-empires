using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;
using DerlictEmpires.Core.Ships;
using DerlictEmpires.Core.Stations;
using DerlictEmpires.Core.Systems;
using DerlictEmpires.Core.Tech;
using DerlictEmpires.Nodes.Camera;
using DerlictEmpires.Nodes.UI;
using DerlictEmpires.Nodes.Units;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Root scene that assembles the galaxy map, camera, and UI layers.
/// Supports two startup paths:
///   1. New Game — setup dialog → galaxy generation → play
///   2. Load Game — apply a GameSaveData directly (from save file or MCP bridge)
/// </summary>
public partial class MainScene : Node3D
{
    private CanvasLayer _uiLayer = null!;
    private GameSetupDialog? _setupDialog;
    private GalaxyMap _galaxyMap = null!;

    // Game data (owned here, mirrored to GameManager)
    private List<EmpireData> _empires = new();
    private List<FleetData> _fleets = new();
    private List<ShipInstanceData> _ships = new();
    private List<ColonyData> _colonyDatas = new();
    private List<StationData> _stationDatas = new();

    // Fleet management
    private Node3D _fleetContainer = null!;
    private Dictionary<int, FleetNode> _fleetNodes = new();
    private FleetMovementSystem? _movementSystem;
    private SelectionManager _selection = new();
    private FleetInfoPanel _fleetInfoPanel = null!;
    private LeftPanel _leftPanel = null!;
    private FleetOrderIndicator _pathIndicator = null!;
    private readonly HashSet<int> _selectedFleetIds = new();
    private int _primarySelectedFleetId = -1;  // most recently added — drives path indicator

    // Resource extraction
    private ResourceExtractionSystem? _extractionSystem;
    private TopBar _topBar = null!;
    private ResourcePanel _resourcePanel = null!;
    private SystemResourceView _systemResourceView = null!;
    private int _incomeUpdateCounter;

    // Settlements
    private DerlictEmpires.Core.Settlements.SettlementSystem? _settlementSystem;
    private ColonyPanel _colonyPanel = null!;

    // Stations
    private StationSystem? _stationSystem;
    private List<Station> _stations = new();
    private StationPanel _stationPanel = null!;

    // Research
    private TechTreeRegistry? _techRegistry;
    private ResearchEngine? _researchEngine;
    private Dictionary<int, EmpireResearchState> _researchStates = new();
    private ResearchPanel _researchPanel = null!;

    // Salvage / exploration
    private ExplorationManager _exploration = null!;
    private SalvageSystem? _salvageSystem;
    private Dictionary<int, ShipInstanceData> _shipsById = new();
    private Dictionary<int, EmpireData> _empiresById = new();

    public override void _Ready()
    {
        McpLog.Info("[MainScene] Starting Derelict Empires...");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CurrentState = GameState.Setup;
            GameManager.Instance.CurrentSpeed = GameSpeed.Paused;
        }

        // Galaxy map (3D world) — no longer auto-generates
        _galaxyMap = new GalaxyMap { Name = "GalaxyMap" };
        AddChild(_galaxyMap);

        // Fleet container
        _fleetContainer = new Node3D { Name = "Fleets" };
        AddChild(_fleetContainer);

        // Path indicator
        _pathIndicator = new FleetOrderIndicator { Name = "PathIndicator" };
        AddChild(_pathIndicator);

        // Camera rig
        var cameraRig = new StrategyCameraRig { Name = "CameraRig" };
        AddChild(cameraRig);

        var camera = new Camera3D { Name = "Camera3D" };
        camera.Position = new Vector3(0, 80, 24);
        camera.RotationDegrees = new Vector3(-70, 0, 0);
        camera.Far = 1000f;
        cameraRig.AddChild(camera);

        // UI layer
        _uiLayer = new CanvasLayer { Name = "UILayer" };
        AddChild(_uiLayer);

        _topBar = new TopBar { Name = "TopBar" };
        _uiLayer.AddChild(_topBar);

        // New UI panels
        _leftPanel = new LeftPanel { Name = "LeftPanel" };
        _uiLayer.AddChild(_leftPanel);
        _leftPanel.SetMainScene(this);

        var rightPanel = new RightPanel { Name = "RightPanel" };
        _uiLayer.AddChild(rightPanel);
        rightPanel.SetMainScene(this);

        var speedWidget = new SpeedTimeWidget { Name = "SpeedTimeWidget" };
        _uiLayer.AddChild(speedWidget);

        // Event log — bottom-right
        var eventLog = new EventLog { Name = "EventLog" };
        _uiLayer.AddChild(eventLog);

        // Minimap — bottom-left
        var minimap = new Minimap { Name = "Minimap" };
        _uiLayer.AddChild(minimap);

        // Old panels — kept for data wiring but hidden until replaced
        var tooltip = new SystemTooltip { Name = "SystemTooltip" };
        tooltip.Visible = false;
        _uiLayer.AddChild(tooltip);

        // FleetInfoPanel is deliberately NOT added: its input area covered the right 260px
        // of the screen once visible, intercepting clicks destined for RightPanel's SCAN/
        // EXTRACT buttons. LeftPanel's fleet cards carry the same info in MVP.
        _fleetInfoPanel = new FleetInfoPanel { Name = "FleetInfoPanel" };
        _fleetInfoPanel.Visible = false;

        _resourcePanel = new ResourcePanel { Name = "ResourcePanel" };
        _resourcePanel.Visible = false;
        _uiLayer.AddChild(_resourcePanel);

        _systemResourceView = new SystemResourceView { Name = "SystemResourceView" };
        _systemResourceView.Visible = false;
        _uiLayer.AddChild(_systemResourceView);

        _colonyPanel = new ColonyPanel { Name = "ColonyPanel" };
        _colonyPanel.Visible = false;
        _uiLayer.AddChild(_colonyPanel);

        _stationPanel = new StationPanel { Name = "StationPanel" };
        _stationPanel.Visible = false;
        _uiLayer.AddChild(_stationPanel);

        _researchPanel = new ResearchPanel { Name = "ResearchPanel" };
        _researchPanel.Visible = false;
        _uiLayer.AddChild(_researchPanel);


        // MVP: skip the setup dialog; auto-start immediately.
        EventBus.Instance.FleetSelected += OnFleetSelected;
        EventBus.Instance.FleetSelectionToggled += OnFleetSelectionToggled;
        EventBus.Instance.FleetDeselected += OnFleetDeselected;
        EventBus.Instance.SystemRightClicked += OnSystemRightClickedForMove;
        EventBus.Instance.FastTick += OnFastTick;
        EventBus.Instance.SlowTick += OnSlowTick;

        McpLog.Info("[MainScene] Auto-starting MVP salvage loop...");
        CallDeferred(nameof(StartMvpGame));
    }

    private void StartMvpGame()
    {
        OnSetupConfirmed((int)PrecursorColor.Red, (int)Origin.Servitors);
    }

    // ── New Game Path ────────────────────────────────────────────

    private void OnSetupConfirmed(int colorIndex, int originIndex)
    {
        var affinity = (PrecursorColor)colorIndex;
        var origin = (Origin)originIndex;
        McpLog.Info($"[MainScene] MVP start — {affinity} {origin}");

        var gm = GameManager.Instance;
        if (gm == null) return;

        gm.MasterSeed = 42;
        var config = new GalaxyGenerationConfig
        {
            Seed = gm.MasterSeed,
            TotalSystems = 20,
            ArmCount = 4,
            GalaxyRadius = 120f,
            MaxLaneLength = 60f,
            MinNeighbors = 2,
            MaxNeighbors = 4,
            HiddenLaneRatio = 0.0f
        };
        var galaxy = GalaxyGenerator.Generate(config);
        gm.Galaxy = galaxy;
        _galaxyMap.LoadGalaxy(galaxy);

        var rng = new GameRandom(gm.MasterSeed);
        var setupManager = new GameSetupManager();
        var setupResult = new GameSetupManager.SetupResult();

        setupManager.CreateMvpPlayerEmpire("Player Empire", affinity, origin, galaxy, setupResult, rng.DeriveChild("player"));

        _empires = setupResult.Empires;
        _fleets = setupResult.Fleets;
        _ships = setupResult.Ships;
        _colonyDatas = setupResult.Colonies;
        _stationDatas = setupResult.Stations;
        gm.Empires = _empires;
        _empiresById = _empires.ToDictionary(e => e.Id);
        _shipsById = _ships.ToDictionary(s => s.Id);

        var player = _empires.First();
        var homeSys = galaxy.GetSystem(player.HomeSystemId);
        McpLog.Info($"  Player home: {homeSys?.Name} ({galaxy.SalvageSites.Count} salvage sites galaxy-wide)");
        McpLog.Info($"  {_fleets.Count} fleets, {_ships.Count} ships");

        InitGameSystems(galaxy);
        InitSalvage(galaxy, player);

        gm.CurrentState = GameState.Playing;
        gm.CurrentSpeed = GameSpeed.Normal;
        McpLog.Info("[MainScene] MVP loop running!");
    }

    private void InitSalvage(GalaxyData galaxy, EmpireData player)
    {
        _exploration = new ExplorationManager();

        // Surface per-empire exploration events to the bus.
        _exploration.SiteDiscovered += (eid, pid) => EventBus.Instance?.FireSiteDiscovered(eid, pid);
        _exploration.ScanProgressChanged += (eid, pid, prog, diff) =>
            EventBus.Instance?.FireScanProgressChanged(eid, pid, prog, diff);
        _exploration.SiteScanComplete += (eid, pid) => EventBus.Instance?.FireSiteScanComplete(eid, pid);

        _salvageSystem = new SalvageSystem(galaxy, _exploration, MvpShipDesigns.Registry);
        _salvageSystem.YieldExtracted += (eid, pid, key, amt) =>
            EventBus.Instance?.FireYieldExtracted(eid, pid, key, amt);
        _salvageSystem.ActivityChanged += (eid, pid, act) =>
        {
            EventBus.Instance?.FireSiteActivityChanged(eid, pid, act);
            UpdateTopBarDelta();
        };
        _salvageSystem.ActivityRateChanged += (eid, pid) =>
        {
            EventBus.Instance?.FireSiteActivityRateChanged(eid, pid);
            UpdateTopBarDelta();
        };

        // Pre-survey the home system's salvage sites for the player.
        var home = galaxy.GetSystem(player.HomeSystemId);
        if (home != null)
        {
            foreach (var poi in home.POIs)
                if (poi.SalvageSiteId.HasValue)
                    _exploration.SurveyPOI(player.Id, poi.Id, 100);
        }
    }

    // ── Load Game Path ───────────────────────────────────────────

    /// <summary>
    /// Load a complete game state from a GameSaveData.
    /// Called by McpBridge's load_state command or future save/load UI.
    /// </summary>
    public void LoadGame(GameSaveData saveData)
    {
        McpLog.Info($"[MainScene] Loading game state (v{saveData.Version}, seed={saveData.MasterSeed})...");

        // Remove setup dialog if still showing
        if (_setupDialog != null)
        {
            _setupDialog.QueueFree();
            _setupDialog = null;
        }

        // Clear existing fleet nodes
        foreach (var node in _fleetNodes.Values)
            node.QueueFree();
        _fleetNodes.Clear();
        _selectedFleetIds.Clear();
        _primarySelectedFleetId = -1;

        var gm = GameManager.Instance;
        if (gm == null) return;

        // Apply core state
        gm.MasterSeed = saveData.MasterSeed;
        gm.GameTime = saveData.GameTime;
        gm.Galaxy = saveData.Galaxy;
        gm.Empires = saveData.Empires;

        // Store local references
        _empires = saveData.Empires;
        _fleets = saveData.Fleets;
        _ships = saveData.Ships;
        _colonyDatas = saveData.Colonies;
        _stationDatas = saveData.Stations;

        // Render galaxy
        _galaxyMap.LoadGalaxy(saveData.Galaxy);

        // Init game systems
        InitGameSystems(saveData.Galaxy);

        // Restore fleet orders
        foreach (var orderData in saveData.FleetOrders)
        {
            var fleet = _fleets.FirstOrDefault(f => f.Id == orderData.FleetId);
            if (fleet == null || _movementSystem == null) continue;

            var order = new FleetOrder
            {
                Type = orderData.Type,
                Path = orderData.Path,
                PathIndex = orderData.PathIndex,
                LaneProgress = orderData.LaneProgress,
                TransitFromSystemId = orderData.TransitFromSystemId
            };
            _movementSystem.RestoreOrder(fleet, order);
        }

        // Restore extractions
        _extractionSystem = new ResourceExtractionSystem();
        _extractionSystem.RegisterGalaxy(saveData.Galaxy);
        foreach (var extraction in saveData.Extractions)
            _extractionSystem.AddAssignment(extraction);

        _extractionSystem.DepositDepleted += (empireId, deposit) =>
            McpLog.Info($"[Resources] Deposit depleted for empire {empireId}: {deposit.Color} {deposit.Type}");

        // Restore settlements
        InitSettlements();

        // Restore stations
        InitStations();

        // Restore research
        var rng = new GameRandom(saveData.MasterSeed);
        InitResearch(rng);
        // Overwrite with saved research states if present
        foreach (var rs in saveData.ResearchStates)
        {
            _researchStates[rs.EmpireId] = StateConverter.ToResearchState(rs);
        }

        // Start game
        gm.CurrentState = GameState.Playing;
        gm.CurrentSpeed = saveData.GameSpeed;
        McpLog.Info($"[MainScene] Game loaded! {_empires.Count} empires, {_fleets.Count} fleets, {_colonyDatas.Count} colonies, {_stations.Count} stations");
    }

    /// <summary>
    /// Capture current game state into a GameSaveData for serialization.
    /// </summary>
    public GameSaveData BuildGameSaveData()
    {
        var gm = GameManager.Instance;
        var saveData = new GameSaveData
        {
            MasterSeed = gm?.MasterSeed ?? 0,
            GameTime = gm?.GameTime ?? 0,
            GameSpeed = gm?.CurrentSpeed ?? GameSpeed.Paused,
            Galaxy = gm?.Galaxy ?? new GalaxyData(),
            Empires = _empires,
            Fleets = _fleets,
            Ships = _ships,
            Colonies = _colonyDatas,
            Stations = _stations.Select(StateConverter.ToStationData).ToList(),
            Extractions = _extractionSystem?.AllAssignments.ToList() ?? new(),
        };

        // Save fleet orders
        if (_movementSystem != null)
        {
            foreach (var fleet in _fleets)
            {
                var order = _movementSystem.GetOrder(fleet.Id);
                if (order != null && !order.IsComplete)
                {
                    saveData.FleetOrders.Add(new FleetOrderSaveData
                    {
                        FleetId = fleet.Id,
                        Type = order.Type,
                        Path = order.Path,
                        PathIndex = order.PathIndex,
                        LaneProgress = order.LaneProgress,
                        TransitFromSystemId = order.TransitFromSystemId
                    });
                }
            }
        }

        // Save research states
        foreach (var kvp in _researchStates)
            saveData.ResearchStates.Add(StateConverter.ToResearchSaveData(kvp.Value));

        return saveData;
    }

    // ── Shared Init ──────────────────────────────────────────────

    private void InitGameSystems(GalaxyData galaxy)
    {
        _movementSystem = new FleetMovementSystem(galaxy);
        _movementSystem.FleetArrived += (fleet, sysId) =>
        {
            var sys = galaxy.GetSystem(sysId);
            McpLog.Info($"[Fleet] {fleet.Name} arrived at {sys?.Name}");
            EventBus.Instance.FireFleetArrivedAtSystem(fleet.Id, sysId);

            if (sys != null && _exploration != null)
            {
                var poiIds = sys.POIs.Select(p => p.Id).ToList();
                _exploration.DiscoverSystem(fleet.OwnerEmpireId, sysId, poiIds);
            }

            // Capability in this system may have just changed — nudge rate-dependent UI.
            _salvageSystem?.NotifyFleetMovedSystem(fleet.OwnerEmpireId, sysId);
        };
        _movementSystem.FleetDeparted += (fleet, fromSysId) =>
        {
            _salvageSystem?.NotifyFleetMovedSystem(fleet.OwnerEmpireId, fromSysId);
        };
        _movementSystem.OrderCompleted += fleet =>
            EventBus.Instance?.FireFleetOrderChanged(fleet.Id);

        // _fleetInfoPanel kept as dormant object; not in the tree. No SetData call.
        _leftPanel.SetData(_fleets, _ships);
        SpawnFleetNodes(galaxy);
    }

    private void InitExtractions(GalaxyData galaxy)
    {
        _extractionSystem = new ResourceExtractionSystem();
        _extractionSystem.RegisterGalaxy(galaxy);

        int assignmentId = 0;
        foreach (var empire in _empires)
        {
            var homeSys = galaxy.GetSystem(empire.HomeSystemId);
            if (homeSys == null) continue;

            var assignments = ResourceDistributionHelper.CreateHomeExtractions(
                empire.Id, homeSys, assignmentId);
            foreach (var a in assignments)
                _extractionSystem.AddAssignment(a);
            assignmentId += assignments.Count;
        }

        _extractionSystem.DepositDepleted += (empireId, deposit) =>
            McpLog.Info($"[Resources] Deposit depleted for empire {empireId}: {deposit.Color} {deposit.Type}");

        McpLog.Info($"  Extraction assignments: {_extractionSystem.AllAssignments.Count}");
    }

    private void InitSettlements()
    {
        _settlementSystem = new DerlictEmpires.Core.Settlements.SettlementSystem();
        foreach (var colonyData in _colonyDatas)
        {
            var colony = new DerlictEmpires.Core.Settlements.Colony
            {
                Id = colonyData.Id,
                Name = colonyData.Name,
                OwnerEmpireId = colonyData.OwnerEmpireId,
                SystemId = colonyData.SystemId,
                POIId = colonyData.POIId,
                PlanetSize = colonyData.PlanetSize,
                Happiness = colonyData.Happiness,
            };
            colony.PopGroups.Add(new DerlictEmpires.Core.Settlements.PopGroup
            {
                Count = colonyData.Population,
                Allocation = WorkPool.Food
            });
            DerlictEmpires.Core.Settlements.PopAllocationManager.AutoAllocate(colony);

            var farm = DerlictEmpires.Core.Settlements.BuildingData.FindById("food_farm");
            if (farm != null)
                colony.Queue.Enqueue(new DerlictEmpires.Core.Settlements.BuildingProducible(farm));

            _settlementSystem.AddColony(colony);
        }

        _settlementSystem.BuildingCompleted += (colony, buildingId) =>
            McpLog.Info($"[Colony] {colony.Name} completed building: {buildingId}");
        _settlementSystem.PopulationGrew += colony =>
            McpLog.Info($"[Colony] {colony.Name} population grew to {colony.TotalPopulation}");

        // Colony panel hidden — LeftPanel COLONIES tab will replace this
        // var playerColony = _settlementSystem.Colonies
        //     .FirstOrDefault(c => c.OwnerEmpireId == (_empires.FirstOrDefault(e => e.IsHuman)?.Id ?? -1));
        // if (playerColony != null)
        //     _colonyPanel.Show(playerColony);

        McpLog.Info($"  Colonies: {_settlementSystem.Colonies.Count}");
    }

    private void InitStations()
    {
        _stationSystem = new StationSystem();
        _stations.Clear();

        foreach (var stationData in _stationDatas)
        {
            var station = StateConverter.ToStation(stationData);
            _stationSystem.AddStation(station);
            _stations.Add(station);
        }

        _stationSystem.ModuleInstalled += (station, module) =>
        {
            McpLog.Info($"[Station] {station.Name} installed module: {module.DisplayName}");
            EventBus.Instance?.FireStationModuleInstalled(station.Id, station.OwnerEmpireId);
        };

        // Station panel hidden — will be replaced by new UI
        // var playerStation = _stations
        //     .FirstOrDefault(s => s.OwnerEmpireId == (_empires.FirstOrDefault(e => e.IsHuman)?.Id ?? -1));
        // if (playerStation != null)
        //     _stationPanel.Show(playerStation);

        McpLog.Info($"  Stations: {_stations.Count}");
    }

    private void InitResearch(GameRandom rng)
    {
        _techRegistry = new TechTreeRegistry();
        _researchEngine = new ResearchEngine(_techRegistry);
        _researchStates.Clear();

        // Create initial research state per empire
        foreach (var empire in _empires)
        {
            var state = StateConverter.CreateInitialResearchState(
                empire.Id, empire.Affinity, _techRegistry, rng.DeriveChild($"research_{empire.Id}"));
            _researchStates[empire.Id] = state;

            // Auto-start: queue first available subsystem for research
            if (state.AvailableSubsystems.Count > 0 && state.CurrentProject == null)
            {
                state.CurrentProject = state.AvailableSubsystems.First();
                McpLog.Info($"[Research] {empire.Name} auto-started: {state.CurrentProject}");
            }
        }

        _researchEngine.SubsystemResearched += (empireId, subId) =>
        {
            var empire = _empires.FirstOrDefault(e => e.Id == empireId);
            McpLog.Info($"[Research] {empire?.Name} completed subsystem: {subId}");
            EventBus.Instance?.FireSubsystemResearched(empireId, subId);

            // Auto-queue next available subsystem
            var state = _researchStates.GetValueOrDefault(empireId);
            if (state != null && state.CurrentProject == null && state.AvailableSubsystems.Count > 0)
            {
                state.CurrentProject = state.AvailableSubsystems.First();
            }
        };

        _researchEngine.TierUnlocked += (empireId, color, category, tier) =>
        {
            var empire = _empires.FirstOrDefault(e => e.Id == empireId);
            McpLog.Info($"[Research] {empire?.Name} unlocked {color} {category} tier {tier}");
            EventBus.Instance?.FireTierUnlocked(empireId, color, category, tier);
        };

        // Research panel hidden — LeftPanel RESEARCH tab will replace this
        // var playerEmpire = _empires.FirstOrDefault(e => e.IsHuman);
        // if (playerEmpire != null && _researchStates.TryGetValue(playerEmpire.Id, out var playerState))
        //     _researchPanel.SetState(playerState, _techRegistry);

        McpLog.Info($"  Research states: {_researchStates.Count} ({_techRegistry.Nodes.Count} tech nodes)");
    }

    // ── Fleet Visuals ────────────────────────────────────────────

    private void SpawnFleetNodes(GalaxyData galaxy)
    {
        int playerEmpireId = _empires.FirstOrDefault(e => e.IsHuman)?.Id ?? -1;

        foreach (var fleet in _fleets)
        {
            var node = new FleetNode();
            _fleetContainer.AddChild(node);
            node.Initialize(fleet, fleet.OwnerEmpireId == playerEmpireId);

            var sys = galaxy.GetSystem(fleet.CurrentSystemId);
            if (sys != null)
                node.UpdatePosition(sys.PositionX, sys.PositionZ);

            node.UpdateLabel();
            _fleetNodes[fleet.Id] = node;
        }
    }

    // ── Fleet Selection & Movement ───────────────────────────────

    /// <summary>Replace selection with a single fleet.</summary>
    private void OnFleetSelected(int fleetId)
    {
        foreach (int prevId in _selectedFleetIds)
            if (_fleetNodes.TryGetValue(prevId, out var prevNode)) prevNode.SetSelected(false);

        _selectedFleetIds.Clear();
        _selectedFleetIds.Add(fleetId);
        _primarySelectedFleetId = fleetId;
        _selection.SelectFleet(fleetId);

        if (_fleetNodes.TryGetValue(fleetId, out var node))
            node.SetSelected(true);

        UpdatePathIndicator();
    }

    /// <summary>Ctrl-click: add/remove the fleet from the current selection.</summary>
    private void OnFleetSelectionToggled(int fleetId)
    {
        if (_selectedFleetIds.Remove(fleetId))
        {
            if (_fleetNodes.TryGetValue(fleetId, out var n)) n.SetSelected(false);
            if (_primarySelectedFleetId == fleetId)
                _primarySelectedFleetId = _selectedFleetIds.Count > 0 ? _selectedFleetIds.First() : -1;
        }
        else
        {
            _selectedFleetIds.Add(fleetId);
            _primarySelectedFleetId = fleetId;
            if (_fleetNodes.TryGetValue(fleetId, out var n)) n.SetSelected(true);
        }
        _selection.SelectFleet(_primarySelectedFleetId);
        UpdatePathIndicator();
    }

    private void OnFleetDeselected()
    {
        foreach (int id in _selectedFleetIds)
            if (_fleetNodes.TryGetValue(id, out var node)) node.SetSelected(false);

        _selectedFleetIds.Clear();
        _primarySelectedFleetId = -1;
        _selection.Deselect();
        _pathIndicator.Clear();
    }

    private void OnSystemRightClickedForMove(StarSystemData targetSystem)
    {
        if (_selectedFleetIds.Count == 0 || _movementSystem == null) return;

        var gm = GameManager.Instance;
        if (gm?.Galaxy == null) return;

        var playerEmpire = _empires.FirstOrDefault(e => e.IsHuman);
        if (playerEmpire == null) return;

        bool canUseHidden = playerEmpire.Origin == Origin.Haulers;

        foreach (int fleetId in _selectedFleetIds)
        {
            var fleet = _fleets.FirstOrDefault(f => f.Id == fleetId);
            if (fleet == null || fleet.OwnerEmpireId != playerEmpire.Id) continue;

            int sourceId = fleet.CurrentSystemId;
            if (sourceId < 0)
            {
                var order = _movementSystem.GetOrder(fleet.Id);
                if (order != null && order.NextSystemId >= 0) sourceId = order.NextSystemId;
                else continue;
            }
            if (sourceId == targetSystem.Id) continue;

            var path = LanePathfinder.FindPath(gm.Galaxy, sourceId, targetSystem.Id, canUseHidden);
            if (path.Count == 0)
            {
                McpLog.Info($"[Fleet] No path from system {sourceId} to {targetSystem.Name}");
                continue;
            }

            McpLog.Info($"[Fleet] {fleet.Name} moving to {targetSystem.Name} ({path.Count} hops)");
            _movementSystem.IssueMoveOrder(fleet, path);
            EventBus.Instance?.FireFleetOrderChanged(fleet.Id);
        }

        UpdatePathIndicator();
    }

    private void UpdatePathIndicator()
    {
        var gm = GameManager.Instance;
        if (gm?.Galaxy == null || _movementSystem == null || _primarySelectedFleetId < 0)
        {
            _pathIndicator.Clear();
            return;
        }

        var order = _movementSystem.GetOrder(_primarySelectedFleetId);
        var fleet = _fleets.FirstOrDefault(f => f.Id == _primarySelectedFleetId);
        if (order == null || fleet == null || order.IsComplete)
        {
            _pathIndicator.Clear();
            return;
        }

        int fromId = order.TransitFromSystemId >= 0 ? order.TransitFromSystemId : fleet.CurrentSystemId;
        var remaining = order.Path.Skip(order.PathIndex).ToList();
        _pathIndicator.ShowPath(gm.Galaxy, fromId, remaining);
    }

    // ── Tick Processing ──────────────────────────────────────────

    private void OnFastTick(float delta)
    {
        if (_movementSystem == null) return;

        _movementSystem.ProcessTick(delta, _fleets);
        _salvageSystem?.ProcessTick(delta, _fleets, _shipsById, _empiresById);

        foreach (var fleet in _fleets)
        {
            if (!_fleetNodes.TryGetValue(fleet.Id, out var node)) continue;
            var (x, z) = _movementSystem.GetFleetPosition(fleet);
            node.UpdatePosition(x, z);
        }

        if (_primarySelectedFleetId >= 0)
            UpdatePathIndicator();
    }

    private void OnSlowTick(float delta)
    {
        UpdateTopBarDelta();
    }

    /// <summary>
    /// Compute expected per-second resource income from active Extracting activities
    /// and push to the top-bar so +X deltas update in real time.
    /// </summary>
    private void UpdateTopBarDelta()
    {
        var player = PlayerEmpire;
        if (player == null || _salvageSystem == null) { _topBar.UpdateIncome(new()); return; }

        var income = new Dictionary<string, float>();
        foreach (var kv in _salvageSystem.AllActivities)
        {
            if (kv.Value.Activity != SiteActivity.Extracting) continue;
            var poi = FindPOI(kv.Key.poiId, out int sysId);
            if (poi == null || poi.SalvageSiteId is not int siteId) continue;
            var site = GetSalvageSite(siteId);
            if (site == null) continue;

            float cap = _salvageSystem.ComputeSystemCapability(
                player.Id, sysId, SiteActivity.Extracting, _fleets, _shipsById);
            int n = _salvageSystem.CountActiveInSystem(player.Id, sysId, SiteActivity.Extracting);
            if (n == 0 || cap <= 0f) continue;
            float perSite = cap / n;

            // Rate for a 1-second window.
            var yields = ExtractionCalculator.PerTickYield(
                site.TotalYield, site.RemainingYield, perSite, site.DepletionCurveExponent, 1.0f);
            foreach (var y in yields)
                income[y.Key] = income.GetValueOrDefault(y.Key) + y.Value;
        }
        _topBar.UpdateIncome(income);
    }

    // ── Input ────────────────────────────────────────────────────

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
        {
            if (_selectedFleetIds.Count > 0)
            {
                EventBus.Instance.FireFleetDeselected();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.FleetSelected -= OnFleetSelected;
            EventBus.Instance.FleetSelectionToggled -= OnFleetSelectionToggled;
            EventBus.Instance.FleetDeselected -= OnFleetDeselected;
            EventBus.Instance.SystemRightClicked -= OnSystemRightClickedForMove;
            EventBus.Instance.FastTick -= OnFastTick;
            EventBus.Instance.SlowTick -= OnSlowTick;
        }
    }

    // Public accessors for UI action handlers (SCAN / EXTRACT / CANCEL buttons).
    public SalvageSystem? SalvageSystem => _salvageSystem;
    public ExplorationManager? ExplorationManager => _exploration;
    public FleetMovementSystem? MovementSystem => _movementSystem;
    public IReadOnlyList<FleetData> Fleets => _fleets;
    public EmpireData? PlayerEmpire => _empires.FirstOrDefault(e => e.IsHuman);
    public int SelectedFleetId => _primarySelectedFleetId;
    public IReadOnlyCollection<int> SelectedFleetIds => _selectedFleetIds;

    public IReadOnlyDictionary<int, ShipInstanceData> ShipsById => _shipsById;

    /// <summary>UI helper: scout-capable player fleets in this POI's system.</summary>
    public float GetSystemCapability(int poiId, SiteActivity type)
    {
        var player = PlayerEmpire;
        if (player == null || _salvageSystem == null) return 0f;
        var poi = FindPOI(poiId, out int sysId);
        if (poi == null) return 0f;
        return _salvageSystem.ComputeSystemCapability(player.Id, sysId, type, _fleets, _shipsById);
    }

    public int GetSystemActiveCount(int poiId, SiteActivity type)
    {
        var player = PlayerEmpire;
        if (player == null || _salvageSystem == null) return 0;
        var poi = FindPOI(poiId, out int sysId);
        if (poi == null) return 0;
        return _salvageSystem.CountActiveInSystem(player.Id, sysId, type);
    }

    /// <summary>UI button handler: toggle SCAN on a POI. No fleet selection required — any
    /// capable fleet in the POI's system auto-contributes.</summary>
    public bool TryToggleScan(int poiId)
    {
        if (_salvageSystem == null) { McpLog.Warn("[Scan] rejected: system not ready"); return false; }
        var player = PlayerEmpire;
        if (player == null) return false;

        var current = _salvageSystem.GetActivity(player.Id, poiId);
        var next = current == SiteActivity.Scanning ? SiteActivity.None : SiteActivity.Scanning;
        bool changed = _salvageSystem.RequestActivity(player.Id, poiId, next);
        if (changed) McpLog.Info($"[Scan] POI {poiId} → {next}");
        return changed;
    }

    public bool TryToggleExtract(int poiId)
    {
        if (_salvageSystem == null) { McpLog.Warn("[Extract] rejected: system not ready"); return false; }
        var player = PlayerEmpire;
        if (player == null) return false;

        var current = _salvageSystem.GetActivity(player.Id, poiId);
        var next = current == SiteActivity.Extracting ? SiteActivity.None : SiteActivity.Extracting;
        bool changed = _salvageSystem.RequestActivity(player.Id, poiId, next);
        if (changed) McpLog.Info($"[Extract] POI {poiId} → {next}");
        return changed;
    }

    private POIData? FindPOI(int poiId, out int systemId)
    {
        systemId = -1;
        var gm = GameManager.Instance;
        if (gm?.Galaxy == null) return null;
        foreach (var sys in gm.Galaxy.Systems)
            foreach (var poi in sys.POIs)
                if (poi.Id == poiId) { systemId = sys.Id; return poi; }
        return null;
    }

    /// <summary>Salvage-site lookup for the UI.</summary>
    public SalvageSiteData? GetSalvageSite(int siteId) =>
        GameManager.Instance?.Galaxy?.GetSalvageSite(siteId);
}
