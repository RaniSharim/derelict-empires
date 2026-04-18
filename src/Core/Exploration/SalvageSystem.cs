using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Ships;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// Site-level activity runtime for scan and extract. Pure C#.
///
/// Scanning and Extracting are per-empire per-site toggles, not fleet orders.
/// Any empire fleet in the POI's system whose ships have the relevant capability
/// (ScanStrength / ExtractionStrength) automatically contributes. When multiple
/// sites in a system have the same activity active for the same empire, the
/// system's total capability splits evenly across them.
///
/// If no capable fleets are in-system, active activities stall (state preserved,
/// no progress this tick). Fleets arriving/leaving trigger a natural re-compute
/// next tick.
/// </summary>
public class SalvageSystem
{
    private readonly GalaxyData _galaxy;
    private readonly ExplorationManager _exploration;
    private readonly Dictionary<string, ShipDesign> _designsById;

    private readonly Dictionary<(int empireId, int poiId), SalvageSiteActivity> _activities = new();

    /// <summary>empireId, poiId, newActivity — fired when a site's activity flips on or off.</summary>
    public event Action<int, int, SiteActivity>? ActivityChanged;

    /// <summary>empireId, poiId — a site whose per-tick rate may have changed (sibling started/stopped, fleet arrived/left).</summary>
    public event Action<int, int>? ActivityRateChanged;

    /// <summary>empireId, poiId, resourceKey, amount extracted this tick.</summary>
    public event Action<int, int, string, float>? YieldExtracted;

    /// <summary>
    /// Remaining-fraction mark where per-tick extraction rate visibly drops. UI draws
    /// a tick on yield bars here to cue the player that diminishing returns are kicking in.
    /// </summary>
    public const float InflectionRemainingFraction = 0.25f;

    public SalvageSystem(
        GalaxyData galaxy,
        ExplorationManager exploration,
        IReadOnlyDictionary<string, ShipDesign> designsById)
    {
        _galaxy = galaxy;
        _exploration = exploration;
        _designsById = new Dictionary<string, ShipDesign>(designsById);
        _exploration.SiteScanComplete += OnSiteScanComplete;
    }

    // ── Query API ─────────────────────────────────────────────────

    public SiteActivity GetActivity(int empireId, int poiId) =>
        _activities.TryGetValue((empireId, poiId), out var a) ? a.Activity : SiteActivity.None;

    public bool IsScanning(int empireId, int poiId) => GetActivity(empireId, poiId) == SiteActivity.Scanning;
    public bool IsExtracting(int empireId, int poiId) => GetActivity(empireId, poiId) == SiteActivity.Extracting;

    /// <summary>All active (poiId) for this empire filtered by activity type. Allocates.</summary>
    public List<int> GetActivePOIs(int empireId, SiteActivity type)
    {
        var result = new List<int>();
        foreach (var kv in _activities)
            if (kv.Key.empireId == empireId && kv.Value.Activity == type)
                result.Add(kv.Key.poiId);
        return result;
    }

    /// <summary>Count of sibling activities of the same type in the same system for this empire.</summary>
    public int CountActiveInSystem(int empireId, int systemId, SiteActivity type)
    {
        int n = 0;
        foreach (var kv in _activities)
        {
            if (kv.Key.empireId != empireId || kv.Value.Activity != type) continue;
            if (TryGetPoiSystem(kv.Key.poiId, out int sid) && sid == systemId) n++;
        }
        return n;
    }

    /// <summary>Aggregate capability across empire fleets in the given system.</summary>
    public float ComputeSystemCapability(
        int empireId, int systemId, SiteActivity type,
        IReadOnlyList<FleetData> fleets,
        IReadOnlyDictionary<int, ShipInstanceData> shipsById)
    {
        float total = 0f;
        foreach (var fleet in fleets)
        {
            if (fleet.OwnerEmpireId != empireId) continue;
            if (fleet.CurrentSystemId != systemId) continue;
            total += AggregateFleetStrength(fleet, shipsById, type);
        }
        return total;
    }

