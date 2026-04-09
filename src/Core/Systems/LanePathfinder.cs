using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Core.Systems;

/// <summary>
/// Dijkstra shortest-path on the lane graph. Pure C#, no Godot dependency.
/// </summary>
public static class LanePathfinder
{
    /// <summary>
    /// Find the shortest path from source to destination through the lane graph.
    /// Returns a list of system IDs representing the path (excluding source, including destination).
    /// Returns empty list if no path exists.
    /// </summary>
    public static List<int> FindPath(
        GalaxyData galaxy,
        int sourceId,
        int destId,
        bool canUseHiddenLanes = false)
    {
        if (sourceId == destId) return new List<int>();

        int n = galaxy.Systems.Count;
        float[] dist = new float[n];
        int[] prev = new int[n];
        Array.Fill(dist, float.MaxValue);
        Array.Fill(prev, -1);

        dist[sourceId] = 0f;

        // Priority queue: (distance, systemId)
        var pq = new PriorityQueue<int, float>();
        pq.Enqueue(sourceId, 0f);

        var visited = new bool[n];

        while (pq.Count > 0)
        {
            int u = pq.Dequeue();
            if (visited[u]) continue;
            visited[u] = true;

            if (u == destId) break;

            foreach (var lane in galaxy.GetLanesForSystem(u))
            {
                if (!canUseHiddenLanes && lane.Type == LaneType.Hidden) continue;

                int v = lane.GetOtherSystem(u);
                if (visited[v]) continue;

                float newDist = dist[u] + lane.Distance;
                if (newDist < dist[v])
                {
                    dist[v] = newDist;
                    prev[v] = u;
                    pq.Enqueue(v, newDist);
                }
            }
        }

        // Reconstruct path
        if (prev[destId] == -1) return new List<int>(); // Unreachable

        var path = new List<int>();
        int current = destId;
        while (current != sourceId)
        {
            path.Add(current);
            current = prev[current];
        }
        path.Reverse();
        return path;
    }

    /// <summary>
    /// Get the total distance of a path.
    /// </summary>
    public static float PathDistance(GalaxyData galaxy, int sourceId, List<int> path)
    {
        if (path.Count == 0) return 0f;

        float total = 0f;
        int current = sourceId;
        foreach (int next in path)
        {
            var lane = galaxy.GetLanesForSystem(current)
                .FirstOrDefault(l => l.GetOtherSystem(current) == next);
            if (lane != null)
                total += lane.Distance;
            current = next;
        }
        return total;
    }
}
