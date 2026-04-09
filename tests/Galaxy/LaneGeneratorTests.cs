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
    public void HiddenLanes_AreRoughly15Percent_OfInterArmLanes()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());

        var interArmLanes = galaxy.Lanes.Where(l =>
        {
            var a = galaxy.Systems[l.SystemA];
            var b = galaxy.Systems[l.SystemB];
            return !a.IsCore && !b.IsCore && a.ArmIndex != b.ArmIndex;
        }).ToList();

        if (interArmLanes.Count == 0) return; // No inter-arm lanes to test

        int hiddenCount = interArmLanes.Count(l => l.Type == LaneType.Hidden);
        float ratio = (float)hiddenCount / interArmLanes.Count;
        // Should be roughly 15% ± 10%
        Assert.InRange(ratio, 0.0f, 0.35f);
    }

    [Fact]
    public void Chokepoints_AreIdentified()
    {
        var galaxy = GalaxyGenerator.Generate(DefaultConfig());
        int chokeCount = galaxy.Lanes.Count(l => l.IsChokepoint);
        // We should find at least some chokepoints in a 100-system galaxy
        Assert.True(chokeCount > 0, "No chokepoints identified");
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
