using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Nodes.Units;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Owns the scene-graph representation of fleets: the parent Node3D, the per-fleet
/// <see cref="FleetNode"/> visuals, and the per-fast-tick position update loop.
///
/// Reads fleet positions from <see cref="GameSystems.Movement"/> and the fleet list
/// from <see cref="GameManager.Instance"/>. Holds no game-logic state of its own —
/// this is a pure visual layer that subscribes to ticks and renders.
/// </summary>
public partial class FleetVisualController : Node3D
{
    private GameSystems _systems = null!;
    private readonly Dictionary<int, FleetNode> _nodes = new();

    /// <summary>Look up the visual node for a fleet, or null if it has no visual.</summary>
    public FleetNode? GetNode(int fleetId) => _nodes.GetValueOrDefault(fleetId);

    public IReadOnlyDictionary<int, FleetNode> Nodes => _nodes;

    public void Configure(GameSystems systems) => _systems = systems;

    public override void _Ready()
    {
        var bus = EventBus.Instance;
        if (bus == null) return;
        bus.FastTick += OnFastTick;
        bus.FleetSelected += OnFleetSelected;
        bus.FleetSelectionToggled += OnFleetSelectionToggled;
        bus.FleetDeselected += OnFleetDeselected;
        bus.FleetDoubleClicked += OnFleetDoubleClicked;
    }

    public override void _ExitTree()
    {
        var bus = EventBus.Instance;
        if (bus == null) return;
        bus.FastTick -= OnFastTick;
        bus.FleetSelected -= OnFleetSelected;
        bus.FleetSelectionToggled -= OnFleetSelectionToggled;
        bus.FleetDeselected -= OnFleetDeselected;
        bus.FleetDoubleClicked -= OnFleetDoubleClicked;
    }

    // === Selection visuals (owned here so SelectionController stays decoupled) ===
    private readonly HashSet<int> _selectedIds = new();

    private void OnFleetSelected(int fleetId)
    {
        foreach (int prev in _selectedIds)
            _nodes.GetValueOrDefault(prev)?.SetSelected(false);
        _selectedIds.Clear();
        _selectedIds.Add(fleetId);
        _nodes.GetValueOrDefault(fleetId)?.SetSelected(true);
    }

    private void OnFleetSelectionToggled(int fleetId)
    {
        if (_selectedIds.Remove(fleetId))
            _nodes.GetValueOrDefault(fleetId)?.SetSelected(false);
        else
        {
            _selectedIds.Add(fleetId);
            _nodes.GetValueOrDefault(fleetId)?.SetSelected(true);
        }
    }

    private void OnFleetDeselected()
    {
        foreach (int id in _selectedIds)
            _nodes.GetValueOrDefault(id)?.SetSelected(false);
        _selectedIds.Clear();
    }

    private void OnFleetDoubleClicked(int fleetId)
    {
        var node = _nodes.GetValueOrDefault(fleetId);
        if (node != null)
            EventBus.Instance?.FireCameraPanToWorldRequested(node.GlobalPosition);
    }

    /// <summary>Spawn FleetNodes for every fleet in <paramref name="fleets"/>.</summary>
    public void SpawnAll(GalaxyData galaxy, IReadOnlyList<FleetData> fleets, int playerEmpireId)
    {
        foreach (var fleet in fleets)
            AddFleetVisual(fleet, fleet.OwnerEmpireId == playerEmpireId, galaxy);
    }

    /// <summary>Spawn a single FleetNode and place it at the fleet's current system.</summary>
    public void AddFleetVisual(FleetData fleet, bool isPlayerFleet, GalaxyData? galaxy = null)
    {
        var node = new FleetNode();
        AddChild(node);
        node.Initialize(fleet, isPlayerFleet);
        var sys = (galaxy ?? GameManager.Instance?.Galaxy)?.GetSystem(fleet.CurrentSystemId);
        if (sys != null) node.UpdatePosition(sys.PositionX, sys.PositionZ);
        node.UpdateLabel();
        _nodes[fleet.Id] = node;
    }

    /// <summary>Tear down every FleetNode. Used by save-game load before respawning.</summary>
    public void ClearAll()
    {
        foreach (var n in _nodes.Values)
            n.QueueFree();
        _nodes.Clear();
        _selectedIds.Clear();
    }

    private void OnFastTick(float delta)
    {
        var movement = _systems?.Movement;
        var gm = GameManager.Instance;
        if (movement == null || gm == null) return;

        // Iterate visuals we own, not the live Fleets list — avoids enumeration
        // mutations if movement adds/removes fleets during the tick.
        foreach (var (fleetId, node) in _nodes)
        {
            var fleet = gm.Fleets.FirstOrDefault(f => f.Id == fleetId);
            if (fleet == null) continue;
            var (x, z) = movement.GetFleetPosition(fleet);
            node.UpdatePosition(x, z);
        }
    }
}
