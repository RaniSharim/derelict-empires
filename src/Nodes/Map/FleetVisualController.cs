using System.Collections.Generic;
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
        EventBus.Instance.FastTick += OnFastTick;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.FastTick -= OnFastTick;
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
    }

    private void OnFastTick(float delta)
    {
        var movement = _systems?.Movement;
        if (movement == null) return;

        foreach (var fleet in GameManager.Instance.Fleets)
        {
            if (!_nodes.TryGetValue(fleet.Id, out var node)) continue;
            var (x, z) = movement.GetFleetPosition(fleet);
            node.UpdatePosition(x, z);
        }
    }
}
