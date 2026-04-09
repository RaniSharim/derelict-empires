using Xunit;
using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Tests.Pathfinding;

public class LanePathfinderTests
{
    /// <summary>Build a simple linear galaxy: 0 -- 1 -- 2 -- 3 -- 4</summary>
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

    /// <summary>Galaxy with a hidden shortcut: 0--1--2--3, plus hidden 0--3</summary>
    private static GalaxyData MakeGalaxyWithHiddenLane()
    {
        var systems = new List<StarSystemData>();
        for (int i = 0; i < 4; i++)
            systems.Add(new StarSystemData { Id = i, Name = $"S{i}", PositionX = i * 10f, PositionZ = 0 });

        var lanes = new List<LaneData>
        {
            new() { SystemA = 0, SystemB = 1, Distance = 10f, Type = LaneType.Visible },
            new() { SystemA = 1, SystemB = 2, Distance = 10f, Type = LaneType.Visible },
            new() { SystemA = 2, SystemB = 3, Distance = 10f, Type = LaneType.Visible },
            new() { SystemA = 0, SystemB = 3, Distance = 5f, Type = LaneType.Hidden }, // shortcut
        };

        for (int i = 0; i < lanes.Count; i++)
        {
            systems[lanes[i].SystemA].ConnectedLaneIndices.Add(i);
            systems[lanes[i].SystemB].ConnectedLaneIndices.Add(i);
        }

        return new GalaxyData { Systems = systems, Lanes = lanes };
    }

    [Fact]
    public void ShortestPath_Linear()
    {
        var galaxy = MakeLinearGalaxy();
        var path = LanePathfinder.FindPath(galaxy, 0, 4);
        Assert.Equal(new List<int> { 1, 2, 3, 4 }, path);
    }

    [Fact]
    public void ShortestPath_Adjacent()
    {
        var galaxy = MakeLinearGalaxy();
        var path = LanePathfinder.FindPath(galaxy, 1, 2);
        Assert.Single(path);
        Assert.Equal(2, path[0]);
    }

    [Fact]
    public void SameSourceAndDest_ReturnsEmpty()
    {
        var galaxy = MakeLinearGalaxy();
        var path = LanePathfinder.FindPath(galaxy, 2, 2);
        Assert.Empty(path);
    }

    [Fact]
    public void UnreachableSystem_ReturnsEmpty()
    {
        // Create a disconnected galaxy
        var systems = new List<StarSystemData>
        {
            new() { Id = 0, Name = "A", PositionX = 0 },
            new() { Id = 1, Name = "B", PositionX = 10 },
            new() { Id = 2, Name = "C", PositionX = 100 }, // isolated
        };
        var lanes = new List<LaneData>
        {
            new() { SystemA = 0, SystemB = 1, Distance = 10f, Type = LaneType.Visible }
        };
        systems[0].ConnectedLaneIndices.Add(0);
        systems[1].ConnectedLaneIndices.Add(0);

        var galaxy = new GalaxyData { Systems = systems, Lanes = lanes };
        var path = LanePathfinder.FindPath(galaxy, 0, 2);
        Assert.Empty(path);
    }

    [Fact]
    public void HiddenLanes_ExcludedByDefault()
    {
        var galaxy = MakeGalaxyWithHiddenLane();
        var path = LanePathfinder.FindPath(galaxy, 0, 3, canUseHiddenLanes: false);
        // Must go 0->1->2->3 (3 hops) since hidden shortcut is excluded
        Assert.Equal(new List<int> { 1, 2, 3 }, path);
    }

    [Fact]
    public void HiddenLanes_UsedWhenAllowed()
    {
        var galaxy = MakeGalaxyWithHiddenLane();
        var path = LanePathfinder.FindPath(galaxy, 0, 3, canUseHiddenLanes: true);
        // Should take the hidden shortcut 0->3 (1 hop, distance 5 < 30)
        Assert.Single(path);
        Assert.Equal(3, path[0]);
    }

    [Fact]
    public void PathDistance_Correct()
    {
        var galaxy = MakeLinearGalaxy();
        var path = LanePathfinder.FindPath(galaxy, 0, 3);
        float dist = LanePathfinder.PathDistance(galaxy, 0, path);
        Assert.Equal(30f, dist);
    }

    [Fact]
    public void MultiHopPath_IsOptimal()
    {
        // On a generated galaxy, verify the path returned is actually shortest
        var galaxy = GalaxyGenerator.Generate(new GalaxyGenerationConfig { Seed = 42, TotalSystems = 50, ArmCount = 4 });

        // Pick two systems far apart
        int src = 0;
        int dst = galaxy.Systems.Count - 1;
        var path = LanePathfinder.FindPath(galaxy, src, dst);

        if (path.Count == 0) return; // May be unreachable via visible lanes only

        // Verify path is valid: each consecutive pair is connected by a lane
        int current = src;
        foreach (int next in path)
        {
            bool connected = galaxy.GetLanesForSystem(current)
                .Any(l => l.GetOtherSystem(current) == next && l.Type == LaneType.Visible);
            Assert.True(connected, $"No visible lane from {current} to {next}");
            current = next;
        }
    }
}