    /// <summary>Fleets currently contributing to the active site (empire, in-system, with capability).</summary>
    public List<FleetData> GetContributingFleets(
        int empireId, int poiId,
        IReadOnlyList<FleetData> fleets,
        IReadOnlyDictionary<int, ShipInstanceData> shipsById)
    {
        var result = new List<FleetData>();
        if (!_activities.TryGetValue((empireId, poiId), out var a) || a.Activity == SiteActivity.None)
            return result;
        if (!TryGetPoiSystem(poiId, out int sysId)) return result;
        foreach (var fleet in fleets)
        {
            if (fleet.OwnerEmpireId != empireId) continue;
            if (fleet.CurrentSystemId != sysId) continue;
            if (AggregateFleetStrength(fleet, shipsById, a.Activity) > 0f)
                result.Add(fleet);
        }
        return result;
    }

    /// <summary>Active sites a fleet is contributing to, split by activity type.</summary>
    public (List<int> scanning, List<int> extracting) GetFleetContributions(
        FleetData fleet,
        IReadOnlyDictionary<int, ShipInstanceData> shipsById)
    {
        var scans = new List<int>();
        var extracts = new List<int>();
        if (fleet.CurrentSystemId < 0) return (scans, extracts);

        float scanCap = AggregateFleetStrength(fleet, shipsById, SiteActivity.Scanning);
        float extractCap = AggregateFleetStrength(fleet, shipsById, SiteActivity.Extracting);

        foreach (var kv in _activities)
        {
            if (kv.Key.empireId != fleet.OwnerEmpireId) continue;
            var a = kv.Value;
            if (a.Activity == SiteActivity.None) continue;
            if (!TryGetPoiSystem(kv.Key.poiId, out int sid) || sid != fleet.CurrentSystemId) continue;
            if (a.Activity == SiteActivity.Scanning && scanCap > 0f) scans.Add(kv.Key.poiId);
            else if (a.Activity == SiteActivity.Extracting && extractCap > 0f) extracts.Add(kv.Key.poiId);
        }
        return (scans, extracts);
    }

    // ── Order mutation ────────────────────────────────────────────

    /// <summary>
    /// Start or stop an activity for this empire on this site. Returns true on state change.
    /// Validates against exploration state: Scanning requires Discovered (not Surveyed),
    /// Extracting requires Surveyed.
    /// </summary>
    public bool RequestActivity(int empireId, int poiId, SiteActivity newActivity)
    {
        if (newActivity == SiteActivity.Scanning)
        {
            var state = _exploration.GetState(empireId, poiId);
            if (state != ExplorationState.Discovered) return false;
        }
        else if (newActivity == SiteActivity.Extracting)
        {
            if (!_exploration.IsSurveyed(empireId, poiId)) return false;
            // Reject extract-toggle on an already-depleted site.
            var site = FindSiteForPoi(poiId);
            if (site != null && IsSiteDepleted(site)) return false;
        }

        var key = (empireId, poiId);
        _activities.TryGetValue(key, out var current);
        var prev = current?.Activity ?? SiteActivity.None;
        if (prev == newActivity) return false;

        if (newActivity == SiteActivity.None)
        {
            if (current != null)
            {
                if (prev == SiteActivity.Scanning && _exploration.GetScanProgress(empireId, poiId) > 0f)
                    current.Activity = SiteActivity.None;     // preserve record so progress persists naturally
                else
                    _activities.Remove(key);
            }
        }
        else
        {
            if (current == null)
            {
                _activities[key] = new SalvageSiteActivity
                {
                    EmpireId = empireId,
                    POIId = poiId,
                    Activity = newActivity,
                };
            }
            else
            {
                current.Activity = newActivity;
            }
        }

        ActivityChanged?.Invoke(empireId, poiId, newActivity);
        NotifyRateChangeForSiblings(empireId, poiId, prev, newActivity);
        return true;
    }

