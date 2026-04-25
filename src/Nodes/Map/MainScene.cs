using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core;
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
///
/// All game-logic systems live on <see cref="Systems"/> (a <see cref="GameSystems"/>
/// composition root hosted by <see cref="GameSystemsHost"/>). MainScene owns scene-graph
/// wiring only.
/// </summary>
public partial class MainScene : Node3D
{
    private CanvasLayer _uiLayer = null!;
    private GalaxyMap _galaxyMap = null!;
    private StrategyCameraRig _cameraRig = null!;

    // Game data lives on GameManager. Read via GameManager.Instance.{Empires|Fleets|Ships|Colonies|StationDatas|EmpiresById|ShipsById}.
    // Game-logic systems live on _systemsHost.Systems. Read via the Systems property.
    private GameSystemsHost _systemsHost = null!;

    /// <summary>Composition root for all game-logic systems. UI/controllers read here
    /// instead of holding direct system references.</summary>
    public GameSystems Systems => _systemsHost.Systems;

    // Fleet visuals — owns the FleetNode container and per-tick position updates.
    private FleetVisualController _fleetVisuals = null!;

    // UI panel refs (the panels themselves get rebuilt in refactor-2-ui).
    private LeftPanel _leftPanel = null!;
    private TopBar _topBar = null!;

    // Sibling controllers.
    private DevHarness _devHarness = null!;
    private OverlayRouter _overlayRouter = null!;
    private CombatRouter _combatRouter = null!;
    private SelectionController _selectionController = null!;

    public override void _Ready()
    {
        McpLog.Info("[MainScene] Starting Derelict Empires...");

        // Project-wide Theme — applied before any UI Control is instantiated so
        // children inherit defaults (StyleBoxes, colors, sizes) without per-control overrides.
        // If the .tres is missing on disk, write it on first run so the editor can preview scenes.
        if (!ResourceLoader.Exists(ThemeBuilder.ThemeResourcePath))
            ThemeBuilder.SaveToDisk();
        ThemeBuilder.Apply(GetTree());

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CurrentState = GameState.Setup;
            GameManager.Instance.CurrentSpeed = GameSpeed.Paused;
        }

        // Galaxy map (3D world) — no longer auto-generates
        _galaxyMap = new GalaxyMap { Name = "GalaxyMap" };
        AddChild(_galaxyMap);

        // Game-systems host — must enter the tree before MainScene's FastTick
        // subscriber so movement processes positions before MainScene reads them.
        _systemsHost = new GameSystemsHost { Name = "GameSystems" };
        AddChild(_systemsHost);

        // Fleet visuals — owns the per-fleet Node3D children and per-tick position update.
        _fleetVisuals = new FleetVisualController { Name = "Fleets" };
        _fleetVisuals.Configure(_systemsHost.Systems);
        AddChild(_fleetVisuals);

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

        _topBar = GD.Load<PackedScene>("res://scenes/ui/top_bar.tscn").Instantiate<TopBar>();
        _topBar.Name = "TopBar";
        _uiLayer.AddChild(_topBar);

        // New UI panels
        _leftPanel = GD.Load<PackedScene>("res://scenes/ui/left_panel.tscn").Instantiate<LeftPanel>();
        _leftPanel.Name = "LeftPanel";
        _uiLayer.AddChild(_leftPanel);
        _leftPanel.SetMainScene(this);

        var rightPanel = GD.Load<PackedScene>("res://scenes/ui/right_panel.tscn").Instantiate<RightPanel>();
        rightPanel.Name = "RightPanel";
        _uiLayer.AddChild(rightPanel);
        rightPanel.SetMainScene(this);

        var speedWidget = GD.Load<PackedScene>("res://scenes/ui/speed_time_widget.tscn").Instantiate<SpeedTimeWidget>();
        speedWidget.Name = "SpeedTimeWidget";
        _uiLayer.AddChild(speedWidget);

        var eventLog = GD.Load<PackedScene>("res://scenes/ui/event_log.tscn").Instantiate<EventLog>();
        eventLog.Name = "EventLog";
        _uiLayer.AddChild(eventLog);

        var minimap = GD.Load<PackedScene>("res://scenes/ui/minimap.tscn").Instantiate<Minimap>();
        minimap.Name = "Minimap";
        _uiLayer.AddChild(minimap);

        // Dev harness — debug input (F7/F10/F11/F12/Shift+B) and seed helpers.
        _devHarness = new DevHarness { Name = "DevHarness" };
        AddChild(_devHarness);
        _devHarness.Configure(this);

