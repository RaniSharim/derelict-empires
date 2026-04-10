using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;
using DerlictEmpires.Core.Systems;
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
    private FleetOrderIndicator _pathIndicator = null!;
    private int _selectedFleetId = -1;

    // Resource extraction
    private ResourceExtractionSystem? _extractionSystem;
    private ResourceBar _resourceBar = null!;
    private ResourcePanel _resourcePanel = null!;
    private SystemResourceView _systemResourceView = null!;
    private int _incomeUpdateCounter;

    // Settlements
    private DerlictEmpires.Core.Settlements.SettlementSystem? _settlementSystem;
    private ColonyPanel _colonyPanel = null!;

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

        var topBar = new TopBar { Name = "TopBar" };
        _uiLayer.AddChild(topBar);

        var tooltip = new SystemTooltip { Name = "SystemTooltip" };
        _uiLayer.AddChild(tooltip);

        _fleetInfoPanel = new FleetInfoPanel { Name = "FleetInfoPanel" };
        _uiLayer.AddChild(_fleetInfoPanel);

        _resourceBar = new ResourceBar { Name = "ResourceBar" };
        _uiLayer.AddChild(_resourceBar);
        _resourceBar.AnchorsPreset = (int)Control.LayoutPreset.TopWide;
        _resourceBar.OffsetTop = 40;

        _resourcePanel = new ResourcePanel { Name = "ResourcePanel" };
        _uiLayer.AddChild(_resourcePanel);

        _systemResourceView = new SystemResourceView { Name = "SystemResourceView" };
        _uiLayer.AddChild(_systemResourceView);

        _colonyPanel = new ColonyPanel { Name = "ColonyPanel" };
        _uiLayer.AddChild(_colonyPanel);

        // Setup dialog (new game flow)
        _setupDialog = new GameSetupDialog { Name = "SetupDialog" };
        _setupDialog.SetupConfirmed += OnSetupConfirmed;
        _uiLayer.AddChild(_setupDialog);

        // Event subscriptions
        EventBus.Instance.FleetSelected += OnFleetSelected;
        EventBus.Instance.FleetDeselected += OnFleetDeselected;
        EventBus.Instance.SystemSelected += OnSystemSelectedForMove;
        EventBus.Instance.FastTick += OnFastTick;
        EventBus.Instance.SlowTick += OnSlowTick;

        McpLog.Info("[MainScene] Showing setup dialog...");
    }

    // ── New Game Path ────────────────────────────────────────────

    private void OnSetupConfirmed(int colorIndex, int originIndex)
    {
        var affinity = (PrecursorColor)colorIndex;
        var origin = (Origin)originIndex;
        McpLog.Info($"[MainScene] Player chose {affinity} {origin}");

        var gm = GameManager.Instance;
        if (gm == null) return;

        // Set seed and generate galaxy
        gm.MasterSeed = 42;
        var config = new GalaxyGenerationConfig
        {
            Seed = gm.MasterSeed,
            TotalSystems = 100,
            ArmCount = 4,
            GalaxyRadius = 200f,
            MaxLaneLength = 60f,
            MinNeighbors = 2,
            MaxNeighbors = 4,
            HiddenLaneRatio = 0.15f
        };
        var galaxy = GalaxyGenerator.Generate(config);
        gm.Galaxy = galaxy;

        // Render galaxy
        _galaxyMap.LoadGalaxy(galaxy);

        var rng = new GameRandom(gm.MasterSeed);
        var setupManager = new GameSetupManager();
        var setupResult = new GameSetupManager.SetupResult();

        // Create empires
        setupManager.CreatePlayerEmpire("Player Empire", affinity, origin, galaxy, setupResult, rng.DeriveChild("player"));
        for (int i = 0; i < 4; i++)
            setupManager.CreateAIEmpire(galaxy, setupResult, rng.DeriveChild(i + 1000));

        // Store data
        _empires = setupResult.Empires;
        _fleets = setupResult.Fleets;
        _ships = setupResult.Ships;
        _colonyDatas = setupResult.Colonies;
        _stationDatas = setupResult.Stations;
        gm.Empires = _empires;

        // Log
        foreach (var empire in _empires)
        {
            var home = galaxy.GetSystem(empire.HomeSystemId);
            McpLog.Info($"  {empire.Name} | {empire.Affinity} {empire.Origin} | Home: {home?.Name}");
        }
        McpLog.Info($"  {_fleets.Count} fleets, {_ships.Count} ships");

        // Init systems
        InitGameSystems(galaxy);
        InitExtractions(galaxy);
        InitSettlements();

        // Remove dialog, start game
        _setupDialog?.QueueFree();
        _setupDialog = null;
        gm.CurrentState = GameState.Playing;
        gm.CurrentSpeed = GameSpeed.Normal;
        McpLog.Info("[MainScene] Game started!");
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
        _selectedFleetId = -1;

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

        // Start game
        gm.CurrentState = GameState.Playing;
        gm.CurrentSpeed = saveData.GameSpeed;
        McpLog.Info($"[MainScene] Game loaded! {_empires.Count} empires, {_fleets.Count} fleets, {_colonyDatas.Count} colonies");
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
            Stations = _stationDatas,
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
        };

        _fleetInfoPanel.SetData(_fleets, _ships);
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

        var playerColony = _settlementSystem.Colonies
            .FirstOrDefault(c => c.OwnerEmpireId == (_empires.FirstOrDefault(e => e.IsHuman)?.Id ?? -1));
        if (playerColony != null)
            _colonyPanel.Show(playerColony);

        McpLog.Info($"  Colonies: {_settlementSystem.Colonies.Count}");
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

    private void OnFleetSelected(int fleetId)
    {
        if (_selectedFleetId >= 0 && _fleetNodes.TryGetValue(_selectedFleetId, out var prevNode))
            prevNode.SetSelected(false);

        _selectedFleetId = fleetId;
        _selection.SelectFleet(fleetId);

        if (_fleetNodes.TryGetValue(fleetId, out var node))
            node.SetSelected(true);

        UpdatePathIndicator();
    }

    private void OnFleetDeselected()
    {
        if (_selectedFleetId >= 0 && _fleetNodes.TryGetValue(_selectedFleetId, out var node))
            node.SetSelected(false);

        _selectedFleetId = -1;
        _selection.Deselect();
        _pathIndicator.Clear();
    }

    private void OnSystemSelectedForMove(StarSystemData targetSystem)
    {
        if (_selectedFleetId < 0 || _movementSystem == null) return;

        var gm = GameManager.Instance;
        if (gm?.Galaxy == null) return;

        var fleet = _fleets.FirstOrDefault(f => f.Id == _selectedFleetId);
        if (fleet == null) return;

        var playerEmpire = _empires.FirstOrDefault(e => e.IsHuman);
        if (playerEmpire == null || fleet.OwnerEmpireId != playerEmpire.Id) return;

        int sourceId = fleet.CurrentSystemId;
        if (sourceId < 0)
        {
            var order = _movementSystem.GetOrder(fleet.Id);
            if (order != null && order.NextSystemId >= 0)
                sourceId = order.NextSystemId;
            else
                return;
        }

        if (sourceId == targetSystem.Id) return;

        bool canUseHidden = playerEmpire.Origin == Origin.Haulers;
        var path = LanePathfinder.FindPath(gm.Galaxy, sourceId, targetSystem.Id, canUseHidden);
        if (path.Count == 0)
        {
            McpLog.Info($"[Fleet] No path from system {sourceId} to {targetSystem.Name}");
            return;
        }

        McpLog.Info($"[Fleet] {fleet.Name} moving to {targetSystem.Name} ({path.Count} hops)");
        _movementSystem.IssueMoveOrder(fleet, path);
        UpdatePathIndicator();
    }

    private void UpdatePathIndicator()
    {
        var gm = GameManager.Instance;
        if (gm?.Galaxy == null || _movementSystem == null || _selectedFleetId < 0)
        {
            _pathIndicator.Clear();
            return;
        }

        var order = _movementSystem.GetOrder(_selectedFleetId);
        var fleet = _fleets.FirstOrDefault(f => f.Id == _selectedFleetId);
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

        foreach (var fleet in _fleets)
        {
            if (!_fleetNodes.TryGetValue(fleet.Id, out var node)) continue;
            var (x, z) = _movementSystem.GetFleetPosition(fleet);
            node.UpdatePosition(x, z);
        }

        if (_selectedFleetId >= 0)
            UpdatePathIndicator();
    }

    private void OnSlowTick(float delta)
    {
        _extractionSystem?.ProcessTick(delta, _empires);
        _settlementSystem?.ProcessTick(delta);

        _incomeUpdateCounter++;
        if (_incomeUpdateCounter % 3 == 0)
        {
            var playerEmpire = _empires.FirstOrDefault(e => e.IsHuman);
            if (playerEmpire != null && _extractionSystem != null)
            {
                var income = _extractionSystem.CalculateIncome(playerEmpire.Id, delta);
                _resourceBar.UpdateIncome(income);
            }
        }
    }

    // ── Input ────────────────────────────────────────────────────

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Right)
        {
            if (_selectedFleetId >= 0)
            {
                EventBus.Instance.FireFleetDeselected();
                GetViewport().SetInputAsHandled();
            }
        }

        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
        {
            if (_selectedFleetId >= 0)
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
            EventBus.Instance.FleetDeselected -= OnFleetDeselected;
            EventBus.Instance.SystemSelected -= OnSystemSelectedForMove;
            EventBus.Instance.FastTick -= OnFastTick;
            EventBus.Instance.SlowTick -= OnSlowTick;
        }
    }
}
