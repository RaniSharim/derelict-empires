using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Services;
using DerlictEmpires.Core.Systems;
using DerlictEmpires.Nodes.UI;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Owns fleet selection state and the path indicator.
/// Translates click/keypress signals from <see cref="EventBus"/> into selection state
/// and movement intent events. Owns its <see cref="FleetOrderIndicator"/> child so
/// path-rendering is fully encapsulated.
///
/// Decoupled from sibling controllers — reads via <see cref="IGameQuery"/> and writes
/// via EventBus intent events (<c>FleetMoveOrderRequested</c>, <c>CameraPanToWorldRequested</c>).
/// </summary>
public partial class SelectionController : Node
{
    private IGameQuery _query = null!;
    private FleetOrderIndicator _pathIndicator = null!;

    private readonly SelectionManager _selection = new();
    private readonly HashSet<int> _selectedFleetIds = new();
    private int _primarySelectedFleetId = -1;  // most recently added — drives path indicator

    public int SelectedFleetId => _primarySelectedFleetId;
    public IReadOnlyCollection<int> SelectedFleetIds => _selectedFleetIds;

    public void Configure(IGameQuery query) => _query = query;

    public override void _Ready()
    {
        _pathIndicator = new FleetOrderIndicator { Name = "PathIndicator" };
        AddChild(_pathIndicator);

        if (EventBus.Instance == null) return;
        EventBus.Instance.FleetSelected += OnFleetSelected;
        EventBus.Instance.FleetSelectionToggled += OnFleetSelectionToggled;
        EventBus.Instance.FleetDeselected += OnFleetDeselected;
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
        _selectedFleetIds.Clear();
        _primarySelectedFleetId = -1;
        _selection.Deselect();
        _pathIndicator.Clear();
    }

    /// <summary>Replace selection with a single fleet.</summary>
    private void OnFleetSelected(int fleetId)
    {
        _selectedFleetIds.Clear();
        _selectedFleetIds.Add(fleetId);
        _primarySelectedFleetId = fleetId;
        _selection.SelectFleet(fleetId);
        UpdatePathIndicator();
    }

    /// <summary>Ctrl-click: add/remove the fleet from the current selection.</summary>
    private void OnFleetSelectionToggled(int fleetId)
    {
        if (_selectedFleetIds.Remove(fleetId))
        {
            if (_primarySelectedFleetId == fleetId)
                _primarySelectedFleetId = _selectedFleetIds.Count > 0 ? _selectedFleetIds.First() : -1;
        }
        else
        {
            _selectedFleetIds.Add(fleetId);
            _primarySelectedFleetId = fleetId;
        }
        _selection.SelectFleet(_primarySelectedFleetId);
        UpdatePathIndicator();
    }

    private void OnFleetDeselected()
    {
        _selectedFleetIds.Clear();
        _primarySelectedFleetId = -1;
        _selection.Deselect();
        _pathIndicator.Clear();
    }

    private void OnSystemRightClickedForMove(StarSystemData targetSystem)
    {
        if (_selectedFleetIds.Count == 0) return;
        foreach (int fleetId in _selectedFleetIds)
            EventBus.Instance?.FireFleetMoveOrderRequested(fleetId, targetSystem.Id);
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
        var galaxy = _query.Galaxy;
        if (galaxy == null || _primarySelectedFleetId < 0)
        {
            _pathIndicator.Clear();
            return;
        }

        var order = _query.GetFleetOrder(_primarySelectedFleetId);
        var fleet = _query.Fleets.FirstOrDefault(f => f.Id == _primarySelectedFleetId);
        if (order == null || fleet == null || order.IsComplete)
        {
            _pathIndicator.Clear();
            return;
        }

        int fromId = order.TransitFromSystemId >= 0 ? order.TransitFromSystemId : fleet.CurrentSystemId;
        var remaining = order.Path.Skip(order.PathIndex).ToList();
        _pathIndicator.ShowPath(galaxy, fromId, remaining);
    }
}
