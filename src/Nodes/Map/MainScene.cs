using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Combat;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;
using DerlictEmpires.Core.Settlements;
using DerlictEmpires.Core.Ships;
using DerlictEmpires.Core.Stations;
using DerlictEmpires.Core.Systems;
using DerlictEmpires.Core.Tech;
using DerlictEmpires.Nodes.Camera;
using DerlictEmpires.Nodes.UI;
using DerlictEmpires.Nodes.UI.ShipDesigner;
using DerlictEmpires.Nodes.UI.SystemView;
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
    private GalaxyMap _galaxyMap = null!;
    private StrategyCameraRig _cameraRig = null!;

    // Game data lives on GameManager. Read via GameManager.Instance.{Empires|Fleets|Ships|Colonies|StationDatas|EmpiresById|ShipsById}.

    // Fleet management
    private Node3D _fleetContainer = null!;
    private Dictionary<int, FleetNode> _fleetNodes = new();
    private FleetMovementSystem? _movementSystem;
    private SelectionManager _selection = new();
    private LeftPanel _leftPanel = null!;
    private FleetOrderIndicator _pathIndicator = null!;
    private readonly HashSet<int> _selectedFleetIds = new();
    private int _primarySelectedFleetId = -1;  // most recently added — drives path indicator

    // Resource extraction
    private ResourceExtractionSystem? _extractionSystem;
    private TopBar _topBar = null!;

    // Settlements
    private SettlementSystem? _settlementSystem;

    // Stations
    private StationSystem? _stationSystem;
    private List<Station> _stations = new();

    // Research
    private TechTreeRegistry? _techRegistry;
    private ResearchEngine? _researchEngine;
    private Dictionary<int, EmpireResearchState> _researchStates = new();

    /// <summary>Tech tree registry (read-only). Available after InitResearch runs.</summary>
    public TechTreeRegistry? TechRegistry => _techRegistry;

    /// <summary>Research engine (used by UI to start projects). Available after InitResearch.</summary>
    public ResearchEngine? ResearchEngine => _researchEngine;

    /// <summary>Local player's research state (null if game not yet loaded or no player empire).</summary>
    public EmpireResearchState? PlayerResearchState
    {
        get
        {
            var playerId = GameManager.Instance?.LocalPlayerEmpire?.Id ?? -1;
            return playerId >= 0 ? _researchStates.GetValueOrDefault(playerId) : null;
        }
    }

    // Salvage / exploration
    private ExplorationManager _exploration = null!;
    private SalvageSystem? _salvageSystem;

    // Dev harness — debug shortcuts and seed helpers
    private DevHarness _devHarness = null!;

    // Overlay router — tech tree, designer, system view
    private OverlayRouter _overlayRouter = null!;

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
        _cameraRig = new StrategyCameraRig { Name = "CameraRig" };
        AddChild(_cameraRig);

        var camera = new Camera3D { Name = "Camera3D" };
        camera.Position = new Vector3(0, 80, 24);
        camera.RotationDegrees = new Vector3(-70, 0, 0);
        camera.Far = 1000f;
        _cameraRig.AddChild(camera);

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

        // Dev harness — debug input (F7/F10/F11/F12/Shift+B) and seed helpers.
        _devHarness = new DevHarness { Name = "DevHarness" };
        AddChild(_devHarness);
        _devHarness.Configure(this);

        // Overlay router — tech tree / designer / system view open requests.
        _overlayRouter = new OverlayRouter { Name = "OverlayRouter" };
        _overlayRouter.Configure(this, _uiLayer);
        AddChild(_overlayRouter);

        // Wire the topbar research strip to this scene for state lookup.
        _topBar.ResearchStrip.Configure(this);

        // MVP: skip the setup dialog; auto-start immediately.
        EventBus.Instance.FleetSelected += OnFleetSelected;
        EventBus.Instance.FleetSelectionToggled += OnFleetSelectionToggled;
        EventBus.Instance.FleetDoubleClicked += OnFleetDoubleClicked;
        EventBus.Instance.FleetDeselected += OnFleetDeselected;
        EventBus.Instance.SystemRightClicked += OnSystemRightClickedForMove;
        EventBus.Instance.FastTick += OnFastTick;
        EventBus.Instance.SlowTick += OnSlowTick;
        EventBus.Instance.CombatStartRequested += OnCombatStartRequested;

        McpLog.Info("[MainScene] Auto-starting MVP salvage loop...");
        CallDeferred(nameof(StartMvpGame));
    }

    private void StartMvpGame()
    {
        OnSetupConfirmed((int)PrecursorColor.Red, (int)Origin.Servitors);
        _devHarness.GrantShipSubsystems();   // Ship modules across all tiers for UI work
        _devHarness.SeedHomeColony();        // a starting colony at home for System View verification
    }

    // ── Combat routing ────────────────────────────────────────────

    private CombatPopup? _activeCombatPopup;
    private BattleManager? _battleManager;
    private int _activeBattleId = -1;
    private readonly Dictionary<int, BattleMarker> _battleMarkers = new();

    private void OnCombatStartRequested(int attackerFleetId, int defenderFleetId)
    {
        var attacker = GameManager.Instance.Fleets.FirstOrDefault(f => f.Id == attackerFleetId);
        var defender = GameManager.Instance.Fleets.FirstOrDefault(f => f.Id == defenderFleetId);
        if (attacker == null || defender == null)
        {
            McpLog.Warn($"[Combat] start rejected: fleet(s) not found (att={attackerFleetId}, def={defenderFleetId})");
            return;
        }

        var attackerEmp = GameManager.Instance.EmpiresById.GetValueOrDefault(attacker.OwnerEmpireId);
        var defenderEmp = GameManager.Instance.EmpiresById.GetValueOrDefault(defender.OwnerEmpireId);
        if (attackerEmp == null || defenderEmp == null) return;

        EngageCombat(attacker, attackerEmp, defender, defenderEmp);
    }

    private void EngageCombat(FleetData attacker, EmpireData attackerEmp,
                              FleetData defender, EmpireData defenderEmp)
    {
        if (_battleManager == null)
        {
            var gm = GameManager.Instance;
            _battleManager = new BattleManager(new GameRandom(gm?.MasterSeed ?? 42).DeriveChild("battles"));
            _battleManager.BattleEnded += OnBattleEndedInternal;
            _battleManager.BattleTicked += id => EventBus.Instance?.FireBattleTick(id);
        }

        int battleId = _battleManager.StartBattle(attacker, attackerEmp, defender, defenderEmp,
            GameManager.Instance.ShipsById, attacker.CurrentSystemId);
        _activeBattleId = battleId;
        EventBus.Instance?.FireCombatStarted(battleId);

        // Pulsing red ring on the battle system.
        var sys = GameManager.Instance?.Galaxy?.GetSystem(attacker.CurrentSystemId);
        if (sys != null)
        {
            var marker = new BattleMarker { Name = $"BattleMarker_{battleId}" };
            marker.Position = new Vector3(sys.PositionX, 1.2f, sys.PositionZ);
            AddChild(marker);
            _battleMarkers[battleId] = marker;
        }

        var popup = new CombatPopup { Name = $"CombatPopup_{battleId}" };
        popup.Configure(_battleManager, battleId, attacker.CurrentSystemId, _cameraRig.GetNode<Camera3D>("Camera3D"));
        _uiLayer.AddChild(popup);
        _activeCombatPopup = popup;

        // Auto-select the battle's system so the popup is visible from the start.
        if (sys != null) EventBus.Instance?.FireSystemSelected(sys);
    }

    private void OnBattleEndedInternal(int battleId, CombatResult result)
    {
        McpLog.Info($"[Combat] Battle {battleId} ended: {result}");
        EventBus.Instance?.FireCombatEnded(battleId, result);

        // Do NOT free the popup — it swaps to debrief content on its own.
        // The popup frees itself when the user presses CONTINUE.
        _activeCombatPopup = null;

        if (_battleMarkers.TryGetValue(battleId, out var marker))
        {
            marker.QueueFree();
            _battleMarkers.Remove(battleId);
        }

        var gm = GameManager.Instance;
        if (gm != null) gm.CurrentSpeed = GameSpeed.Paused;
        EventBus.Instance?.FireGamePaused();

        _activeBattleId = -1;
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

        var playerEmpire = setupManager.CreateMvpPlayerEmpire("Player Empire", affinity, origin, galaxy, setupResult, rng.DeriveChild("player"));
        setupManager.CreateMvpHostileNeighbor(playerEmpire, galaxy, setupResult, rng.DeriveChild("hostile"));

        gm.LoadState(
            setupResult.Empires,
            setupResult.Fleets,
            setupResult.Ships,
            setupResult.Colonies,
            setupResult.Stations);

        var player = gm.Empires.First();
        var homeSys = galaxy.GetSystem(player.HomeSystemId);
        McpLog.Info($"  Player home: {homeSys?.Name} ({galaxy.SalvageSites.Count} salvage sites galaxy-wide)");
        McpLog.Info($"  {gm.Fleets.Count} fleets, {gm.Ships.Count} ships");

        InitGameSystems(galaxy);
        InitResearch(rng.DeriveChild("research"));
        InitSalvage(galaxy, player);
        InitSettlements();
        InitStations();

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
        gm.LoadState(
            saveData.Empires,
            saveData.Fleets,
            saveData.Ships,
            saveData.Colonies,
            saveData.Stations);

        // Render galaxy
        _galaxyMap.LoadGalaxy(saveData.Galaxy);

        // Init game systems
        InitGameSystems(saveData.Galaxy);

        // Restore fleet orders
        foreach (var orderData in saveData.FleetOrders)
        {
            var fleet = gm.Fleets.FirstOrDefault(f => f.Id == orderData.FleetId);
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
        McpLog.Info($"[MainScene] Game loaded! {gm.Empires.Count} empires, {gm.Fleets.Count} fleets, {gm.Colonies.Count} colonies, {_stations.Count} stations");
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
            Empires = gm.Empires,
            Fleets = gm.Fleets,
            Ships = gm.Ships,
            Colonies = gm.Colonies,
            Stations = _stations.Select(StateConverter.ToStationData).ToList(),
            Extractions = _extractionSystem?.AllAssignments.ToList() ?? new(),
        };

        // Save fleet orders
        if (_movementSystem != null)
        {
            foreach (var fleet in gm.Fleets)
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

        _leftPanel.SetData(GameManager.Instance.Fleets, GameManager.Instance.Ships);
        SpawnFleetNodes(galaxy);
    }

    private void InitSettlements()
    {
        _settlementSystem = new SettlementSystem();
        foreach (var colonyData in GameManager.Instance.Colonies)
        {
            var colony = new Colony
            {
                Id = colonyData.Id,
                Name = colonyData.Name,
                OwnerEmpireId = colonyData.OwnerEmpireId,
                SystemId = colonyData.SystemId,
                POIId = colonyData.POIId,
                PlanetSize = colonyData.PlanetSize,
                Happiness = colonyData.Happiness,
            };
            colony.PopGroups.Add(new PopGroup
            {
                Count = colonyData.Population,
                Allocation = WorkPool.Food
            });
            PopAllocationManager.AutoAllocate(colony);

            var farm = BuildingData.FindById("food_farm");
            if (farm != null)
                colony.Queue.Enqueue(new BuildingProducible(farm));

            _settlementSystem.AddColony(colony);
        }

        _settlementSystem.BuildingCompleted += (colony, buildingId) =>
            McpLog.Info($"[Colony] {colony.Name} completed building: {buildingId}");
        _settlementSystem.PopulationGrew += colony =>
            McpLog.Info($"[Colony] {colony.Name} population grew to {colony.TotalPopulation}");

        McpLog.Info($"  Colonies: {_settlementSystem.Colonies.Count}");
    }

    private void InitStations()
    {
        _stationSystem = new StationSystem();
        _stations.Clear();

        foreach (var stationData in GameManager.Instance.StationDatas)
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

        McpLog.Info($"  Stations: {_stations.Count}");
    }

    private void InitResearch(GameRandom rng)
    {
        _techRegistry = new TechTreeRegistry();
        _researchEngine = new ResearchEngine(_techRegistry);
        _researchStates.Clear();

        // Create initial research state per empire
        foreach (var empire in GameManager.Instance.Empires)
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
            var empire = GameManager.Instance.Empires.FirstOrDefault(e => e.Id == empireId);
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
            var empire = GameManager.Instance.Empires.FirstOrDefault(e => e.Id == empireId);
            McpLog.Info($"[Research] {empire?.Name} unlocked {color} {category} tier {tier}");
            EventBus.Instance?.FireTierUnlocked(empireId, color, category, tier);
        };

        McpLog.Info($"  Research states: {_researchStates.Count} ({_techRegistry.Nodes.Count} tech nodes)");
    }

    // ── Fleet Visuals ────────────────────────────────────────────

    private void SpawnFleetNodes(GalaxyData galaxy)
    {
        int playerEmpireId = GameManager.Instance.Empires.FirstOrDefault(e => e.IsHuman)?.Id ?? -1;

        foreach (var fleet in GameManager.Instance.Fleets)
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

    /// <summary>Double-click on a fleet pans the camera to its current world position.
    /// Using the fleet node's own transform works for both docked and in-transit fleets.</summary>
    private void OnFleetDoubleClicked(int fleetId)
    {
        if (_fleetNodes.TryGetValue(fleetId, out var node))
            _cameraRig.PanToWorld(node.GlobalPosition);
    }

    private void OnSystemRightClickedForMove(StarSystemData targetSystem)
    {
        if (_selectedFleetIds.Count == 0 || _movementSystem == null) return;

        var gm = GameManager.Instance;
        if (gm?.Galaxy == null) return;

        var playerEmpire = GameManager.Instance.Empires.FirstOrDefault(e => e.IsHuman);
        if (playerEmpire == null) return;

        bool canUseHidden = playerEmpire.Origin == Origin.Haulers;

        foreach (int fleetId in _selectedFleetIds)
        {
            var fleet = GameManager.Instance.Fleets.FirstOrDefault(f => f.Id == fleetId);
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
        var fleet = GameManager.Instance.Fleets.FirstOrDefault(f => f.Id == _primarySelectedFleetId);
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

        _movementSystem.ProcessTick(delta, GameManager.Instance.Fleets);
        _salvageSystem?.ProcessTick(delta, GameManager.Instance.Fleets, GameManager.Instance.ShipsById, GameManager.Instance.EmpiresById);
        _battleManager?.ProcessTick(delta);

        foreach (var fleet in GameManager.Instance.Fleets)
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
                player.Id, sysId, SiteActivity.Extracting, GameManager.Instance.Fleets, GameManager.Instance.ShipsById);
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
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.Escape)
            {
                if (_selectedFleetIds.Count > 0)
                {
                    EventBus.Instance.FireFleetDeselected();
                    GetViewport().SetInputAsHandled();
                }
            }
            else if (key.Keycode == Key.T)
            {
                var empire = GameManager.Instance?.LocalPlayerEmpire;
                var color = empire?.Affinity ?? PrecursorColor.Red;
                EventBus.Instance?.FireTechTreeOpenRequested(new TechTreeOpenRequest
                {
                    Color = color,
                    Intent = TechTreeIntent.View,
                });
                GetViewport().SetInputAsHandled();
            }
            // Shift+D to open the Ship Designer — plain D is consumed by camera_right panning.
            else if (key.Keycode == Key.D && key.ShiftPressed)
            {
                EventBus.Instance?.FireDesignerOpenRequested(new DesignerOpenRequest());
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
            EventBus.Instance.FleetDoubleClicked -= OnFleetDoubleClicked;
            EventBus.Instance.FleetDeselected -= OnFleetDeselected;
            EventBus.Instance.SystemRightClicked -= OnSystemRightClickedForMove;
            EventBus.Instance.FastTick -= OnFastTick;
            EventBus.Instance.SlowTick -= OnSlowTick;
            EventBus.Instance.CombatStartRequested -= OnCombatStartRequested;
        }
    }

    // Public accessors for UI action handlers (SCAN / EXTRACT / CANCEL buttons).
    public SalvageSystem? SalvageSystem => _salvageSystem;
    public ExplorationManager? ExplorationManager => _exploration;
    public FleetMovementSystem? MovementSystem => _movementSystem;
    public IReadOnlyList<FleetData> Fleets => GameManager.Instance.Fleets;
    public EmpireData? PlayerEmpire => GameManager.Instance.LocalPlayerEmpire;
    public int SelectedFleetId => _primarySelectedFleetId;
    public IReadOnlyCollection<int> SelectedFleetIds => _selectedFleetIds;

    public IReadOnlyDictionary<int, ShipInstanceData> ShipsById => GameManager.Instance.ShipsById;

    internal SettlementSystem? SettlementSystem => _settlementSystem;
    internal StationSystem? StationSystem => _stationSystem;

    /// <summary>Add a new fleet plus its ships, spawn a FleetNode at the fleet's current system,
    /// and refresh the LeftPanel fleet list. Data is owned by GameManager; visuals live here.</summary>
    internal void RegisterFleet(FleetData fleet, IEnumerable<ShipInstanceData> ships, bool isPlayerFleet)
    {
        var gm = GameManager.Instance;
        gm.AddFleetData(fleet, ships);

        var node = new FleetNode();
        _fleetContainer.AddChild(node);
        node.Initialize(fleet, isPlayerFleet);
        var sys = gm.Galaxy?.GetSystem(fleet.CurrentSystemId);
        if (sys != null) node.UpdatePosition(sys.PositionX, sys.PositionZ);
        node.UpdateLabel();
        _fleetNodes[fleet.Id] = node;

        _leftPanel.SetData(gm.Fleets, gm.Ships);
    }

    /// <summary>Register a new colony with the settlement system. Returns false if settlements not yet initialized.</summary>
    internal bool RegisterColony(Colony colony)
    {
        if (_settlementSystem == null) return false;
        _settlementSystem.AddColony(colony);
        return true;
    }

    /// <summary>Register a new station with the station system and add the matching DTO mirror.
    /// Returns false if stations not yet initialized.</summary>
    internal bool RegisterStation(Station station, StationData mirror)
    {
        if (_stationSystem == null) return false;
        _stationSystem.AddStation(station);
        GameManager.Instance.StationDatas.Add(mirror);
        return true;
    }

    /// <summary>UI helper: scout-capable player fleets in this POI's system.</summary>
    public float GetSystemCapability(int poiId, SiteActivity type)
    {
        var player = PlayerEmpire;
        if (player == null || _salvageSystem == null) return 0f;
        var poi = FindPOI(poiId, out int sysId);
        if (poi == null) return 0f;
        return _salvageSystem.ComputeSystemCapability(player.Id, sysId, type, GameManager.Instance.Fleets, GameManager.Instance.ShipsById);
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
