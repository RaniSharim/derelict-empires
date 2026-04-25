using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Systems;
using DerlictEmpires.Nodes.Camera;
using DerlictEmpires.Nodes.UI;
using DerlictEmpires.Nodes.Units;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Owns fleet selection state and the path indicator.
/// Translates click/keypress signals from <see cref="EventBus"/> into selection visual state,
/// camera moves, and movement orders. Owns its <see cref="FleetOrderIndicator"/> child so
/// path-rendering is fully encapsulated.
/// </summary>
public partial class SelectionController : Node
{
    private MainScene _main = null!;
    private Dictionary<int, FleetNode> _fleetNodes = null!;
    private StrategyCameraRig _cameraRig = null!;
    private FleetOrderIndicator _pathIndicator = null!;

    private readonly SelectionManager _selection = new();
    private readonly HashSet<int> _selectedFleetIds = new();
    private int _primarySelectedFleetId = -1;  // most recently added — drives path indicator

    public int SelectedFleetId => _primarySelectedFleetId;
    public IReadOnlyCollection<int> SelectedFleetIds => _selectedFleetIds;

    public void Configure(MainScene main, Dictionary<int, FleetNode> fleetNodes, StrategyCameraRig cameraRig)
    {
        _main = main;
        _fleetNodes = fleetNodes;
        _cameraRig = cameraRig;
    }

    public override void _Ready()
    {
        _pathIndicator = new FleetOrderIndicator { Name = "PathIndicator" };
        AddChild(_pathIndicator);

        if (EventBus.Instance == null) return;
        EventBus.Instance.FleetSelected += OnFleetSelected;
        EventBus.Instance.FleetSelectionToggled += OnFleetSelectionToggled;
        EventBus.Instance.FleetDeselected += OnFleetDeselected;
        EventBus.Instance.FleetDoubleClicked += OnFleetDoubleClicked;
        EventBus.Instance.SystemRightClicked += OnSystemRightClickedForMove;
        EventBus.Instance.FleetOrderChanged += OnFleetOrderChanged;
        EventBus.Instance.FastTick += OnFastTick;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance == null) return;
        EventBus.Instance.FleetSelected -= OnFleetSelected;
        EventBus.Instance.FleetSelectionToggled -= OnFleetSelectionToggled;
        EventBus.Instance.FleetDeselected -= OnFleetDeselected;
        EventBus.Instance.FleetDoubleClicked -= OnFleetDoubleClicked;
        EventBus.Instance.SystemRightClicked -= OnSystemRightClickedForMove;
        EventBus.Instance.FleetOrderChanged -= OnFleetOrderChanged;
        EventBus.Instance.FastTick -= OnFastTick;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape }
            && _selectedFleetIds.Count > 0)
        {
            EventBus.Instance?.FireFleetDeselected();
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>Drop all selection state. Called by MainScene during save-game load.</summary>
    public void Reset()
    {
        foreach (int id in _selectedFleetIds)
            if (_fleetNodes.TryGetValue(id, out var node)) node.SetSelected(false);
        _selectedFleetIds.Clear();
        _primarySelectedFleetId = -1;
        _selection.Deselect();
        _pathIndicator.Clear();
    }

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

    /// <summary>Double-click on a fleet pans the camera to the fleet's current world position
    /// (works for both docked and in-transit fleets via the node's own transform).</summary>
    private void OnFleetDoubleClicked(int fleetId)
    {
        if (_fleetNodes.TryGetValue(fleetId, out var node))
            _cameraRig.PanToWorld(node.GlobalPosition);
    }

    private void OnSystemRightClickedForMove(StarSystemData targetSystem)
    {
        var movement = _main.MovementSystem;
        if (_selectedFleetIds.Count == 0 || movement == null) return;

        var gm = GameManager.Instance;
        if (gm.Galaxy == null) return;

        var playerEmpire = gm.LocalPlayerEmpire;
        if (playerEmpire == null) return;

        bool canUseHidden = playerEmpire.Origin == Origin.Haulers;

        foreach (int fleetId in _selectedFleetIds)
        {
            var fleet = gm.Fleets.FirstOrDefault(f => f.Id == fleetId);
            if (fleet == null || fleet.OwnerEmpireId != playerEmpire.Id) continue;

            int sourceId = fleet.CurrentSystemId;
            if (sourceId < 0)
            {
                var order = movement.GetOrder(fleet.Id);
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
            movement.IssueMoveOrder(fleet, path);
            EventBus.Instance?.FireFleetOrderChanged(fleet.Id);
        }
    }

    private void OnFleetOrderChanged(int fleetId)
    {
        if (fleetId == _primarySelectedFleetId)
            UpdatePathIndicator();
    }

    private void OnFastTick(float delta)
    {
        if (_primarySelectedFleetId >= 0)
            UpdatePathIndicator();
    }

    private void UpdatePathIndicator()
    {
        var gm = GameManager.Instance;
        var movement = _main.MovementSystem;
        if (gm.Galaxy == null || movement == null || _primarySelectedFleetId < 0)
        {
            _pathIndicator.Clear();
            return;
        }

        var order = movement.GetOrder(_primarySelectedFleetId);
        var fleet = gm.Fleets.FirstOrDefault(f => f.Id == _primarySelectedFleetId);
        if (order == null || fleet == null || order.IsComplete)
        {
            _pathIndicator.Clear();
            return;
        }

        int fromId = order.TransitFromSystemId >= 0 ? order.TransitFromSystemId : fleet.CurrentSystemId;
        var remaining = order.Path.Skip(order.PathIndex).ToList();
        _pathIndicator.ShowPath(gm.Galaxy, fromId, remaining);
    }
}