        // Overlay router — tech tree / designer / system view open requests.
        _overlayRouter = new OverlayRouter { Name = "OverlayRouter" };
        _overlayRouter.Configure(this, _uiLayer);
        AddChild(_overlayRouter);

        // Combat router — BattleManager + popup + system markers.
        _combatRouter = new CombatRouter { Name = "CombatRouter" };
        _combatRouter.Configure(_uiLayer, _cameraRig);
        AddChild(_combatRouter);

        // Selection controller — fleet selection, path indicator, right-click move.
        _selectionController = new SelectionController { Name = "SelectionController" };
        _selectionController.Configure(this, _fleetVisuals, _cameraRig);
        AddChild(_selectionController);

        // Salvage action handler — converts EventBus.ScanToggleRequested / ExtractToggleRequested
        // intent events into GameSystems.Salvage calls. UI fires the events; this handler validates.
        var salvageHandler = new SalvageActionHandler { Name = "SalvageActionHandler" };
        salvageHandler.Configure(_systemsHost.Systems);
        AddChild(salvageHandler);

        _topBar.ResearchStrip.Configure(GameManager.Instance!);

        // MainScene only owns the top-bar income recompute (slow tick + salvage events).
        // Fleet visuals tick is owned by FleetVisualController; movement+salvage by GameSystemsHost.
        EventBus.Instance.SlowTick += OnSlowTick;
        EventBus.Instance.SiteActivityChanged += OnSiteActivityChangedForTopBar;
        EventBus.Instance.SiteActivityRateChanged += OnSiteActivityRateChangedForTopBar;

