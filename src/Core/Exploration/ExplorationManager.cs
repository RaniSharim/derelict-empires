using System;
using System.Collections.Generic;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// Per-empire per-POI exploration state and scan progress.
/// Pure C#.
/// State mapping for the salvage loop:
///   Undiscovered — POI not visible
///   Discovered   — coarse-known (system entered, yields hidden)
///   Surveyed     — fully scanned (yields visible, extraction allowed)
/// </summary>
public class ExplorationManager
{
    // Key: "empireId_poiId"
    private readonly Dictionary<string, ExplorationState> _states = new();
    private readonly Dictionary<string, int> _surveyDetail = new(); // 0-100 detail level
    private readonly Dictionary<string, float> _scanProgress = new();

    public event Action<int, int>? SystemDiscovered;              // empireId, systemId
    public event Action<int, int>? SiteDiscovered;                // empireId, poiId
    public event Action<int, int, int>? POISurveyed;              // empireId, poiId, detailLevel
    public event Action<int, int, float, float>? ScanProgressChanged; // empireId, poiId, progress, difficulty
    public event Action<int, int>? SiteScanComplete;              // empireId, poiId

    private static string Key(int empireId, int poiId) => $"{empireId}_{poiId}";

    public ExplorationState GetState(int empireId, int poiId) =>
        _states.GetValueOrDefault(Key(empireId, poiId), ExplorationState.Undiscovered);

    public int GetSurveyDetail(int empireId, int poiId) =>
        _surveyDetail.GetValueOrDefault(Key(empireId, poiId));

    public float GetScanProgress(int empireId, int poiId) =>
        _scanProgress.GetValueOrDefault(Key(empireId, poiId));

    /// <summary>Discover all POIs in a system for an empire (fleet enters system).</summary>
    public void DiscoverSystem(int empireId, int systemId, IReadOnlyList<int> poiIds)
    {
        bool anyNew = false;
        foreach (int poiId in poiIds)
        {
            var key = Key(empireId, poiId);
            if (!_states.ContainsKey(key))
            {
                _states[key] = ExplorationState.Discovered;
                SiteDiscovered?.Invoke(empireId, poiId);
                anyNew = true;
            }
        }
        if (anyNew)
            SystemDiscovered?.Invoke(empireId, systemId);
    }

    /// <summary>Force a POI to Surveyed (e.g. home-system pre-reveal). Clears scan progress.</summary>
    public void SurveyPOI(int empireId, int poiId, int detailLevel)
    {
        var key = Key(empireId, poiId);
        _states[key] = ExplorationState.Surveyed;
        _surveyDetail[key] = Math.Max(_surveyDetail.GetValueOrDefault(key), detailLevel);
        _scanProgress.Remove(key);
        POISurveyed?.Invoke(empireId, poiId, detailLevel);
    }

    /// <summary>
    /// Accumulate scan points on a site. Flips Discovered→Surveyed when progress meets
    /// the site's ScanDifficulty, firing SiteScanComplete.
    /// </summary>
    /// <returns>true if this call completed the scan.</returns>
    public bool AdvanceScan(int empireId, int poiId, float difficulty, float deltaPoints)
    {
        var key = Key(empireId, poiId);
        // Only advance if we've at least discovered the POI.
        var state = _states.GetValueOrDefault(key, ExplorationState.Undiscovered);
        if (state == ExplorationState.Surveyed) return false;
        if (state == ExplorationState.Undiscovered)
        {
            _states[key] = ExplorationState.Discovered;
            SiteDiscovered?.Invoke(empireId, poiId);
        }

        float current = _scanProgress.GetValueOrDefault(key) + deltaPoints;
        if (current >= difficulty)
        {
            _states[key] = ExplorationState.Surveyed;
            _scanProgress.Remove(key);
            _surveyDetail[key] = 100;
            ScanProgressChanged?.Invoke(empireId, poiId, difficulty, difficulty);
            SiteScanComplete?.Invoke(empireId, poiId);
            return true;
        }

        _scanProgress[key] = current;
        ScanProgressChanged?.Invoke(empireId, poiId, current, difficulty);
        return false;
    }

    /// <summary>Check if an empire has surveyed a POI.</summary>
    public bool IsSurveyed(int empireId, int poiId) =>
        GetState(empireId, poiId) == ExplorationState.Surveyed;
}
