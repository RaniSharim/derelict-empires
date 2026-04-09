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
/// Shows a setup dialog first, then starts the game with fleet management.
/// </summary>
public partial class MainScene : Node3D
{
    private CanvasLayer _uiLayer = null!;
    private GameSetupDialog _setupDialog = null!;
    private GameSetupManager.SetupResult _setupResult = null!;

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
        GD.Print("[MainScene] Starting Derelict Empires...");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.MasterSeed = 42;
            GameManager.Instance.CurrentState = GameState.Setup;
            GameManager.Instance.CurrentSpeed = GameSpeed.Paused;
        }

        // Galaxy map (3D world)
        var galaxyMap = new GalaxyMap { Name = "GalaxyMap" };
        AddChild(galaxyMap);

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
        // Position below the top bar
        _resourceBar.AnchorsPreset = (int)Control.LayoutPreset.TopWide;
        _resourceBar.OffsetTop = 40;

        _resourcePanel = new ResourcePanel { Name = "ResourcePanel" };
        _uiLayer.AddChild(_resourcePanel);

        _systemResourceView = new SystemResourceView { Name = "SystemResourceView" };
        _uiLayer.AddChild(_systemResourceView);

        _colonyPanel = new ColonyPanel { Name = "ColonyPanel" };
        _uiLayer.AddChild(_colonyPanel);

        // Setup dialog
        _setupDialog = new GameSetupDialog { Name = "SetupDialog" };
        _setupDialog.SetupConfirmed += OnSetupConfirmed;
        _uiLayer.AddChild(_setupDialog);

        // Event subscriptions
        EventBus.Instance.FleetSelected += OnFleetSelected;
        EventBus.Instance.FleetDeselected += OnFleetDeselected;
        EventBus.Instance.SystemSelected += OnSystemSelectedForMove;
        EventBus.Instance.FastTick += OnFastTick;
        EventBus.Instance.SlowTick += OnSlowTick;

        GD.Print("[MainScene] Showing setup dialog...");
    }

    private void OnSetupConfirmed(int colorIndex, int originIndex)
    {
        var affinity = (PrecursorColor)colorIndex;
        var origin = (Origin)originIndex;
        GD.Print($"[MainScene] Player chose {affinity} {origin}");

        var gm = GameManager.Instance;
        if (gm?.Galaxy == null) return;

        var rng = new GameRandom(gm.MasterSeed);
        var setupManager = new GameSetupManager();
        _setupResult = new GameSetupManager.SetupResult();

        // Create empires
        setupManager.CreatePlayerEmpire("Player Empire", affinity, origin, gm.Galaxy, _setupResult, rng.DeriveChild("player"));
        for (int i = 0; i < 4; i++)
            setupManager.CreateAIEmpire(gm.Galaxy, _setupResult, rng.DeriveChild(i + 1000));

        gm.Empires = _setupResult.Empires;

        // Log
        foreach (var empire in _setupResult.Empires)
        {
            var home = gm.Galaxy.GetSystem(empire.HomeSystemId);
            GD.Print($"  {empire.Name} | {empire.Affinity} {empire.Origin} | Home: {home?.Name}");
        }
        GD.Print($"  {_setupResult.Fleets.Count} fleets, {_setupResult.Ships.Count} ships");

        // Init movement system
        _movementSystem = new FleetMovementSystem(gm.Galaxy);
        _movementSystem.FleetArrived += (fleet, sysId) =>
        {
            var sys = gm.Galaxy.GetSystem(sysId);
            GD.Print($"[Fleet] {fleet.Name} arrived at {sys?.Name}");
            EventBus.Instance.FireFleetArrivedAtSystem(fleet.Id, sysId);
        };

        // Give fleet info panel the data
        _fleetInfoPanel.SetData(_setupResult.Fleets, _setupResult.Ships);

        // Spawn fleet nodes on map
        SpawnFleetNodes(gm.Galaxy);

        // Init extraction system
        _extractionSystem = new ResourceExtractionSystem();
        _extractionSystem.RegisterGalaxy(gm.Galaxy);

        // Auto-assign extraction at each empire's home system
        int assignmentId = 0;
        foreach (var empire in _setupResult.Empires)
        {
            var homeSys = gm.Galaxy.GetSystem(empire.HomeSystemId);
            if (homeSys == null) continue;

            var assignments = ResourceDistributionHelper.CreateHomeExtractions(
                empire.Id, homeSys, assignmentId);
            foreach (var a in assignments)
                _extractionSystem.AddAssignment(a);
            assignmentId += assignments.Count;
        }

        _extractionSystem.DepositDepleted += (empireId, deposit) =>
            GD.Print($"[Resources] Deposit depleted for empire {empireId}: {deposit.Color} {deposit.Type}");

        GD.Print($"  Extraction assignments: {_extractionSystem.AllAssignments.Count}");

        // Init settlement system
        _settlementSystem = new DerlictEmpires.Core.Settlements.SettlementSystem();
        foreach (var colonyData in _setupResult.Colonies)
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
            // Add starting population as food workers
            colony.PopGroups.Add(new DerlictEmpires.Core.Settlements.PopGroup
            {
                Count = colonyData.Population,
                Allocation = WorkPool.Food
            });
            DerlictEmpires.Core.Settlements.PopAllocationManager.AutoAllocate(colony);

            // Queue a starting building
            var farm = DerlictEmpires.Core.Settlements.BuildingData.FindById("food_farm");
            if (farm != null)
                colony.Queue.Enqueue(new DerlictEmpires.Core.Settlements.BuildingProducible(farm));

            _settlementSystem.AddColony(colony);
        }

        _settlementSystem.BuildingCompleted += (colony, buildingId) =>
            GD.Print($"[Colony] {colony.Name} completed building: {buildingId}");
        _settlementSystem.PopulationGrew += colony =>
            GD.Print($"[Colony] {colony.Name} population grew to {colony.TotalPopulation}");

        // Show player colony panel
        var playerColony = _settlementSystem.Colonies
            .FirstOrDefault(c => c.OwnerEmpireId == (_setupResult.Empires.FirstOrDefault(e => e.IsHuman)?.Id ?? -1));
        if (playerColony != null)
            _colonyPanel.Show(playerColony);

        GD.Print($"  Colonies: {_settlementSystem.Colonies.Count}");

        // Remove dialog, start game
        _setupDialog.QueueFree();
        gm.CurrentState = GameState.Playing;
        gm.CurrentSpeed = GameSpeed.Normal;
        GD.Print("[MainScene] Game started!");
    }

    private void SpawnFleetNodes(GalaxyData galaxy)
    {
        int playerEmpireId = _setupResult.Empires.FirstOrDefault(e => e.IsHuman)?.Id ?? -1;

        foreach (var fleet in _setupResult.Fleets)
        {
            var node = new FleetNode();
            _fleetContainer.AddChild(node);
            node.Initialize(fleet, fleet.OwnerEmpireId == playerEmpireId);

            // Set initial position
            var sys = galaxy.GetSystem(fleet.CurrentSystemId);
            if (sys != null)
                node.UpdatePosition(sys.PositionX, sys.PositionZ);

            node.UpdateLabel();
            _fleetNodes[fleet.Id] = node;
        }
    }

    private void OnFleetSelected(int fleetId)
    {
        // Deselect previous
        if (_selectedFleetId >= 0 && _fleetNodes.TryGetValue(_selectedFleetId, out var prevNode))
            prevNode.SetSelected(false);

        _selectedFleetId = fleetId;
        _selection.SelectFleet(fleetId);

        if (_fleetNodes.TryGetValue(fleetId, out var node))
            node.SetSelected(true);

        // Show path if fleet has an order
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

    /// <summary>
    /// When a system is clicked while a fleet is selected, issue a move order.
    /// </summary>
    private void OnSystemSelectedForMove(StarSystemData targetSystem)
    {
        if (_selectedFleetId < 0 || _movementSystem == null) return;

        var gm = GameManager.Instance;
        if (gm?.Galaxy == null) return;

        var fleet = _setupResult.Fleets.FirstOrDefault(f => f.Id == _selectedFleetId);
        if (fleet == null) return;

        // Only move player fleets
        var playerEmpire = _setupResult.Empires.FirstOrDefault(e => e.IsHuman);
        if (playerEmpire == null || fleet.OwnerEmpireId != playerEmpire.Id) return;

        // Need a source system
        int sourceId = fleet.CurrentSystemId;
        if (sourceId < 0)
        {
            // Fleet is in transit — use the transit destination as source
            var order = _movementSystem.GetOrder(fleet.Id);
            if (order != null && order.NextSystemId >= 0)
                sourceId = order.NextSystemId;
            else
                return;
        }

        if (sourceId == targetSystem.Id) return;

        // Check if hauler can use hidden lanes
        bool canUseHidden = playerEmpire.Origin == Origin.Haulers;

        var path = LanePathfinder.FindPath(gm.Galaxy, sourceId, targetSystem.Id, canUseHidden);
        if (path.Count == 0)
        {
            GD.Print($"[Fleet] No path from system {sourceId} to {targetSystem.Name}");
            return;
        }

        GD.Print($"[Fleet] {fleet.Name} moving to {targetSystem.Name} ({path.Count} hops)");
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
        var fleet = _setupResult.Fleets.FirstOrDefault(f => f.Id == _selectedFleetId);
        if (order == null || fleet == null || order.IsComplete)
        {
            _pathIndicator.Clear();
            return;
        }

        // Show remaining path
        int fromId = order.TransitFromSystemId >= 0 ? order.TransitFromSystemId : fleet.CurrentSystemId;
        var remaining = order.Path.Skip(order.PathIndex).ToList();
        _pathIndicator.ShowPath(gm.Galaxy, fromId, remaining);
    }

    private void OnFastTick(float delta)
    {
        if (_movementSystem == null || _setupResult == null) return;

        _movementSystem.ProcessTick(delta, _setupResult.Fleets);

        // Update fleet node positions
        foreach (var fleet in _setupResult.Fleets)
        {
            if (!_fleetNodes.TryGetValue(fleet.Id, out var node)) continue;
            var (x, z) = _movementSystem.GetFleetPosition(fleet);
            node.UpdatePosition(x, z);
        }

        // Update path indicator if selected fleet is moving
        if (_selectedFleetId >= 0)
            UpdatePathIndicator();
    }

    private void OnSlowTick(float delta)
    {
        if (_extractionSystem == null || _setupResult == null) return;

        _extractionSystem.ProcessTick(delta, _setupResult.Empires);
        _settlementSystem?.ProcessTick(delta);

        // Update income display every few slow ticks
        _incomeUpdateCounter++;
        if (_incomeUpdateCounter % 3 == 0)
        {
            var playerEmpire = _setupResult.Empires.FirstOrDefault(e => e.IsHuman);
            if (playerEmpire != null)
            {
                var income = _extractionSystem.CalculateIncome(playerEmpire.Id, delta);
                _resourceBar.UpdateIncome(income);
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Right-click to deselect fleet
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Right)
        {
            if (_selectedFleetId >= 0)
            {
                EventBus.Instance.FireFleetDeselected();
                GetViewport().SetInputAsHandled();
            }
        }

        // Escape to deselect
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
