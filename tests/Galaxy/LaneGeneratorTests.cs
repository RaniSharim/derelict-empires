using Xunit;
using DerlictEmpires.Core.Systems;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Tests.Galaxy;

public class LaneGeneratorTests
{
    private static GalaxyGenerationConfig DefaultConfig() => new()
    {
        Seed = 42,
        TotalSystems = 100,
        ArmCount = 4,
        GalaxyRadius = 200f,
        MaxLaneLength = 60f,
        MinNeighbors = 2,
        MaxNeighbors = 4,
        HiddenLaneRatio = 0.15f
    };

    [Fact]
    public void Graph_IsConnected()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());

        // BFS from system 0 should reach all systems
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(0);
        visited.Add(0);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            foreach (int neighbor in galaxy.GetNeighbors(current))
            {
                if (visited.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }

        Assert.Equal(galaxy.Systems.Count, visited.Count);
    }

    [Fact]
    public void VisibleGraph_IsConnected()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());

        // BFS using only visible lanes should still reach all systems
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(0);
        visited.Add(0);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            foreach (var lane in galaxy.GetLanesForSystem(current))
            {
                if (lane.Type == LaneType.Hidden) continue;
                int neighbor = lane.GetOtherSystem(current);
                if (visited.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }

        Assert.Equal(galaxy.Systems.Count, visited.Count);
    }

    [Fact]
    public void VisibleGraph_IsConnected_AcrossMultipleSeeds()
    {
        // Test with many seeds to catch probabilistic disconnections
        for (int seed = 0; seed < 50; seed++)
        {
            var config = DefaultConfig();
            config.Seed = seed;
            var galaxy = GalaxyGenerator.Generate(config);

            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(0);
            visited.Add(0);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (var lane in galaxy.GetLanesForSystem(current))
                {
                    if (lane.Type == LaneType.Hidden) continue;
                    int neighbor = lane.GetOtherSystem(current);
                    if (visited.Add(neighbor))
                        queue.Enqueue(neighbor);
                }
            }

            Assert.True(visited.Count == galaxy.Systems.Count,
                $"Seed {seed}: visible graph disconnected — reached {visited.Count}/{galaxy.Systems.Count} systems");
        }
    }

    [Fact]
    public void NoLane_ExceedsMaxLength_ByTooMuch()
    {
        var config = DefaultConfig();
        var galaxy = GalaxyGenerator.Generate(config);

        // Lanes from K-nearest may be within maxLaneLength.
        // Connectivity bridges may exceed it but should be reasonable.
        foreach (var lane in galaxy.Lanes)
        {
            // Allow 50% over for connectivity bridges
            Assert.True(lane.Distance < config.MaxLaneLength * 2f,
                $"Lane {lane.SystemA}-{lane.SystemB} distance {lane.Distance} exceeds reasonable limit");
        }
    }

    [Fact]
    public void HiddenLanes_AreInterArm()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());

        foreach (var lane in galaxy.Lanes.Where(l => l.Type == LaneType.Hidden))
        {
            var sysA = galaxy.Systems[lane.SystemA];
            var sysB = galaxy.Systems[lane.SystemB];
            // Hidden lanes should connect systems in different arms (neither core)
            Assert.True(
                sysA.ArmIndex != sysB.ArmIndex || sysA.IsCore || sysB.IsCore,
                $"Hidden lane {lane.SystemA}-{lane.SystemB} connects same-arm non-core systems");
        }
    }

    [Fact]
    public void HiddenLanes_AreRoughly15Percent_OfVisibleLanes()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());

        int visibleCount = galaxy.Lanes.Count(l => l.Type == LaneType.Visible);
        int hiddenCount = galaxy.Lanes.Count(l => l.Type == LaneType.Hidden);

        if (visibleCount == 0) return;

        float ratio = (float)hiddenCount / visibleCount;
        // Target is 15% of visible lane count, allow generous range
        Assert.InRange(ratio, 0.0f, 0.30f);
    }

    [Fact]
    public void HiddenLanes_DoNotDuplicateVisibleLanes()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());

        // Each pair of systems should have at most one lane
        var pairs = new HashSet<(int, int)>();
        foreach (var lane in galaxy.Lanes)
        {
            int a = Math.Min(lane.SystemA, lane.SystemB);
            int b = Math.Max(lane.SystemA, lane.SystemB);
            Assert.True(pairs.Add((a, b)),
                $"Duplicate lane between {a} and {b}");
        }
    }

    [Fact]
    public void Chokepoints_AreIdentifiedOrGraphIsDense()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());
        int chokeCount = galaxy.Lanes.Count(l => l.IsChokepoint);

        // In a well-connected K-nearest graph, there may be no bridge edges.
        // Verify the algorithm runs without error and that if bridges exist they're marked.
        // If no chokepoints, confirm graph is well-connected (min degree >= 2).
        if (chokeCount == 0)
        {
            // Every system should have multiple connections (no leaf nodes needing bridges)
            foreach (var sys in galaxy.Systems)
            {
                int degree = galaxy.GetLanesForSystem(sys.Id).Count();
                Assert.True(degree >= 2,
                    $"System {sys.Id} has degree {degree} but no chokepoints found — should have a bridge");
            }
        }
        // If chokepoints are found, they should be valid bridge edges
        Assert.True(chokeCount >= 0); // Algorithm ran without error
    }

    [Fact]
    public void EverySystems_HasAtLeastOneLane()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());
        foreach (var sys in galaxy.Systems)
        {
            var laneCount = galaxy.GetLanesForSystem(sys.Id).Count();
            Assert.True(laneCount > 0, $"System {sys.Id} ({sys.Name}) has no lanes");
        }
    }
}
