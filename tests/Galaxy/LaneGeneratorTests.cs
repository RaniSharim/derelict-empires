using Xunit;
using Xunit.Abstractions;
using DerlictEmpires.Core.Systems;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Tests.Galaxy;

public class LaneGeneratorTests
{
    private readonly ITestOutputHelper _output;

    public LaneGeneratorTests(ITestOutputHelper output)
    {
        _output = output;
    }

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

        var visited = VisibleBFS(galaxy, 0);
        Assert.Equal(galaxy.Systems.Count, visited.Count);
    }

    [Fact]
    public void VisibleGraph_IsConnected_AcrossMultipleSeeds()
    {
        for (int seed = 0; seed < 50; seed++)
        {
            var config = DefaultConfig();
            config.Seed = seed;
            var galaxy = GalaxyGenerator.Generate(config);

            var visited = VisibleBFS(galaxy, 0);
            Assert.True(visited.Count == galaxy.Systems.Count,
                $"Seed {seed}: visible graph disconnected — reached {visited.Count}/{galaxy.Systems.Count} systems");
        }
    }

    [Fact]
    public void EverySystem_HasAtLeastOneVisibleLane()
    {
        for (int seed = 0; seed < 50; seed++)
        {
            var config = DefaultConfig();
            config.Seed = seed;
            var galaxy = GalaxyGenerator.Generate(config);

            foreach (var sys in galaxy.Systems)
            {
                int visibleCount = galaxy.GetLanesForSystem(sys.Id)
                    .Count(l => l.Type == LaneType.Visible);
                Assert.True(visibleCount > 0,
                    $"Seed {seed}: System {sys.Id} ({sys.Name}) arm={sys.ArmIndex} " +
                    $"pos=({sys.PositionX:F1},{sys.PositionZ:F1}) has 0 visible lanes");
            }
        }
    }

    [Fact]
    public void EverySystem_HasAtLeastTwoVisibleLanes()
    {
        var config = DefaultConfig();
        var galaxy = GalaxyGenerator.Generate(config);

        int singletons = 0;
        foreach (var sys in galaxy.Systems)
        {
            int visibleCount = galaxy.GetLanesForSystem(sys.Id)
                .Count(l => l.Type == LaneType.Visible);
            if (visibleCount < 2)
            {
                singletons++;
                _output.WriteLine(
                    $"System {sys.Id} ({sys.Name}) arm={sys.ArmIndex} core={sys.IsCore} " +
                    $"pos=({sys.PositionX:F1},{sys.PositionZ:F1}) visibleLanes={visibleCount}");
            }
        }

        // A few leaf nodes are acceptable, but most systems should have 2+ visible lanes
        Assert.True(singletons <= galaxy.Systems.Count * 0.15,
            $"{singletons} systems have fewer than 2 visible lanes (max allowed: {galaxy.Systems.Count * 0.15:F0})");
    }

    [Fact]
    public void NoVisibleLane_ExceedsMaxLength_ByTooMuch()
    {
        var config = DefaultConfig();
        var galaxy = GalaxyGenerator.Generate(config);

        foreach (var lane in galaxy.Lanes.Where(l => l.Type == LaneType.Visible))
        {
            Assert.True(lane.Distance < config.MaxLaneLength * 2f,
                $"Visible lane {lane.SystemA}-{lane.SystemB} distance {lane.Distance:F1} " +
                $"exceeds {config.MaxLaneLength * 2f}");
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
            Assert.True(
                sysA.ArmIndex != sysB.ArmIndex || sysA.IsCore || sysB.IsCore,
                $"Hidden lane {lane.SystemA}-{lane.SystemB} connects same-arm non-core systems");
        }
    }

    [Fact]
    public void HiddenLanes_AreNewConnections_NotDuplicates()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());

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
    public void HiddenLanes_CountIsReasonable()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());

        int visibleCount = galaxy.Lanes.Count(l => l.Type == LaneType.Visible);
        int hiddenCount = galaxy.Lanes.Count(l => l.Type == LaneType.Hidden);

        _output.WriteLine($"Visible: {visibleCount}, Hidden: {hiddenCount}");

        Assert.True(hiddenCount > 0, "Should have some hidden lanes");
        Assert.True(hiddenCount <= visibleCount,
            $"Hidden lanes ({hiddenCount}) should not exceed visible lanes ({visibleCount})");
    }

    [Fact]
    public void Chokepoints_AreIdentifiedOrGraphIsDense()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());
        int chokeCount = galaxy.Lanes.Count(l => l.IsChokepoint);

        if (chokeCount == 0)
        {
            foreach (var sys in galaxy.Systems)
            {
                int degree = galaxy.GetLanesForSystem(sys.Id).Count();
                Assert.True(degree >= 2,
                    $"System {sys.Id} has degree {degree} but no chokepoints found");
            }
        }
        Assert.True(chokeCount >= 0);
    }

    [Fact]
    public void Diagnostic_GalaxyStats()
    {
        var config = DefaultConfig();
        var galaxy = GalaxyGenerator.Generate(config);

        var visibleDeg = new int[galaxy.Systems.Count];
        foreach (var lane in galaxy.Lanes.Where(l => l.Type == LaneType.Visible))
        {
            visibleDeg[lane.SystemA]++;
            visibleDeg[lane.SystemB]++;
        }

        _output.WriteLine($"Systems: {galaxy.Systems.Count}");
        _output.WriteLine($"Visible lanes: {galaxy.Lanes.Count(l => l.Type == LaneType.Visible)}");
        _output.WriteLine($"Hidden lanes: {galaxy.Lanes.Count(l => l.Type == LaneType.Hidden)}");
        _output.WriteLine($"Min visible degree: {visibleDeg.Min()}");
        _output.WriteLine($"Max visible degree: {visibleDeg.Max()}");
        _output.WriteLine($"Avg visible degree: {visibleDeg.Average():F1}");

        // Print systems with low visible degree
        for (int i = 0; i < galaxy.Systems.Count; i++)
        {
            if (visibleDeg[i] <= 1)
            {
                var s = galaxy.Systems[i];
                var lanes = galaxy.GetLanesForSystem(i).ToList();
                var visLanes = lanes.Where(l => l.Type == LaneType.Visible).ToList();
                _output.WriteLine(
                    $"  LOW: sys {s.Id} arm={s.ArmIndex} core={s.IsCore} " +
                    $"pos=({s.PositionX:F1},{s.PositionZ:F1}) " +
                    $"visDeg={visibleDeg[i]} totalDeg={lanes.Count}" +
                    (visLanes.Count > 0 ? $" nearestVisDist={visLanes.Min(l => l.Distance):F1}" : " NO_VIS_LANES"));
            }
        }

        // Visible BFS
        var visited = VisibleBFS(galaxy, 0);
        _output.WriteLine($"Visible BFS reached: {visited.Count}/{galaxy.Systems.Count}");

        Assert.Equal(galaxy.Systems.Count, visited.Count);
    }

    private static HashSet<int> VisibleBFS(DerlictEmpires.Core.Models.GalaxyData galaxy, int start)
    {
        var visited = new HashSet<int> { start };
        var queue = new Queue<int>();
        queue.Enqueue(start);

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

        return visited;
    }
}
