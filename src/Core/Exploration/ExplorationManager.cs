using System;
using System.Collections.Generic;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// Manages per-empire per-POI exploration state (Undiscovered/Discovered/Surveyed).
/// Pure C#.
/// </summary>
public class ExplorationManager
{
    // Key: "empireId_poiId"
    private readonly Dictionary<string, ExplorationState> _states = new();
    private readonly Dictionary<string, int> _surveyDetail = new(); // 0-100 detail level

    public event Action<int, int>? SystemDiscovered; // empireId, systemId
    public event Action<int, int, int>? POISurveyed; // empireId, poiId, detailLevel

    private static string Key(int empireId, int poiId) => $"{empireId}_{poiId}";

    public ExplorationState GetState(int empireId, int poiId) =>
        _states.GetValueOrDefault(Key(empireId, poiId), ExplorationState.Undiscovered);

    public int GetSurveyDetail(int empireId, int poiId) =>
        _surveyDetail.GetValueOrDefault(Key(empireId, poiId));

    /// <summary>Discover all POIs in a system for an empire (scout enters system).</summary>
    public void DiscoverSystem(int empireId, int systemId, IReadOnlyList<int> poiIds)
    {
        bool anyNew = false;
        foreach (int poiId in poiIds)
        {
            var key = Key(empireId, poiId);
            if (!_states.ContainsKey(key))
            {
                _states[key] = ExplorationState.Discovered;
                anyNew = true;
            }
        }
        if (anyNew)
            SystemDiscovered?.Invoke(empireId, systemId);
    }

    /// <summary>Survey a specific POI. Detail level depends on tech (0-100).</summary>
    public void SurveyPOI(int empireId, int poiId, int detailLevel)
    {
        var key = Key(empireId, poiId);
        _states[key] = ExplorationState.Surveyed;
        _surveyDetail[key] = Math.Max(_surveyDetail.GetValueOrDefault(key), detailLevel);
        POISurveyed?.Invoke(empireId, poiId, detailLevel);
    }

    /// <summary>Check if an empire has surveyed a POI.</summary>
    public bool IsSurveyed(int empireId, int poiId) =>
        GetState(empireId, poiId) == ExplorationState.Surveyed;
}
