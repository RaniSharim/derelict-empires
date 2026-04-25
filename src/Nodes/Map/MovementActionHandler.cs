using System.Linq;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Receives <c>EventBus.FleetMoveOrderRequested</c> intent events, computes a lane path,
/// and forwards the order to <see cref="GameSystems.Movement"/>. The single seam between
/// UI/SelectionController click intents and the movement system — keeps SelectionController
/// free of any system reference.
/// </summary>
public partial class MovementActionHandler : Node
{
    private GameSystems _systems = null!;

    public void Configure(GameSystems systems) => _systems = systems;

    public override void _Ready()
    {
        EventBus.Instance.FleetMoveOrderRequested += OnMoveRequested;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance == null) return;
        EventBus.Instance.FleetMoveOrderRequested -= OnMoveRequested;
    }

    private void OnMoveRequested(int fleetId, int targetSystemId)
    {
        var movement = _systems?.Movement;
        if (movement == null) { McpLog.Warn("[Move] rejected: movement system not ready"); return; }

        var gm = GameManager.Instance;
        if (gm?.Galaxy == null) return;

        var player = gm.LocalPlayerEmpire;
        if (player == null) return;

        var fleet = gm.Fleets.FirstOrDefault(f => f.Id == fleetId);
        if (fleet == null || fleet.OwnerEmpireId != player.Id) return;

        int sourceId = fleet.CurrentSystemId;
        if (sourceId < 0)
        {
            var existing = movement.GetOrder(fleet.Id);
            if (existing != null && existing.NextSystemId >= 0) sourceId = existing.NextSystemId;
            else return;
        }
        if (sourceId == targetSystemId) return;

        bool canUseHidden = player.Origin == Origin.Haulers;
        var path = LanePathfinder.FindPath(gm.Galaxy, sourceId, targetSystemId, canUseHidden);
        if (path.Count == 0)
        {
            McpLog.Info($"[Move] No path from {sourceId} to {targetSystemId} for {fleet.Name}");
            return;
        }

        movement.IssueMoveOrder(fleet, path);
        EventBus.Instance?.FireFleetOrderChanged(fleet.Id);
        McpLog.Info($"[Move] {fleet.Name} → system {targetSystemId} ({path.Count} hops)");
    }
}