    /// <summary>Cancel every activity this empire has on any site.</summary>
    public void CancelAll(int empireId)
    {
        var toFire = new List<int>();
        foreach (var kv in _activities.Where(k => k.Key.empireId == empireId && k.Value.Activity != SiteActivity.None).ToList())
        {
            kv.Value.Activity = SiteActivity.None;
            toFire.Add(kv.Key.poiId);
            if (_exploration.GetScanProgress(empireId, kv.Key.poiId) <= 0f)
                _activities.Remove(kv.Key);
        }
        foreach (int pid in toFire)
            ActivityChanged?.Invoke(empireId, pid, SiteActivity.None);
    }

    // ── Tick processing ──────────────────────────────────────────

    public void ProcessTick(
        float delta,
        IReadOnlyList<FleetData> fleets,
        IReadOnlyDictionary<int, ShipInstanceData> shipsById,
        IReadOnlyDictionary<int, EmpireData> empiresById)
    {
        if (_activities.Count == 0 || delta <= 0f) return;

        var groups = new Dictionary<(int empireId, int systemId, SiteActivity type), List<int>>();
        foreach (var kv in _activities)
        {
            var a = kv.Value;
            if (a.Activity == SiteActivity.None) continue;
            if (!TryGetPoiSystem(kv.Key.poiId, out int sysId)) continue;
            var gk = (a.EmpireId, sysId, a.Activity);
            if (!groups.TryGetValue(gk, out var list))
                groups[gk] = list = new List<int>();
            list.Add(kv.Key.poiId);
        }

        foreach (var (gk, poiIds) in groups)
        {
            if (!empiresById.TryGetValue(gk.empireId, out var empire)) continue;
            float totalCap = ComputeSystemCapability(gk.empireId, gk.systemId, gk.type, fleets, shipsById);
            if (totalCap <= 0f) continue;   // stall — preserve state, no progress
            int n = poiIds.Count;
            float perSite = totalCap / n;

            if (gk.type == SiteActivity.Scanning)
                ProcessScanGroup(empire, poiIds, perSite, delta);
            else if (gk.type == SiteActivity.Extracting)
                ProcessExtractGroup(empire, poiIds, perSite, delta);
        }
    }

    private void ProcessScanGroup(EmpireData empire, List<int> poiIds, float perSite, float delta)
    {
        // Copy to a snapshot because AdvanceScan may trigger OnSiteScanComplete which mutates _activities.
        foreach (int poiId in poiIds)
        {
            var site = FindSiteForPoi(poiId);
            if (site == null) continue;
            _exploration.AdvanceScan(empire.Id, poiId, site.ScanDifficulty, perSite * delta);
        }
    }

    private void ProcessExtractGroup(EmpireData empire, List<int> poiIds, float perSite, float delta)
    {
        foreach (int poiId in poiIds)
        {
            var site = FindSiteForPoi(poiId);
            if (site == null) continue;
            var yields = ExtractionCalculator.PerTickYield(
                site.TotalYield, site.RemainingYield, perSite, site.DepletionCurveExponent, delta);
            foreach (var kv in yields)
            {
                site.RemainingYield[kv.Key] = MathF.Max(0f, site.RemainingYield.GetValueOrDefault(kv.Key) - kv.Value);
                empire.ResourceStockpile[kv.Key] = empire.ResourceStockpile.GetValueOrDefault(kv.Key) + kv.Value;
                YieldExtracted?.Invoke(empire.Id, site.POIId, kv.Key, kv.Value);
            }

            // Auto-stop when the site is fully depleted. UI flips back to the scanned
            // yield-bars view (all 0/N) and the player can move on.
            if (IsSiteDepleted(site) && _activities.Remove((empire.Id, poiId)))
            {
                ActivityChanged?.Invoke(empire.Id, poiId, SiteActivity.None);
                NotifyRateChangeForSiblings(empire.Id, poiId, SiteActivity.Extracting, SiteActivity.None);
            }
        }
    }

