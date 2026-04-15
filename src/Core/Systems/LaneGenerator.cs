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

        // Step 1: Connect each system to K nearest neighbors (no crossing visible lanes)
        for (int i = 0; i < systems.Count; i++)
        {
            var sys = systems[i];
            int k = rng.RangeInt(minNeighbors, maxNeighbors + 1);

            // Try more candidates than k so we can skip crossings and still get enough
            var nearest = systems
                .Select((other, idx) => (other, idx, dist: Distance(sys, other)))
                .Where(x => x.idx != i && x.dist <= maxLaneLength)
                .OrderBy(x => x.dist)
                .Take(k * 3);

            int added = 0;
            foreach (var (other, idx, dist) in nearest)
            {
                if (added >= k) break;
                int a = Math.Min(i, idx);
                int b = Math.Max(i, idx);
                if (!laneSet.Add((a, b))) { added++; continue; } // already exists, counts toward k

                if (CrossesAnyLane(systems, lanes, a, b))
                {
                    laneSet.Remove((a, b)); // undo the add
                    continue; // skip but don't count toward k
                }

                lanes.Add(new LaneData
                {
                    SystemA = a,
                    SystemB = b,
                    Distance = dist,
                    Type = LaneType.Visible
                });
                added++;
            }
        }

        // Step 2: Ensure full connectivity via union-find
        EnsureConnectivity(systems, lanes, laneSet, maxLaneLength * 1.5f);

        // Step 3: Remove any remaining visible lane crossings (connectivity bridges exempt)
        RemoveCrossingLanes(systems, lanes, laneSet);

        // Step 4: Add hidden lanes (shortcuts, exempt from crossing rules)
        AddHiddenLanes(systems, lanes, laneSet, maxLaneLength, hiddenLaneRatio, rng);

        // Step 5: Identify chokepoints (simplified: lanes whose removal disconnects components)
        MarkChokepoints(systems, lanes);

        // Step 6: Wire lane indices back to systems
        for (int i = 0; i < lanes.Count; i++)
        {
            systems[lanes[i].SystemA].ConnectedLaneIndices.Add(i);
            systems[lanes[i].SystemB].ConnectedLaneIndices.Add(i);
        }

        return lanes;
    }

    /// <summary>
    /// Post-process: remove visible lanes that cross other visible lanes.
    /// For each crossing pair, remove the longer lane if it's not a bridge.
    /// </summary>
    private static void RemoveCrossingLanes(
        List<StarSystemData> systems,
        List<LaneData> lanes,
        HashSet<(int, int)> laneSet)
    {
        bool changed = true;
        while (changed)
        {
            changed = false;
            // Find first crossing pair among visible lanes
            for (int i = 0; i < lanes.Count && !changed; i++)
            {
                if (lanes[i].Type != LaneType.Visible) continue;
                for (int j = i + 1; j < lanes.Count && !changed; j++)
                {
                    if (lanes[j].Type != LaneType.Visible) continue;
                    if (!SegmentsCross(systems, lanes[i], lanes[j])) continue;

                    // Remove the longer lane if the graph stays connected without it
                    int removeIdx = lanes[i].Distance >= lanes[j].Distance ? i : j;
                    var removeLane = lanes[removeIdx];

                    // Check if removal disconnects the visible graph
                    if (!IsVisibleBridge(systems, lanes, removeIdx))
                    {
                        laneSet.Remove((removeLane.SystemA, removeLane.SystemB));
                        lanes.RemoveAt(removeIdx);
                        changed = true;
                    }
                    else
                    {
                        // Try the other one
                        int otherIdx = removeIdx == i ? j : i;
                        var otherLane = lanes[otherIdx];
                        if (!IsVisibleBridge(systems, lanes, otherIdx))
                        {
                            laneSet.Remove((otherLane.SystemA, otherLane.SystemB));
                            lanes.RemoveAt(otherIdx);
                            changed = true;
                        }
                        // else: both are bridges, keep both (rare)
                    }
                }
            }
        }
    }

    /// <summary>Check if removing lane at index would disconnect the visible graph.</summary>
    private static bool IsVisibleBridge(List<StarSystemData> systems, List<LaneData> lanes, int skipIdx)
    {
        int n = systems.Count;
        // BFS from SystemA of the skipped lane, see if we can reach SystemB
        int start = lanes[skipIdx].SystemA;
        int target = lanes[skipIdx].SystemB;

        var visited = new bool[n];
        var queue = new Queue<int>();
        visited[start] = true;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            int cur = queue.Dequeue();
            if (cur == target) return false; // reachable → not a bridge

            for (int li = 0; li < lanes.Count; li++)
            {
                if (li == skipIdx) continue;
                if (lanes[li].Type != LaneType.Visible) continue;

                int neighbor = -1;
                if (lanes[li].SystemA == cur) neighbor = lanes[li].SystemB;
                else if (lanes[li].SystemB == cur) neighbor = lanes[li].SystemA;
                else continue;

                if (!visited[neighbor])
                {
                    visited[neighbor] = true;
                    queue.Enqueue(neighbor);
                }
            }
        }
        return true; // not reachable → it's a bridge
    }

    /// <summary>Check if a candidate lane (a→b) crosses any existing visible lane.</summary>
    private static bool CrossesAnyLane(List<StarSystemData> systems, List<LaneData> lanes, int a, int b)
    {
        for (int i = 0; i < lanes.Count; i++)
        {
            if (lanes[i].Type != LaneType.Visible) continue;
            if (SegmentsCross(systems, a, b, lanes[i].SystemA, lanes[i].SystemB))
                return true;
        }
        return false;
    }

    private static bool SegmentsCross(List<StarSystemData> systems, LaneData l1, LaneData l2)
        => SegmentsCross(systems, l1.SystemA, l1.SystemB, l2.SystemA, l2.SystemB);

    /// <summary>
    /// True if segments (p1→p2) and (p3→p4) cross each other in the interior.
    /// Shared endpoints do not count as crossing.
    /// </summary>
    private static bool SegmentsCross(List<StarSystemData> systems, int i1, int i2, int i3, int i4)
    {
        // Shared endpoint → never a crossing
        if (i1 == i3 || i1 == i4 || i2 == i3 || i2 == i4) return false;

        float ax = systems[i1].PositionX, az = systems[i1].PositionZ;
        float bx = systems[i2].PositionX, bz = systems[i2].PositionZ;
        float cx = systems[i3].PositionX, cz = systems[i3].PositionZ;
        float dx = systems[i4].PositionX, dz = systems[i4].PositionZ;

        float d1 = Cross(cx, cz, dx, dz, ax, az);
        float d2 = Cross(cx, cz, dx, dz, bx, bz);
        float d3 = Cross(ax, az, bx, bz, cx, cz);
        float d4 = Cross(ax, az, bx, bz, dx, dz);

        // Opposite sides check (strict crossing)
        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            return true;

        return false;
    }

    /// <summary>Cross product of (b-a) × (c-a).</summary>
    private static float Cross(float ax, float az, float bx, float bz, float cx, float cz)
        => (bx - ax) * (cz - az) - (bz - az) * (cx - ax);

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

        // Connect each component to the nearest other component, preferring non-crossing lanes
        var roots = components.Keys.ToList();
        for (int ci = 1; ci < roots.Count; ci++)
        {
            // Collect all candidate pairs sorted by distance
            var candidates = new List<(int a, int b, float dist)>();
            foreach (int a in components[roots[0]])
            foreach (int b in components[roots[ci]])
            {
                float d = Distance(systems[a], systems[b]);
                if (d <= extendedMaxLength)
                    candidates.Add((a, b, d));
            }
            candidates.Sort((x, y) => x.dist.CompareTo(y.dist));

            // Pick the shortest non-crossing candidate; fall back to shortest overall
            int bestA = -1, bestB = -1;
            float bestDist = float.MaxValue;
            foreach (var (a, b, d) in candidates)
            {
                int la = Math.Min(a, b);
                int lb = Math.Max(a, b);
                if (laneSet.Contains((la, lb))) continue;
                if (!CrossesAnyLane(systems, lanes, la, lb))
                {
                    bestA = a; bestB = b; bestDist = d;
                    break;
                }
                if (bestA < 0) { bestA = a; bestB = b; bestDist = d; } // fallback
            }

            // If no candidates within range, pick absolute closest
            if (bestA < 0)
            {
                foreach (int a in components[roots[0]])
                foreach (int b in components[roots[ci]])
                {
                    float d = Distance(systems[a], systems[b]);
                    if (d < bestDist) { bestDist = d; bestA = a; bestB = b; }
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
    /// Add hidden lanes as shortcuts — both cross-arm and along-arm.
    /// Candidates must not already be connected and must be long enough
    /// to be meaningful shortcuts (not redundant with visible lanes).
    /// Budget is spread across arms + core to avoid clustering.
    /// </summary>
    private static void AddHiddenLanes(
        List<StarSystemData> systems,
        List<LaneData> lanes,
        HashSet<(int, int)> laneSet,
        float maxLaneLength,
        float hiddenRatio,
        GameRandom rng)
    {
        float hiddenMaxLength = maxLaneLength * 1.2f;
        float hiddenMinLength = maxLaneLength * 0.4f; // must be meaningful shortcuts

        // Bucket candidates by region so we can spread the budget
        // Regions: each arm index + core (-1)
        var buckets = new Dictionary<int, List<(int a, int b, float dist)>>();

        for (int i = 0; i < systems.Count; i++)
        {
            for (int j = i + 1; j < systems.Count; j++)
            {
                if (laneSet.Contains((i, j))) continue;

                float d = Distance(systems[i], systems[j]);
                if (d < hiddenMinLength || d > hiddenMaxLength) continue;

                // Assign to the region of the lower-index system (arbitrary but stable)
                int region = systems[i].IsCore ? -1 : systems[i].ArmIndex;
                if (!buckets.ContainsKey(region))
                    buckets[region] = new List<(int, int, float)>();
                buckets[region].Add((i, j, d));
            }
        }

        // Total hidden lane budget
        int hiddenCount = (int)(lanes.Count * hiddenRatio / (1f - hiddenRatio));

        // Spread evenly across regions, remainder goes to random buckets
        int regionCount = Math.Max(1, buckets.Count);
        int perRegion = hiddenCount / regionCount;
        int remainder = hiddenCount - (perRegion * regionCount);

        var regionKeys = buckets.Keys.ToList();
        rng.Shuffle(regionKeys);

        int totalAdded = 0;
        for (int ri = 0; ri < regionKeys.Count && totalAdded < hiddenCount; ri++)
        {
            int budget = perRegion + (ri < remainder ? 1 : 0);
            var candidates = buckets[regionKeys[ri]];
            rng.Shuffle(candidates);

            for (int ci = 0; ci < candidates.Count && budget > 0; ci++)
            {
                var (a, b, dist) = candidates[ci];
                if (laneSet.Add((a, b)))
                {
                    lanes.Add(new LaneData
                    {
                        SystemA = a,
                        SystemB = b,
                        Distance = dist,
                        Type = LaneType.Hidden
                    });
                    budget--;
                    totalAdded++;
                }
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
