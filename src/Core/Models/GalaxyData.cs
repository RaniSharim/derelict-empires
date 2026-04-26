using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Exploration;

namespace DerlictEmpires.Core.Models;

/// <summary>
/// The complete galaxy state produced by the generator.
/// Contains all star systems, lanes, and metadata.
/// </summary>
public class GalaxyData
{
    public int Seed { get; set; }
    public List<StarSystemData> Systems { get; set; } = new();
    public List<LaneData> Lanes { get; set; } = new();
    public List<SalvageSiteData> SalvageSites { get; set; } = new();

    /// <summary>Number of spiral arms.</summary>
    public int ArmCount { get; set; }

    /// <summary>Find a salvage site by id. Returns null if not found.</summary>
    public SalvageSiteData? GetSalvageSite(int id) =>
        id >= 0 && id < SalvageSites.Count ? SalvageSites[id] : null;

    /// <summary>Find a system by ID.</summary>
    public StarSystemData? GetSystem(int id) =>
        id >= 0 && id < Systems.Count ? Systems[id] : null;

    /// <summary>
    /// systemId → lanes touching that system. Built lazily on first call to
    /// <see cref="GetLanesForSystem"/> and rebuilt when <see cref="Lanes"/>.Count
    /// drifts from the indexed count. Avoids the per-call <c>Lanes.Where(...)</c>
    /// allocation that <c>FleetMovementSystem.AdvanceFleet</c> would otherwise
    /// pay 10 Hz × moving-fleet-count.
    /// </summary>
    private Dictionary<int, List<LaneData>>? _lanesBySystem;
    private int _indexedLaneCount = -1;
    private static readonly LaneData[] _emptyLanes = Array.Empty<LaneData>();

    /// <summary>Get all lanes connected to a system. O(1) after first call.</summary>
    public IReadOnlyList<LaneData> GetLanesForSystem(int systemId)
    {
        if (_lanesBySystem == null || _indexedLaneCount != Lanes.Count)
            RebuildLaneIndex();
        return _lanesBySystem!.TryGetValue(systemId, out var list) ? list : _emptyLanes;
    }

    /// <summary>
    /// Force-rebuild the lane index. Call after mutating <see cref="Lanes"/>
    /// in place (the lazy rebuild only catches Count changes, not in-place edits
    /// to existing lane endpoints).
    /// </summary>
    public void RebuildLaneIndex()
    {
        var idx = _lanesBySystem ?? new Dictionary<int, List<LaneData>>();
        idx.Clear();
        foreach (var lane in Lanes)
        {
            if (!idx.TryGetValue(lane.SystemA, out var listA))
                idx[lane.SystemA] = listA = new List<LaneData>();
            listA.Add(lane);
            if (lane.SystemA != lane.SystemB)
            {
                if (!idx.TryGetValue(lane.SystemB, out var listB))
                    idx[lane.SystemB] = listB = new List<LaneData>();
                listB.Add(lane);
            }
        }
        _lanesBySystem = idx;
        _indexedLaneCount = Lanes.Count;
    }

    /// <summary>Get IDs of systems directly connected to this one.</summary>
    public IEnumerable<int> GetNeighbors(int systemId) =>
        GetLanesForSystem(systemId).Select(l => l.GetOtherSystem(systemId));
}
