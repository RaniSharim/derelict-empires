using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;
using DerlictEmpires.Core.Ships;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// Layered salvage runtime. Sites have 1..5 layers; players scan them sequentially,
/// and after each scan choose to scavenge (drain yield, roll danger) or skip.
/// Once every layer has reached a terminal state (scavenged or skipped) and the
/// site has a special outcome id, that outcome becomes available.
///
/// Activity is per-site (not per-fleet). Any empire-owned fleet in the site's
/// system whose ships have the matching capability (ScanStrength /
/// ExtractionStrength) automatically contributes; capability splits evenly
/// across sibling active sites of the same activity in the same system.
/// </summary>
public class SalvageSystem
{
    private readonly GalaxyData _galaxy;
    private readonly ExplorationManager _exploration;
    private readonly Dictionary<string, ShipDesign> _designsById;
    private readonly int _runSeed;
    private SalvageRegistry? _registry;

    private readonly Dictionary<(int empireId, int poiId), SalvageSiteProgress> _progress = new();

    /// <summary>empireId, poiId, newActivity — site-level activity changed.</summary>
    public event Action<int, int, SiteActivity>? ActivityChanged;

    /// <summary>empireId, poiId — per-tick rate may have changed (sibling toggled, fleet moved).</summary>
    public event Action<int, int>? ActivityRateChanged;

    /// <summary>empireId, poiId, resourceKey, amount extracted this tick.</summary>
    public event Action<int, int, string, float>? YieldExtracted;

    /// <summary>empireId, poiId, layerIndex, progress, difficulty — per-tick scan progress for a layer.</summary>
    public event Action<int, int, int, float, float>? LayerScanProgressChanged;

    /// <summary>empireId, poiId, layerIndex — a layer's scan finished.</summary>
    public event Action<int, int, int>? LayerScanned;

    /// <summary>empireId, poiId, layerIndex — a layer was fully scavenged (yield drained).</summary>
    public event Action<int, int, int>? LayerScavenged;

    /// <summary>empireId, poiId, layerIndex — a layer was explicitly skipped.</summary>
    public event Action<int, int, int>? LayerSkipped;

    /// <summary>empireId, poiId, layerIndex, dangerTypeId, severity — danger triggered.</summary>
    public event Action<int, int, int, string, float>? DangerTriggered;

    /// <summary>empireId, poiId, layerIndex — research roll landed (subsystemId via progress).</summary>
    public event Action<int, int, int>? ResearchUnlocked;

    /// <summary>empireId, poiId, outcomeId — final layer revealed, special action available.</summary>
    public event Action<int, int, string>? SpecialOutcomeReady;

    /// <summary>empireId, poiId, resolution — the outcome action was paid for and resolved.</summary>
    public event Action<int, int, SalvageOutcomeProcessor.Resolution>? SpecialOutcomeResolved;

    /// <summary>Remaining-fraction mark where per-tick extraction rate visibly drops.</summary>
    public const float InflectionRemainingFraction = 0.25f;

    public SalvageSystem(
        GalaxyData galaxy,
        ExplorationManager exploration,
        IReadOnlyDictionary<string, ShipDesign> designsById,
        int runSeed = 0,
        SalvageRegistry? registry = null)
    {
        _galaxy = galaxy;
        _exploration = exploration;
        _designsById = new Dictionary<string, ShipDesign>(designsById);
        _runSeed = runSeed;
        _registry = registry;
    }

    public void SetRegistry(SalvageRegistry registry) => _registry = registry;

    // ── Query API ─────────────────────────────────────────────────

    public SiteActivity GetActivity(int empireId, int poiId) =>
        _progress.TryGetValue((empireId, poiId), out var p) ? p.Activity : SiteActivity.None;

    public bool IsScanning(int empireId, int poiId)   => GetActivity(empireId, poiId) == SiteActivity.Scanning;
    public bool IsExtracting(int empireId, int poiId) => GetActivity(empireId, poiId) == SiteActivity.Extracting;

