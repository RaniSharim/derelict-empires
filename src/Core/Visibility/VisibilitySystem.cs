using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Core.Visibility;

/// <summary>
/// Central visibility calculation. Tracks per-empire per-system visibility state
/// and determines detection levels between entities. Pure C#.
/// </summary>
public class VisibilitySystem
{
    /// <summary>Per-empire per-system visibility state.</summary>
    private readonly Dictionary<string, VisibilityState> _systemVisibility = new();

    private static string Key(int empireId, int systemId) => $"{empireId}_{systemId}";

    public VisibilityState GetVisibility(int empireId, int systemId) =>
        _systemVisibility.GetValueOrDefault(Key(empireId, systemId), VisibilityState.Unexplored);

    public void SetVisible(int empireId, int systemId)
    {
        _systemVisibility[Key(empireId, systemId)] = VisibilityState.Visible;
    }

    public void SetExplored(int empireId, int systemId)
    {
        var key = Key(empireId, systemId);
        if (_systemVisibility.GetValueOrDefault(key) != VisibilityState.Visible)
            _systemVisibility[key] = VisibilityState.Explored;
    }

    /// <summary>
    /// Update visibility based on fleet positions and station sensors.
    /// Call each fast tick.
    /// </summary>
    public void UpdateVisibility(
        int empireId,
        IEnumerable<int> ownedSystemIds,
        IEnumerable<int> stationSensorSystems,
        int stationSensorRange,
        GalaxyData galaxy)
    {
        // Dim all visible to explored
        var toExplore = new List<string>();
        foreach (var (key, state) in _systemVisibility)
        {
            if (key.StartsWith($"{empireId}_") && state == VisibilityState.Visible)
                toExplore.Add(key);
        }
        foreach (var key in toExplore)
            _systemVisibility[key] = VisibilityState.Explored;

        // Re-light systems with own fleets
        foreach (int sysId in ownedSystemIds)
            SetVisible(empireId, sysId);

        // Station sensor coverage (extends to adjacent systems)
        foreach (int sysId in stationSensorSystems)
        {
            SetVisible(empireId, sysId);
            if (stationSensorRange >= 1)
            {
                foreach (int neighbor in galaxy.GetNeighbors(sysId))
                    SetVisible(empireId, neighbor);
            }
        }
    }

    /// <summary>
    /// Get all visible systems for an empire.
    /// </summary>
    public List<int> GetVisibleSystems(int empireId)
    {
        var result = new List<int>();
        string prefix = $"{empireId}_";
        foreach (var (key, state) in _systemVisibility)
        {
            if (key.StartsWith(prefix) && state == VisibilityState.Visible)
            {
                if (int.TryParse(key.AsSpan(prefix.Length), out int sysId))
                    result.Add(sysId);
            }
        }
        return result;
    }
}

/// <summary>
/// Calculates detection level between a detector and a target based on
/// their respective visibility and detection range attributes.
/// </summary>
public static class DetectionCalculator
{
    /// <summary>
    /// Determine what level of detection an observer achieves on a target.
    /// </summary>
    /// <param name="targetVisibility">How detectable the target is (higher = easier to spot).</param>
    /// <param name="observerDetectionRange">Observer's sensor capability.</param>
    /// <param name="distance">Distance between observer and target.</param>
    /// <param name="targetSilentRunning">Whether target has suppressed emissions.</param>
    public static DetectionLevel Calculate(
        float targetVisibility,
        float observerDetectionRange,
        float distance,
        bool targetSilentRunning = false)
    {
        if (distance <= 0) return DetectionLevel.Full;

        float effectiveVisibility = targetSilentRunning
            ? targetVisibility * 0.1f  // 90% reduction
            : targetVisibility;

        // Detection score = (visibility * sensorRange) / (distance^2)
        float score = (effectiveVisibility * observerDetectionRange) / (distance * distance);

        return score switch
        {
            >= 10f => DetectionLevel.Full,
            >= 3f => DetectionLevel.Detailed,
            >= 1f => DetectionLevel.Basic,
            >= 0.3f => DetectionLevel.Minimal,
            _ => DetectionLevel.None
        };
    }
}
