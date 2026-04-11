using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Core.Systems;

/// <summary>
/// Generates navigable lanes between star systems using K-nearest-neighbors,
/// ensures graph connectivity, marks hidden lanes, and identifies chokepoints.
/// </summary>
public static class LaneGenerator
{
    public static List<LaneData> Generate(
        List<StarSystemData> systems,
        float maxLaneLength,
        int minNeighbors,
        int maxNeighbors,
        float hiddenLaneRatio,
        GameRandom rng)
    {
        var lanes = new List<LaneData>();
        var laneSet = new HashSet<(int, int)>(); // prevent duplicate edges

        // Step 1: Connect each system to K nearest neighbors
        for (int i = 0; i < systems.Count; i++)
        {
            var sys = systems[i];
            int k = rng.RangeInt(minNeighbors, maxNeighbors + 1);

            var nearest = systems
                .Select((other, idx) => (other, idx, dist: Distance(sys, other)))
                .Where(x => x.idx != i && x.dist <= maxLaneLength)
                .OrderBy(x => x.dist)
                .Take(k);

            foreach (var (other, idx, dist) in nearest)
            {
                int a = Math.Min(i, idx);
                int b = Math.Max(i, idx);
                if (laneSet.Add((a, b)))
                {
                    lanes.Add(new LaneData
                    {
                        SystemA = a,
                        SystemB = b,
                        Distance = dist,
                        Type = LaneType.Visible
                    });
                }
            }
        }

        // Step 2: Ensure full connectivity via union-find
        EnsureConnectivity(systems, lanes, laneSet, maxLaneLength * 1.5f);

        // Step 3: Add hidden lanes (new inter-arm shortcuts between unconnected systems)
        AddHiddenLanes(systems, lanes, laneSet, maxLaneLength, hiddenLaneRatio, rng);

        // Step 4: Identify chokepoints (simplified: lanes whose removal disconnects components)
        MarkChokepoints(systems, lanes);

        // Step 5: Wire lane indices back to systems
        for (int i = 0; i < lanes.Count; i++)
        {
            systems[lanes[i].SystemA].ConnectedLaneIndices.Add(i);
            systems[lanes[i].SystemB].ConnectedLaneIndices.Add(i);
        }

        return lanes;
    }

