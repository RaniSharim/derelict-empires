using Xunit;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Tests.Pathfinding;

public class FleetMovementTests
{
    private static GalaxyData MakeLinearGalaxy()
    {
        var systems = new List<StarSystemData>();
        for (int i = 0; i < 5; i++)
            systems.Add(new StarSystemData { Id = i, Name = $"S{i}", PositionX = i * 10f, PositionZ = 0 });

        var lanes = new List<LaneData>();
        for (int i = 0; i < 4; i++)
        {
            lanes.Add(new LaneData { SystemA = i, SystemB = i + 1, Distance = 10f, Type = LaneType.Visible });
            systems[i].ConnectedLaneIndices.Add(i);
            systems[i + 1].ConnectedLaneIndices.Add(i);
        }

        return new GalaxyData { Systems = systems, Lanes = lanes };
    }

    [Fact]
    public void Fleet_MovesAlongPath()
    {
        var galaxy = MakeLinearGalaxy();
        var movement = new FleetMovementSystem(galaxy);

        var fleet = new FleetData { Id = 0, Name = "Test Fleet", CurrentSystemId = 0, Speed = 10f };
        var fleets = new List<FleetData> { fleet };

        // Issue order: move from 0 to 2
        movement.IssueMoveOrder(fleet, new List<int> { 1, 2 });

        // Tick enough to reach system 1 (distance 10, speed 10, takes 1.0s = 10 fast ticks)
        for (int i = 0; i < 10; i++)
            movement.ProcessTick(0.1f, fleets);

        Assert.Equal(1, fleet.CurrentSystemId);
    }

    [Fact]
    public void Fleet_CompletesFullPath()
    {
        var galaxy = MakeLinearGalaxy();
        var movement = new FleetMovementSystem(galaxy);
        bool orderComplete = false;
        movement.OrderCompleted += _ => orderComplete = true;

        var fleet = new FleetData { Id = 0, Name = "Test", CurrentSystemId = 0, Speed = 10f };
        var fleets = new List<FleetData> { fleet };

        movement.IssueMoveOrder(fleet, new List<int> { 1, 2, 3 });

        // Need 30 distance at speed 10 = 3 seconds = 30 fast ticks
        for (int i = 0; i < 35; i++)
            movement.ProcessTick(0.1f, fleets);

        Assert.Equal(3, fleet.CurrentSystemId);
        Assert.True(orderComplete);
        Assert.Null(movement.GetOrder(fleet.Id));
    }

    [Fact]
    public void Fleet_InTransit_HasNegativeSystemId()
    {
        var galaxy = MakeLinearGalaxy();
        var movement = new FleetMovementSystem(galaxy);

        var fleet = new FleetData { Id = 0, Name = "Test", CurrentSystemId = 0, Speed = 10f };
        var fleets = new List<FleetData> { fleet };

        movement.IssueMoveOrder(fleet, new List<int> { 1 });

        // One tick — fleet is partway between 0 and 1
        movement.ProcessTick(0.1f, fleets);
        Assert.Equal(-1, fleet.CurrentSystemId); // In transit
    }

    [Fact]
    public void Fleet_Position_InterpolatesDuringTransit()
    {
        var galaxy = MakeLinearGalaxy();
        var movement = new FleetMovementSystem(galaxy);

        var fleet = new FleetData { Id = 0, Name = "Test", CurrentSystemId = 0, Speed = 10f };
        var fleets = new List<FleetData> { fleet };

        movement.IssueMoveOrder(fleet, new List<int> { 1 });

        // System 0 is at x=0, system 1 is at x=10
        // After 5 ticks (0.5s), should be halfway
        for (int i = 0; i < 5; i++)
            movement.ProcessTick(0.1f, fleets);

        var (x, z) = movement.GetFleetPosition(fleet);
        Assert.InRange(x, 4f, 6f); // Roughly halfway
        Assert.Equal(0f, z);
    }

    [Fact]
    public void Fleet_StationaryPosition_MatchesSystem()
    {
        var galaxy = MakeLinearGalaxy();
        var movement = new FleetMovementSystem(galaxy);

        var fleet = new FleetData { Id = 0, Name = "Test", CurrentSystemId = 2, Speed = 10f };

        var (x, z) = movement.GetFleetPosition(fleet);
        Assert.Equal(20f, x); // System 2 is at x=20
        Assert.Equal(0f, z);
    }

    [Fact]
    public void CancelOrder_StopsFleet()
    {
        var galaxy = MakeLinearGalaxy();
        var movement = new FleetMovementSystem(galaxy);

        var fleet = new FleetData { Id = 0, Name = "Test", CurrentSystemId = 0, Speed = 10f };
        var fleets = new List<FleetData> { fleet };

        movement.IssueMoveOrder(fleet, new List<int> { 1, 2, 3 });
        movement.ProcessTick(0.1f, fleets); // Start moving

        movement.CancelOrder(fleet);
        Assert.Null(movement.GetOrder(fleet.Id));

        // Further ticks shouldn't move the fleet
        int sysId = fleet.CurrentSystemId;
        movement.ProcessTick(0.1f, fleets);
        movement.ProcessTick(0.1f, fleets);
        Assert.Equal(sysId, fleet.CurrentSystemId);
    }

    [Fact]
    public void MultipleFleets_MoveIndependently()
    {
        var galaxy = MakeLinearGalaxy();
        var movement = new FleetMovementSystem(galaxy);

        var fleet1 = new FleetData { Id = 0, Name = "F1", CurrentSystemId = 0, Speed = 10f };
        var fleet2 = new FleetData { Id = 1, Name = "F2", CurrentSystemId = 4, Speed = 20f };
        var fleets = new List<FleetData> { fleet1, fleet2 };

        movement.IssueMoveOrder(fleet1, new List<int> { 1 });
        movement.IssueMoveOrder(fleet2, new List<int> { 3 });

        // Fleet2 is faster, should arrive sooner
        for (int i = 0; i < 5; i++)
            movement.ProcessTick(0.1f, fleets);

        // Fleet2 (speed 20, distance 10) should arrive in 0.5s = 5 ticks
        Assert.Equal(3, fleet2.CurrentSystemId);
        // Fleet1 (speed 10, distance 10) should still be in transit after 0.5s
        Assert.Equal(-1, fleet1.CurrentSystemId);
    }

    [Fact]
    public void FleetArrived_EventFires()
    {
        var galaxy = MakeLinearGalaxy();
        var movement = new FleetMovementSystem(galaxy);

        var arrivals = new List<(int fleetId, int systemId)>();
        movement.FleetArrived += (fleet, sysId) => arrivals.Add((fleet.Id, sysId));

        var fleet = new FleetData { Id = 0, Name = "Test", CurrentSystemId = 0, Speed = 100f };
        var fleets = new List<FleetData> { fleet };

        movement.IssueMoveOrder(fleet, new List<int> { 1, 2 });

        // High speed: should reach both systems quickly
        for (int i = 0; i < 20; i++)
            movement.ProcessTick(0.1f, fleets);

        Assert.Contains(arrivals, a => a.fleetId == 0 && a.systemId == 1);
        Assert.Contains(arrivals, a => a.fleetId == 0 && a.systemId == 2);
    }
}