    public SalvageSiteProgress? GetProgress(int empireId, int poiId) =>
        _progress.GetValueOrDefault((empireId, poiId));

    /// <summary>All POIs this empire is currently scanning OR scavenging, filtered by type.</summary>
    public List<int> GetActivePOIs(int empireId, SiteActivity type)
    {
        var result = new List<int>();
        foreach (var kv in _progress)
            if (kv.Key.empireId == empireId && kv.Value.Activity == type)
                result.Add(kv.Key.poiId);
        return result;
    }

    /// <summary>Count of sibling active sites of the same type in the same system.</summary>
    public int CountActiveInSystem(int empireId, int systemId, SiteActivity type)
    {
        int n = 0;
        foreach (var kv in _progress)
        {
            if (kv.Key.empireId != empireId || kv.Value.Activity != type) continue;
            if (TryGetPoiSystem(kv.Key.poiId, out int sid) && sid == systemId) n++;
        }
        return n;
    }

    /// <summary>Aggregate capability of empire fleets in a given system.</summary>
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

    /// <summary>Fleets currently contributing to the active activity at a site.</summary>
    public List<FleetData> GetContributingFleets(
        int empireId, int poiId,
        IReadOnlyList<FleetData> fleets,
        IReadOnlyDictionary<int, ShipInstanceData> shipsById)
    {
        var result = new List<FleetData>();
        if (!_progress.TryGetValue((empireId, poiId), out var p) || p.Activity == SiteActivity.None)
            return result;
        if (!TryGetPoiSystem(poiId, out int sysId)) return result;
        foreach (var fleet in fleets)
        {
            if (fleet.OwnerEmpireId != empireId) continue;
            if (fleet.CurrentSystemId != sysId) continue;
            if (AggregateFleetStrength(fleet, shipsById, p.Activity) > 0f)
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

        foreach (var kv in _progress)
        {
            if (kv.Key.empireId != fleet.OwnerEmpireId) continue;
            var p = kv.Value;
            if (p.Activity == SiteActivity.None) continue;
            if (!TryGetPoiSystem(kv.Key.poiId, out int sid) || sid != fleet.CurrentSystemId) continue;
            if (p.Activity == SiteActivity.Scanning && scanCap > 0f) scans.Add(kv.Key.poiId);
            else if (p.Activity == SiteActivity.Extracting && extractCap > 0f) extracts.Add(kv.Key.poiId);
        }
        return (scans, extracts);
    }

    // ── Order mutation ────────────────────────────────────────────

    /// <summary>
    /// Start scanning the active layer (or resume — scan progress persists if previously stopped).
    /// Requires the site to be Discovered. Returns true if state changed.
    /// </summary>
    public bool RequestScan(int empireId, int poiId)
    {
        if (_exploration.GetState(empireId, poiId) == ExplorationState.Undiscovered) return false;
        var site = FindSite(poiId);
        if (site == null) return false;

        var p = GetOrCreate(empireId, poiId, site);
        if (p.ActiveLayerIndex >= p.LayerCount) return false; // all layers terminal
        if (p.LayerScanned[p.ActiveLayerIndex]) return false; // already scanned, must scavenge or skip first
        if (p.Activity == SiteActivity.Scanning) return false;

        var prev = p.Activity;
        p.Activity = SiteActivity.Scanning;
        ActivityChanged?.Invoke(empireId, poiId, p.Activity);
        NotifyRateChangeForSiblings(empireId, poiId, prev, p.Activity);
        return true;
    }

    /// <summary>
    /// Start scavenging the active layer. Requires that layer to be scanned and
    /// not yet scavenged or skipped.
    /// </summary>
    public bool RequestScavenge(int empireId, int poiId)
    {
        var site = FindSite(poiId);
        if (site == null) return false;
        if (!_progress.TryGetValue((empireId, poiId), out var p)) return false;
        if (p.ActiveLayerIndex >= p.LayerCount) return false;
        int idx = p.ActiveLayerIndex;
        if (!p.LayerScanned[idx]) return false;
        if (p.LayerScavenged[idx] || p.LayerSkipped[idx]) return false;
        if (p.Activity == SiteActivity.Extracting) return false;

        var prev = p.Activity;
        p.Activity = SiteActivity.Extracting;

        // Roll danger on first transition into scavenge for this layer (if not yet rolled).
        if (!p.DangerTriggered[idx])
        {
            var layer = site.Layers[idx];
            var dangerRng = MakeLayerRng(site.Id, idx, empireId, "danger");
            p.DangerTriggered[idx] = true;
            if (dangerRng.Chance(layer.DangerChance))
                DangerTriggered?.Invoke(empireId, poiId, idx, layer.DangerTypeId, layer.DangerSeverity);
        }

        ActivityChanged?.Invoke(empireId, poiId, p.Activity);
        NotifyRateChangeForSiblings(empireId, poiId, prev, p.Activity);
        return true;
    }

    /// <summary>
    /// Mark the active layer skipped (no scavenge, no yield). Layer must be
    /// scanned and not yet terminal.
    /// </summary>
    public bool RequestSkip(int empireId, int poiId)
    {
        var site = FindSite(poiId);
        if (site == null) return false;
        if (!_progress.TryGetValue((empireId, poiId), out var p)) return false;
        if (p.ActiveLayerIndex >= p.LayerCount) return false;
        int idx = p.ActiveLayerIndex;
        if (!p.LayerScanned[idx]) return false;
        if (p.LayerScavenged[idx] || p.LayerSkipped[idx]) return false;

        var prev = p.Activity;
        p.LayerSkipped[idx] = true;
        p.Activity = SiteActivity.None;
        AdvanceActiveLayer(p, site);

        LayerSkipped?.Invoke(empireId, poiId, idx);
        ActivityChanged?.Invoke(empireId, poiId, p.Activity);
        NotifyRateChangeForSiblings(empireId, poiId, prev, p.Activity);
        return true;
    }

    /// <summary>Stop the current activity. Scan progress is preserved across stops.</summary>
    public bool RequestStop(int empireId, int poiId)
    {
        if (!_progress.TryGetValue((empireId, poiId), out var p)) return false;
        if (p.Activity == SiteActivity.None) return false;
        var prev = p.Activity;
        p.Activity = SiteActivity.None;
        ActivityChanged?.Invoke(empireId, poiId, p.Activity);
        NotifyRateChangeForSiblings(empireId, poiId, prev, p.Activity);
        return true;
    }

    /// <summary>
    /// Pay for and apply a site's special outcome. Returns the resolution describing
    /// what happened (success/failure + payload). Does NOT mutate StationSystem /
    /// derelict storage — the host listens to <see cref="SpecialOutcomeResolved"/>
    /// and integrates the spawned station/derelict into world state.
    /// </summary>
    public SalvageOutcomeProcessor.Resolution RequestSpecialOutcome(EmpireData empire, int poiId)
    {
        if (_registry == null)
            return SalvageOutcomeProcessor.Resolution.Failure("registry not loaded");
        var site = FindSite(poiId);
        if (site == null)
            return SalvageOutcomeProcessor.Resolution.Failure("site not found");
        if (!_progress.TryGetValue((empire.Id, poiId), out var p))
            return SalvageOutcomeProcessor.Resolution.Failure("no progress for site");
        if (!TryGetPoiSystem(poiId, out int sysId))
            return SalvageOutcomeProcessor.Resolution.Failure("system lookup failed");

        var resolution = SalvageOutcomeProcessor.Resolve(empire, site, p, _registry, sysId);
        if (resolution.Success)
            SpecialOutcomeResolved?.Invoke(empire.Id, poiId, resolution);
        return resolution;
    }

    /// <summary>Cancel every activity this empire has on any site.</summary>
    public void CancelAll(int empireId)
    {
        var changed = new List<int>();
        foreach (var kv in _progress)
        {
            if (kv.Key.empireId != empireId) continue;
            if (kv.Value.Activity == SiteActivity.None) continue;
            kv.Value.Activity = SiteActivity.None;
            changed.Add(kv.Key.poiId);
        }
        foreach (int pid in changed)
            ActivityChanged?.Invoke(empireId, pid, SiteActivity.None);
    }

    // ── Tick processing ──────────────────────────────────────────

    public void ProcessTick(
        float delta,
        IReadOnlyList<FleetData> fleets,
        IReadOnlyDictionary<int, ShipInstanceData> shipsById,
        IReadOnlyDictionary<int, EmpireData> empiresById)
    {
        if (_progress.Count == 0 || delta <= 0f) return;

        // Group active progresses by (empire, system, type) to split capability evenly.
        var groups = new Dictionary<(int empireId, int systemId, SiteActivity type), List<int>>();
        foreach (var kv in _progress)
        {
            var p = kv.Value;
            if (p.Activity == SiteActivity.None) continue;
            if (!TryGetPoiSystem(kv.Key.poiId, out int sysId)) continue;
            var gk = (p.EmpireId, sysId, p.Activity);
            if (!groups.TryGetValue(gk, out var list))
                groups[gk] = list = new List<int>();
            list.Add(kv.Key.poiId);
        }

        foreach (var (gk, poiIds) in groups)
        {
            if (!empiresById.TryGetValue(gk.empireId, out var empire)) continue;
            float totalCap = ComputeSystemCapability(gk.empireId, gk.systemId, gk.type, fleets, shipsById);
            if (totalCap <= 0f) continue;
            float perSite = totalCap / poiIds.Count;

            if (gk.type == SiteActivity.Scanning)
                ProcessScanGroup(empire, poiIds, perSite, delta);
            else if (gk.type == SiteActivity.Extracting)
                ProcessExtractGroup(empire, poiIds, perSite, delta);
        }
    }

    private void ProcessScanGroup(EmpireData empire, List<int> poiIds, float perSite, float delta)
    {
        // Snapshot — completion may mutate _progress (advancing active layer).
        foreach (int poiId in poiIds.ToList())
        {
            var site = FindSite(poiId);
            if (site == null) continue;
            if (!_progress.TryGetValue((empire.Id, poiId), out var p)) continue;
            if (p.ActiveLayerIndex >= p.LayerCount) continue;
            int idx = p.ActiveLayerIndex;
            if (p.LayerScanned[idx]) continue;

            var layer = site.Layers[idx];
            float gained = perSite * delta;
            p.LayerScanProgress[idx] += gained;

            // Whole-site scan progress also drives ExplorationManager so the legacy
            // Discovered→Surveyed flip still fires when the first layer finishes.
            _exploration.AdvanceScan(empire.Id, poiId, layer.ScanDifficulty, gained);

            LayerScanProgressChanged?.Invoke(
                empire.Id, poiId, idx, p.LayerScanProgress[idx], layer.ScanDifficulty);

            if (p.LayerScanProgress[idx] >= layer.ScanDifficulty)
            {
                p.LayerScanProgress[idx] = layer.ScanDifficulty;
                p.LayerScanned[idx] = true;
                p.Activity = SiteActivity.None;

                // Research unlock roll — deterministic per (siteId, layerIndex, empireId).
                var rng = MakeLayerRng(site.Id, idx, empire.Id, "research");
                if (rng.Chance(layer.ResearchUnlockChance))
                {
                    p.ResearchUnlocked[idx] = true;
                    ResearchUnlocked?.Invoke(empire.Id, poiId, idx);
                }

                LayerScanned?.Invoke(empire.Id, poiId, idx);
                ActivityChanged?.Invoke(empire.Id, poiId, p.Activity);
            }
        }
    }

    private void ProcessExtractGroup(EmpireData empire, List<int> poiIds, float perSite, float delta)
    {
        foreach (int poiId in poiIds.ToList())
        {
            var site = FindSite(poiId);
            if (site == null) continue;
            if (!_progress.TryGetValue((empire.Id, poiId), out var p)) continue;
            if (p.ActiveLayerIndex >= p.LayerCount) continue;
            int idx = p.ActiveLayerIndex;
            if (!p.LayerScanned[idx] || p.LayerScavenged[idx] || p.LayerSkipped[idx]) continue;

            var layer = site.Layers[idx];
            var yields = ExtractionCalculator.PerTickYield(
                layer.Yield, layer.RemainingYield, perSite, site.DepletionCurveExponent, delta);

            foreach (var kv in yields)
            {
                float remaining = layer.RemainingYield.GetValueOrDefault(kv.Key) - kv.Value;
                layer.RemainingYield[kv.Key] = MathF.Max(0f, remaining);
                empire.ResourceStockpile[kv.Key] = empire.ResourceStockpile.GetValueOrDefault(kv.Key) + kv.Value;
                YieldExtracted?.Invoke(empire.Id, poiId, kv.Key, kv.Value);
            }

            if (LayerDepleted(layer))
            {
                p.LayerScavenged[idx] = true;
                p.Activity = SiteActivity.None;
                AdvanceActiveLayer(p, site);
                LayerScavenged?.Invoke(empire.Id, poiId, idx);
                ActivityChanged?.Invoke(empire.Id, poiId, p.Activity);
            }
        }
    }

    private static bool LayerDepleted(SalvageLayer layer)
    {
        foreach (var v in layer.RemainingYield.Values)
            if (v > 0.01f) return false;
        return true;
    }

    /// <summary>Advance ActiveLayerIndex past terminal layers; flip SpecialOutcome if appropriate.</summary>
    private void AdvanceActiveLayer(SalvageSiteProgress p, SalvageSiteData site)
    {
        while (p.ActiveLayerIndex < p.LayerCount && p.LayerTerminal(p.ActiveLayerIndex))
            p.ActiveLayerIndex++;

        if (p.ActiveLayerIndex >= p.LayerCount &&
            !string.IsNullOrEmpty(site.SpecialOutcomeId) &&
            !p.SpecialOutcomeAvailable && !p.SpecialOutcomeConsumed)
        {
            p.SpecialOutcomeAvailable = true;
            SpecialOutcomeReady?.Invoke(p.EmpireId, p.POIId, site.SpecialOutcomeId!);
        }
    }

    /// <summary>Notify after a fleet move — rate-dependent UI re-reads capability.</summary>
    public void NotifyFleetMovedSystem(int empireId, int affectedSystemId)
    {
        foreach (var kv in _progress)
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

        foreach (var kv in _progress)
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

    private SalvageSiteData? FindSite(int poiId)
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
                SiteActivity.Scanning   => design.ScanStrength,
                SiteActivity.Extracting => design.ExtractionStrength,
                _                       => 0f,
            };
        }
        return total;
    }

    private SalvageSiteProgress GetOrCreate(int empireId, int poiId, SalvageSiteData site)
    {
        var key = (empireId, poiId);
        if (_progress.TryGetValue(key, out var existing)) return existing;
        var fresh = SalvageSiteProgress.ForSite(empireId, poiId, site.Layers.Count);
        _progress[key] = fresh;
        return fresh;
    }

    private GameRandom MakeLayerRng(int siteId, int layerIndex, int empireId, string differentiator)
    {
        // Stable per (run, site, layer, empire, purpose) so a save/load round-trip
        // doesn't change the outcome of a roll already committed.
        var rng = new GameRandom(_runSeed);
        return rng.DeriveChild(siteId)
                  .DeriveChild(layerIndex)
                  .DeriveChild(empireId)
                  .DeriveChild(differentiator);
    }

    // ── Save/load hooks ──────────────────────────────────────────

    public IReadOnlyDictionary<(int empireId, int poiId), SalvageSiteProgress> AllProgress => _progress;

    public void RestoreProgress(SalvageSiteProgress progress) =>
        _progress[(progress.EmpireId, progress.POIId)] = progress;
}