    private static bool IsSiteDepleted(SalvageSiteData site)
    {
        foreach (var v in site.RemainingYield.Values)
            if (v > 0.01f) return false;
        return true;
    }

    /// <summary>
    /// Called by the fleet-arrival pipeline so rate-dependent UI can refresh. We don't
    /// have a matching "fleet left system" event, so callers should also invoke this
    /// when they observe a departure (or rely on per-tick re-render).
    /// </summary>
    public void NotifyFleetMovedSystem(int empireId, int affectedSystemId)
    {
        foreach (var kv in _activities)
        {
            if (kv.Key.empireId != empireId) continue;
            if (kv.Value.Activity == SiteActivity.None) continue;
            if (!TryGetPoiSystem(kv.Key.poiId, out int sid) || sid != affectedSystemId) continue;
            ActivityRateChanged?.Invoke(empireId, kv.Key.poiId);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────

    private void NotifyRateChangeForSiblings(int empireId, int poiId, SiteActivity prev, SiteActivity next)
    {
        ActivityRateChanged?.Invoke(empireId, poiId);
        if (!TryGetPoiSystem(poiId, out int sysId)) return;

        foreach (var kv in _activities)
        {
            if (kv.Key.empireId != empireId) continue;
            if (kv.Key.poiId == poiId) continue;
            var t = kv.Value.Activity;
            if (t == SiteActivity.None) continue;
            if (t != prev && t != next) continue;
            if (!TryGetPoiSystem(kv.Key.poiId, out int sid) || sid != sysId) continue;
            ActivityRateChanged?.Invoke(empireId, kv.Key.poiId);
        }
    }

    private bool TryGetPoiSystem(int poiId, out int systemId)
    {
        systemId = -1;
        foreach (var system in _galaxy.Systems)
            foreach (var poi in system.POIs)
                if (poi.Id == poiId) { systemId = system.Id; return true; }
        return false;
    }

    private SalvageSiteData? FindSiteForPoi(int poiId)
    {
        foreach (var system in _galaxy.Systems)
            foreach (var poi in system.POIs)
                if (poi.Id == poiId)
                    return poi.SalvageSiteId is int sid ? _galaxy.GetSalvageSite(sid) : null;
        return null;
    }

    private float AggregateFleetStrength(
        FleetData fleet,
        IReadOnlyDictionary<int, ShipInstanceData> shipsById,
        SiteActivity type)
    {
        float total = 0f;
        foreach (int shipId in fleet.ShipIds)
        {
            if (!shipsById.TryGetValue(shipId, out var ship)) continue;
            if (!_designsById.TryGetValue(ship.ShipDesignId, out var design)) continue;
            total += type switch
            {
                SiteActivity.Scanning => design.ScanStrength,
                SiteActivity.Extracting => design.ExtractionStrength,
                _ => 0f,
            };
        }
        return total;
    }

    private void OnSiteScanComplete(int empireId, int poiId)
    {
        if (_activities.Remove((empireId, poiId)))
        {
            ActivityChanged?.Invoke(empireId, poiId, SiteActivity.None);
            NotifyRateChangeForSiblings(empireId, poiId, SiteActivity.Scanning, SiteActivity.None);
        }
    }

    // ── Save/load hooks ──────────────────────────────────────────

    public IReadOnlyDictionary<(int empireId, int poiId), SalvageSiteActivity> AllActivities => _activities;

    public void RestoreActivity(int empireId, int poiId, SiteActivity activity)
    {
        if (activity == SiteActivity.None) return;
        _activities[(empireId, poiId)] = new SalvageSiteActivity
        {
            EmpireId = empireId,
            POIId = poiId,
            Activity = activity,
        };
    }
}
