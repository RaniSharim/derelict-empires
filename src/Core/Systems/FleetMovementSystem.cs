using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Core.Systems;

/// <summary>
/// Processes fleet movement along lanes each fast tick. Pure C#.
/// Fleets interpolate between systems along their order's path.
/// </summary>
public class FleetMovementSystem
{
    /// <summary>Fired when a fleet arrives at a system.</summary>
    public event Action<FleetData, int>? FleetArrived;

    /// <summary>Fired when a fleet leaves a system and enters lane transit. Arg: systemId it left.</summary>
    public event Action<FleetData, int>? FleetDeparted;

    /// <summary>Fired when a fleet completes its entire move order.</summary>
    public event Action<FleetData>? OrderCompleted;

    private readonly GalaxyData _galaxy;
    private readonly Dictionary<int, FleetOrder> _orders = new();

    public FleetMovementSystem(GalaxyData galaxy)
    {
        _galaxy = galaxy;
    }

    /// <summary>Issue a move order to a fleet.</summary>
    public void IssueMoveOrder(FleetData fleet, List<int> path)
    {
        if (path.Count == 0) return;

        var order = new FleetOrder
        {
            Type = FleetOrderType.MoveTo,
            Path = new List<int>(path),
            PathIndex = 0,
            LaneProgress = 0f,
            TransitFromSystemId = fleet.CurrentSystemId
        };
        _orders[fleet.Id] = order;
    }

    /// <summary>Restore a saved order for a fleet (used by save/load).</summary>
    public void RestoreOrder(FleetData fleet, FleetOrder order)
    {
        _orders[fleet.Id] = order;
    }

    /// <summary>Cancel the current order for a fleet.</summary>
    public void CancelOrder(FleetData fleet)
    {
        _orders.Remove(fleet.Id);
    }

    /// <summary>Get the current order for a fleet, if any.</summary>
    public FleetOrder? GetOrder(int fleetId) =>
        _orders.GetValueOrDefault(fleetId);

    /// <summary>
    /// Process one fast tick of movement for all fleets with orders.
    /// Call this from the fast tick handler.
    /// </summary>
    public void ProcessTick(float tickDelta, IReadOnlyList<FleetData> allFleets)
    {
        foreach (var fleet in allFleets)
        {
            if (!_orders.TryGetValue(fleet.Id, out var order)) continue;
            if (order.IsComplete)
            {
                _orders.Remove(fleet.Id);
                OrderCompleted?.Invoke(fleet);
                continue;
            }

            AdvanceFleet(fleet, order, tickDelta);
        }
    }

    private void AdvanceFleet(FleetData fleet, FleetOrder order, float tickDelta)
    {
        int fromId = order.TransitFromSystemId;
        int toId = order.NextSystemId;
        if (toId < 0) return;

        // Find the lane between fromId and toId. Inlined loop (not LINQ) because this
        // runs 10 Hz × moving-fleet-count; FirstOrDefault on IReadOnlyList still
        // allocates an enumerator for non-List concrete types.
        LaneData? lane = null;
        var lanes = _galaxy.GetLanesForSystem(fromId);
        for (int i = 0; i < lanes.Count; i++)
        {
            if (lanes[i].GetOtherSystem(fromId) == toId) { lane = lanes[i]; break; }
        }

        if (lane == null)
        {
            // No lane exists — cancel order
            _orders.Remove(fleet.Id);
            return;
        }

        // Advance progress
        float travelTime = lane.Distance / fleet.Speed;
        if (travelTime <= 0f) travelTime = 0.01f;

        order.LaneProgress += tickDelta / travelTime;

        if (order.LaneProgress >= 1.0f)
        {
            // Arrived at next system
            order.LaneProgress = 0f;
            fleet.CurrentSystemId = toId;
            FleetArrived?.Invoke(fleet, toId);

            order.PathIndex++;
            if (order.PathIndex < order.Path.Count)
            {
                order.TransitFromSystemId = toId;
            }
            else
            {
                _orders.Remove(fleet.Id);
                OrderCompleted?.Invoke(fleet);
            }
        }
        else
        {
            // In transit — mark fleet as not at any system
            int prevSystem = fleet.CurrentSystemId;
            fleet.CurrentSystemId = -1;
            if (prevSystem >= 0)
                FleetDeparted?.Invoke(fleet, prevSystem);
        }
    }

    /// <summary>
    /// Get the interpolated world position of a fleet (for rendering).
    /// Returns the position between two systems based on lane progress.
    /// </summary>
    public (float x, float z) GetFleetPosition(FleetData fleet)
    {
        if (!_orders.TryGetValue(fleet.Id, out var order) || order.IsComplete)
        {
            // Fleet is stationary at its current system
            var sys = _galaxy.GetSystem(fleet.CurrentSystemId);
            if (sys == null) return (0, 0);
            return (sys.PositionX, sys.PositionZ);
        }

        var fromSys = _galaxy.GetSystem(order.TransitFromSystemId);
        var toSys = _galaxy.GetSystem(order.NextSystemId);
        if (fromSys == null || toSys == null)
        {
            var fallback = _galaxy.GetSystem(fleet.CurrentSystemId);
            return fallback != null ? (fallback.PositionX, fallback.PositionZ) : (0, 0);
        }

        float t = order.LaneProgress;
        float x = fromSys.PositionX + (toSys.PositionX - fromSys.PositionX) * t;
        float z = fromSys.PositionZ + (toSys.PositionZ - fromSys.PositionZ) * t;
        return (x, z);
    }
}