    private static float Distance(StarSystemData a, StarSystemData b)
    {
        float dx = a.PositionX - b.PositionX;
        float dz = a.PositionZ - b.PositionZ;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>Union-find to detect and fix disconnected components.</summary>
    private static void EnsureConnectivity(
        List<StarSystemData> systems,
        List<LaneData> lanes,
        HashSet<(int, int)> laneSet,
        float extendedMaxLength)
    {
        int n = systems.Count;
        int[] parent = new int[n];
        int[] rank = new int[n];
        for (int i = 0; i < n; i++) parent[i] = i;

        int Find(int x)
        {
            while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; }
            return x;
        }

        void Union(int x, int y)
        {
            int rx = Find(x), ry = Find(y);
            if (rx == ry) return;
            if (rank[rx] < rank[ry]) (rx, ry) = (ry, rx);
            parent[ry] = rx;
            if (rank[rx] == rank[ry]) rank[rx]++;
        }

        foreach (var lane in lanes)
            Union(lane.SystemA, lane.SystemB);

        // Group systems by component
        var components = new Dictionary<int, List<int>>();
        for (int i = 0; i < n; i++)
        {
            int root = Find(i);
            if (!components.ContainsKey(root))
                components[root] = new List<int>();
            components[root].Add(i);
        }

        if (components.Count <= 1) return;

        // Connect each component to the nearest other component
        var roots = components.Keys.ToList();
        for (int ci = 1; ci < roots.Count; ci++)
        {
            float bestDist = float.MaxValue;
            int bestA = -1, bestB = -1;

            foreach (int a in components[roots[0]])
            foreach (int b in components[roots[ci]])
            {
                float d = Distance(systems[a], systems[b]);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestA = a;
                    bestB = b;
                }
            }

            if (bestA >= 0)
            {
                int la = Math.Min(bestA, bestB);
                int lb = Math.Max(bestA, bestB);
                if (laneSet.Add((la, lb)))
                {
                    lanes.Add(new LaneData
                    {
                        SystemA = la,
                        SystemB = lb,
                        Distance = bestDist,
                        Type = LaneType.Visible
                    });
                }
                Union(bestA, bestB);
            }
        }
    }

    /// <summary>
    /// Add hidden lanes as new inter-arm shortcuts between systems that are
    /// not already connected. The visible graph is never modified.
    /// </summary>
    private static void AddHiddenLanes(
        List<StarSystemData> systems,
        List<LaneData> lanes,
        HashSet<(int, int)> laneSet,
        float maxLaneLength,
        float hiddenRatio,
        GameRandom rng)
    {
        // Find candidate pairs: inter-arm, within extended range, not already connected
        float hiddenMaxLength = maxLaneLength * 1.5f;
        var candidates = new List<(int a, int b, float dist)>();

        for (int i = 0; i < systems.Count; i++)
        {
            if (systems[i].IsCore) continue;

            for (int j = i + 1; j < systems.Count; j++)
            {
                if (systems[j].IsCore) continue;
                if (systems[i].ArmIndex == systems[j].ArmIndex) continue;
                if (laneSet.Contains((i, j))) continue;

                float d = Distance(systems[i], systems[j]);
                if (d <= hiddenMaxLength)
                    candidates.Add((i, j, d));
            }
        }

        // Pick a number proportional to existing visible lanes
        int hiddenCount = (int)(lanes.Count * hiddenRatio);
        rng.Shuffle(candidates);
        int added = Math.Min(hiddenCount, candidates.Count);

        for (int i = 0; i < added; i++)
        {
            var (a, b, dist) = candidates[i];
            if (laneSet.Add((a, b)))
            {
                lanes.Add(new LaneData
                {
                    SystemA = a,
                    SystemB = b,
                    Distance = dist,
                    Type = LaneType.Hidden
                });
            }
        }
    }

    /// <summary>
    /// Iterative bridge-finding (Tarjan's algorithm without recursion).
    /// A lane is a chokepoint/bridge if removing it disconnects the graph.
    /// </summary>
    private static void MarkChokepoints(List<StarSystemData> systems, List<LaneData> lanes)
    {
        int n = systems.Count;
        var adj = new List<(int neighbor, int laneIdx)>[n];
        for (int i = 0; i < n; i++)
            adj[i] = new List<(int, int)>();

        for (int i = 0; i < lanes.Count; i++)
        {
            adj[lanes[i].SystemA].Add((lanes[i].SystemB, i));
            adj[lanes[i].SystemB].Add((lanes[i].SystemA, i));
        }

        int[] disc = new int[n];
        int[] low = new int[n];
        bool[] visited = new bool[n];
        int[] parentLane = new int[n];   // lane index used to reach this node
        int[] neighborIdx = new int[n];  // iteration cursor into adj[node]
        Array.Fill(disc, -1);
        Array.Fill(parentLane, -1);

        int timer = 0;
        var stack = new Stack<int>();

        for (int start = 0; start < n; start++)
        {
            if (visited[start]) continue;

            stack.Push(start);
            visited[start] = true;
            disc[start] = low[start] = timer++;
            neighborIdx[start] = 0;

            while (stack.Count > 0)
            {
                int u = stack.Peek();

                if (neighborIdx[u] < adj[u].Count)
                {
                    var (v, laneIdx) = adj[u][neighborIdx[u]];
                    neighborIdx[u]++;

                    if (laneIdx == parentLane[u]) continue;

                    if (!visited[v])
                    {
                        visited[v] = true;
                        disc[v] = low[v] = timer++;
                        parentLane[v] = laneIdx;
                        neighborIdx[v] = 0;
                        stack.Push(v);
                    }
                    else
                    {
                        low[u] = Math.Min(low[u], disc[v]);
                    }
                }
                else
                {
                    // Backtrack: update parent's low value
                    stack.Pop();
                    if (stack.Count > 0)
                    {
                        int parent = stack.Peek();
                        low[parent] = Math.Min(low[parent], low[u]);

                        if (low[u] > disc[parent])
                            lanes[parentLane[u]].IsChokepoint = true;
                    }
                }
            }
        }
    }
}