        McpLog.Info("[MainScene] Auto-starting MVP salvage loop...");
        CallDeferred(nameof(StartMvpGame));
    }

    private void StartMvpGame()
    {
        OnSetupConfirmed((int)PrecursorColor.Red, (int)Origin.Servitors);
        _devHarness.GrantShipSubsystems();   // Ship modules across all tiers for UI work
        _devHarness.SeedHomeColony();        // a starting colony at home for System View verification
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

        LoadAllSystems(galaxy, player, rng.DeriveChild("research"));

        gm.CurrentState = GameState.Playing;
        gm.CurrentSpeed = GameSpeed.Normal;
        McpLog.Info("[MainScene] MVP loop running!");
    }

    // ── Load Game Path ───────────────────────────────────────────

    /// <summary>
    /// Load a complete game state from a GameSaveData.
    /// Called by McpBridge's load_state command or future save/load UI.
    /// </summary>
    public void LoadGame(GameSaveData saveData)
    {
        McpLog.Info($"[MainScene] Loading game state (v{saveData.Version}, seed={saveData.MasterSeed})...");

        _fleetVisuals.ClearAll();
        _selectionController.Reset();

        var gm = GameManager.Instance;
        if (gm == null) return;

        gm.MasterSeed = saveData.MasterSeed;
        gm.GameTime = saveData.GameTime;
        gm.Galaxy = saveData.Galaxy;
        gm.LoadState(
            saveData.Empires,
            saveData.Fleets,
            saveData.Ships,
            saveData.Colonies,
            saveData.Stations);

        _galaxyMap.LoadGalaxy(saveData.Galaxy);

        var player = gm.Empires.First();
        var rng = new GameRandom(saveData.MasterSeed);
        LoadAllSystems(saveData.Galaxy, player, rng);

        // Restore fleet orders.
        foreach (var orderData in saveData.FleetOrders)
        {
            var fleet = gm.Fleets.FirstOrDefault(f => f.Id == orderData.FleetId);
            if (fleet == null) continue;

            var order = new FleetOrder
            {
                Type = orderData.Type,
                Path = orderData.Path,
                PathIndex = orderData.PathIndex,
                LaneProgress = orderData.LaneProgress,
                TransitFromSystemId = orderData.TransitFromSystemId
            };
            Systems.Movement.RestoreOrder(fleet, order);
        }

        // Restore extraction assignments.
        foreach (var extraction in saveData.Extractions)
            Systems.Extraction.AddAssignment(extraction);

        // Restore research states.
        foreach (var rs in saveData.ResearchStates)
            Systems.SetResearchState(rs.EmpireId, StateConverter.ToResearchState(rs));

        gm.CurrentState = GameState.Playing;
        gm.CurrentSpeed = saveData.GameSpeed;
        McpLog.Info($"[MainScene] Game loaded! {gm.Empires.Count} empires, {gm.Fleets.Count} fleets, {gm.Colonies.Count} colonies, {Systems.Stations.Stations.Count} stations");
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
            Stations = Systems.Stations.Stations.Select(StateConverter.ToStationData).ToList(),
            Extractions = Systems.Extraction.AllAssignments.ToList(),
        };

        // Save fleet orders.
        foreach (var fleet in gm.Fleets)
        {
            var order = Systems.Movement.GetOrder(fleet.Id);
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

        foreach (var kvp in Systems.ResearchStates)
            saveData.ResearchStates.Add(StateConverter.ToResearchSaveData(kvp.Value));

        return saveData;
    }

    // ── Shared Init ──────────────────────────────────────────────

    /// <summary>
    /// Construct every game-logic system in the order required by their dependencies:
    /// Movement → Exploration → Salvage (needs Exploration) → Extraction → Settlements →
    /// Stations → Research. Then register cross-system orchestration and spawn fleet visuals.
    /// </summary>
    private void LoadAllSystems(GalaxyData galaxy, EmpireData player, GameRandom researchRng)
    {
        Systems.LoadMovement(galaxy);
        Systems.LoadExploration();
        Systems.LoadSalvage(galaxy, player);
        Systems.LoadExtraction(galaxy);
        Systems.LoadSettlements(GameManager.Instance.Colonies);
        Systems.LoadStations(GameManager.Instance.StationDatas);
        Systems.LoadResearch(GameManager.Instance.Empires, researchRng);

        // Cross-system orchestration on arrival/departure: discover new POIs and refresh
        // per-system salvage capability. EventBus re-emission is handled by GameSystemsHost.
        Systems.FleetArrived += (fleet, sysId) =>
        {
            var sys = galaxy.GetSystem(sysId);
            if (sys != null)
            {
                var poiIds = sys.POIs.Select(p => p.Id).ToList();
                Systems.Exploration.DiscoverSystem(fleet.OwnerEmpireId, sysId, poiIds);
            }
            Systems.Salvage.NotifyFleetMovedSystem(fleet.OwnerEmpireId, sysId);
        };
        Systems.FleetDeparted += (fleet, fromSysId) =>
            Systems.Salvage.NotifyFleetMovedSystem(fleet.OwnerEmpireId, fromSysId);

        _leftPanel.SetData(GameManager.Instance.Fleets, GameManager.Instance.Ships);

        int playerEmpireId = GameManager.Instance.Empires.FirstOrDefault(e => e.IsHuman)?.Id ?? -1;
        _fleetVisuals.SpawnAll(galaxy, GameManager.Instance.Fleets, playerEmpireId);

        McpLog.Info($"  Colonies: {Systems.Settlements.Colonies.Count}");
        McpLog.Info($"  Stations: {Systems.Stations.Stations.Count}");
        McpLog.Info($"  Research states: {Systems.ResearchStates.Count} ({Systems.TechRegistry.Nodes.Count} tech nodes)");
    }

    // ── Tick Processing ──────────────────────────────────────────

    private void OnSlowTick(float delta) => UpdateTopBarDelta();

    private void OnSiteActivityChangedForTopBar(int empireId, int poiId, SiteActivity activity) =>
        UpdateTopBarDelta();

    private void OnSiteActivityRateChangedForTopBar(int empireId, int poiId) =>
        UpdateTopBarDelta();

    /// <summary>
    /// Compute expected per-second resource income from active Extracting activities
    /// and push to the top-bar so +X deltas update in real time. Refactor-2-ui will
    /// move this onto the rebuilt TopBar so it self-subscribes.
    /// </summary>
    private void UpdateTopBarDelta()
    {
        var player = PlayerEmpire;
        var salvage = Systems.Salvage;
        if (player == null || salvage == null) { _topBar.UpdateIncome(new()); return; }

        var galaxy = GameManager.Instance.Galaxy;
        var income = new Dictionary<string, float>();
        foreach (var kv in salvage.AllActivities)
        {
            if (kv.Value.Activity != SiteActivity.Extracting) continue;
            var poi = Systems.FindPOI(galaxy, kv.Key.poiId, out int sysId);
            if (poi == null || poi.SalvageSiteId is not int siteId) continue;
            var site = Systems.GetSalvageSite(galaxy, siteId);
            if (site == null) continue;

            float cap = salvage.ComputeSystemCapability(
                player.Id, sysId, SiteActivity.Extracting, GameManager.Instance.Fleets, GameManager.Instance.ShipsById);
            int n = salvage.CountActiveInSystem(player.Id, sysId, SiteActivity.Extracting);
            if (n == 0 || cap <= 0f) continue;
            float perSite = cap / n;

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
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;

        if (key.Keycode == Key.T)
        {
            var color = GameManager.Instance.LocalPlayerEmpire?.Affinity ?? PrecursorColor.Red;
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

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.SlowTick -= OnSlowTick;
            EventBus.Instance.SiteActivityChanged -= OnSiteActivityChangedForTopBar;
            EventBus.Instance.SiteActivityRateChanged -= OnSiteActivityRateChangedForTopBar;
        }
    }

    // ── Public accessors (forwards into Systems / GameManager) ───
    // These survive Phase 2 to keep existing UI panels working. Refactor-2-ui replaces
    // them with IGameQuery + EventBus intent events.

    public SalvageSystem? SalvageSystem => _systemsHost?.Systems.Salvage;
    public ExplorationManager? ExplorationManager => _systemsHost?.Systems.Exploration;
    public FleetMovementSystem? MovementSystem => _systemsHost?.Systems.Movement;
    public TechTreeRegistry? TechRegistry => _systemsHost?.Systems.TechRegistry;
    public ResearchEngine? ResearchEngine => _systemsHost?.Systems.Research;

    public IReadOnlyList<FleetData> Fleets => GameManager.Instance.Fleets;
    public EmpireData? PlayerEmpire => GameManager.Instance.LocalPlayerEmpire;
    public IReadOnlyDictionary<int, ShipInstanceData> ShipsById => GameManager.Instance.ShipsById;

    public int SelectedFleetId => _selectionController.SelectedFleetId;
    public IReadOnlyCollection<int> SelectedFleetIds => _selectionController.SelectedFleetIds;

    public EmpireResearchState? PlayerResearchState
    {
        get
        {
            var playerId = GameManager.Instance?.LocalPlayerEmpire?.Id ?? -1;
            return playerId >= 0 ? Systems.GetResearchState(playerId) : null;
        }
    }

    internal SettlementSystem? SettlementSystem => _systemsHost?.Systems.Settlements;
    internal StationSystem? StationSystem => _systemsHost?.Systems.Stations;

    /// <summary>Add a new fleet plus its ships, spawn a FleetNode at the fleet's current system,
    /// and refresh the LeftPanel fleet list. Data is owned by GameManager; visuals live in
    /// <see cref="FleetVisualController"/>.</summary>
    internal void RegisterFleet(FleetData fleet, IEnumerable<ShipInstanceData> ships, bool isPlayerFleet)
    {
        var gm = GameManager.Instance;
        gm.AddFleetData(fleet, ships);
        _fleetVisuals.AddFleetVisual(fleet, isPlayerFleet);
        _leftPanel.SetData(gm.Fleets, gm.Ships);
    }

    /// <summary>Register a new colony with the settlement system. Returns false if settlements not yet initialized.</summary>
    internal bool RegisterColony(Colony colony) => Systems.AddColony(colony);

    /// <summary>Register a new station with the station system and add the matching DTO mirror.
    /// Returns false if stations not yet initialized.</summary>
    internal bool RegisterStation(Station station, StationData mirror)
    {
        if (!Systems.AddStation(station)) return false;
        GameManager.Instance.StationDatas.Add(mirror);
        return true;
    }

    /// <summary>UI helper: scout-capable player fleets in this POI's system.</summary>
    public float GetSystemCapability(int poiId, SiteActivity type)
    {
        var player = PlayerEmpire;
        if (player == null) return 0f;
        return Systems.GetSystemCapability(
            GameManager.Instance.Galaxy, player.Id, poiId, type,
            GameManager.Instance.Fleets, GameManager.Instance.ShipsById);
    }

    public int GetSystemActiveCount(int poiId, SiteActivity type)
    {
        var player = PlayerEmpire;
        if (player == null) return 0;
        return Systems.GetSystemActiveCount(GameManager.Instance.Galaxy, player.Id, poiId, type);
    }

    /// <summary>Salvage-site lookup for the UI.</summary>
    public SalvageSiteData? GetSalvageSite(int siteId) =>
        GameManager.Instance?.Galaxy?.GetSalvageSite(siteId);
}
