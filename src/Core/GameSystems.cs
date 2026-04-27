using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;
using DerlictEmpires.Core.Settlements;
using DerlictEmpires.Core.Ships;
using DerlictEmpires.Core.Stations;
using DerlictEmpires.Core.Systems;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Core;

/// <summary>
/// Composition root for all game-logic systems. Plain C# — no Godot dependencies.
/// Constructable in tests: build the GameSystems, call the Load* methods needed by the test,
/// then drive Tick(...) directly. The Godot side wraps this in <c>GameSystemsHost</c>, which
/// pumps EventBus FastTick into Tick(...) and bridges this object's events back onto EventBus.
///
/// Lifecycle:
///   1. <see cref="LoadMovement"/> after a galaxy exists.
///   2. <see cref="LoadResearch"/>, <see cref="LoadExploration"/>, <see cref="LoadSalvage"/>,
///      <see cref="LoadSettlements"/>, <see cref="LoadStations"/>, <see cref="LoadExtraction"/>
///      in any order.
///   3. <see cref="Tick"/> each fast tick; cross-system orchestration (e.g. salvage rate refresh
///      on fleet arrival) runs through the events on this class.
/// </summary>
public class GameSystems
{
    // ── Logic systems (initialized by Load* calls; null before then) ─────
    public FleetMovementSystem      Movement     { get; private set; } = null!;
    public ResourceExtractionSystem Extraction   { get; private set; } = null!;
    public SettlementSystem         Settlements  { get; private set; } = null!;
    public StationSystem            Stations     { get; private set; } = null!;
    public ExplorationManager       Exploration  { get; private set; } = null!;
    public SalvageSystem            Salvage      { get; private set; } = null!;
    public TechTreeRegistry         TechRegistry { get; private set; } = null!;
    public ResearchEngine           Research     { get; private set; } = null!;

    private readonly Dictionary<int, EmpireResearchState> _researchStates = new();
    public IReadOnlyDictionary<int, EmpireResearchState> ResearchStates => _researchStates;

    public EmpireResearchState? GetResearchState(int empireId) =>
        _researchStates.GetValueOrDefault(empireId);

    public void SetResearchState(int empireId, EmpireResearchState state) =>
        _researchStates[empireId] = state;

    public void ClearResearchStates() => _researchStates.Clear();

    // ── Lifecycle ────────────────────────────────────────────────

    public void LoadMovement(GalaxyData galaxy)
    {
        Movement = new FleetMovementSystem(galaxy);
        Movement.FleetArrived += (fleet, sysId) => FleetArrived?.Invoke(fleet, sysId);
        Movement.FleetDeparted += (fleet, fromSysId) => FleetDeparted?.Invoke(fleet, fromSysId);
        Movement.OrderCompleted += fleet => FleetOrderCompleted?.Invoke(fleet);
    }

    public void LoadExtraction(GalaxyData galaxy)
    {
        Extraction = new ResourceExtractionSystem();
        Extraction.RegisterGalaxy(galaxy);
        Extraction.DepositDepleted += (eid, dep) => DepositDepleted?.Invoke(eid, dep);
    }

    public void LoadExploration()
    {
        Exploration = new ExplorationManager();
        Exploration.SiteDiscovered += (eid, pid) => SiteDiscovered?.Invoke(eid, pid);
        Exploration.ScanProgressChanged += (eid, pid, prog, diff) =>
            ScanProgressChanged?.Invoke(eid, pid, prog, diff);
        Exploration.SiteScanComplete += (eid, pid) => SiteScanComplete?.Invoke(eid, pid);
    }

    /// <summary>
    /// Construct the SalvageSystem and pre-survey the player's home-system POIs.
    /// Requires <see cref="LoadExploration"/> to have run first. <paramref name="runSeed"/>
    /// is mixed into per-layer research / danger rolls so save→load doesn't change committed outcomes.
    /// </summary>
    public void LoadSalvage(
        GalaxyData galaxy, EmpireData player, int runSeed = 0,
        SalvageRegistry? registry = null)
    {
        if (Exploration == null)
            throw new InvalidOperationException("LoadExploration must run before LoadSalvage.");

        Salvage = new SalvageSystem(galaxy, Exploration, MvpShipDesigns.Registry, runSeed, registry);
        Salvage.YieldExtracted += (eid, pid, key, amt) => YieldExtracted?.Invoke(eid, pid, key, amt);
        Salvage.ActivityChanged += (eid, pid, act) => SiteActivityChanged?.Invoke(eid, pid, act);
        Salvage.ActivityRateChanged += (eid, pid) => SiteActivityRateChanged?.Invoke(eid, pid);
        Salvage.LayerScanProgressChanged += (eid, pid, idx, prog, diff) =>
            SiteLayerScanProgressChanged?.Invoke(eid, pid, idx, prog, diff);
        Salvage.LayerScanned += (eid, pid, idx) => SiteLayerScanned?.Invoke(eid, pid, idx);
        Salvage.LayerScavenged += (eid, pid, idx) => SiteLayerScavenged?.Invoke(eid, pid, idx);
        Salvage.LayerSkipped += (eid, pid, idx) => SiteLayerSkipped?.Invoke(eid, pid, idx);
        Salvage.ResearchUnlocked += (eid, pid, idx) => SiteResearchUnlocked?.Invoke(eid, pid, idx);
        Salvage.DangerTriggered += (eid, pid, idx, danger, sev) => SiteDangerTriggered?.Invoke(eid, pid, idx, danger, sev);
        Salvage.SpecialOutcomeReady += (eid, pid, oid) => SiteSpecialOutcomeReady?.Invoke(eid, pid, oid);
        Salvage.SpecialOutcomeResolved += (eid, pid, res) => SiteSpecialOutcomeResolved?.Invoke(eid, pid, res);

        // Pre-survey the home system's salvage sites for the player.
        var home = galaxy.GetSystem(player.HomeSystemId);
        if (home != null)
        {
            foreach (var poi in home.POIs)
                if (poi.SalvageSiteId.HasValue)
                    Exploration.SurveyPOI(player.Id, poi.Id, 100);
        }
    }

    public void LoadSettlements(IReadOnlyList<ColonyData> colonies)
    {
        Settlements = new SettlementSystem();
        foreach (var colonyData in colonies)
        {
            var colony = new Colony
            {
                Id = colonyData.Id,
                Name = colonyData.Name,
                OwnerEmpireId = colonyData.OwnerEmpireId,
                SystemId = colonyData.SystemId,
                POIId = colonyData.POIId,
                PlanetSize = colonyData.PlanetSize,
                Happiness = colonyData.Happiness,
            };
            colony.PopGroups.Add(new PopGroup
            {
                Count = colonyData.Population,
                Allocation = WorkPool.Food
            });
            PopAllocationManager.AutoAllocate(colony);

            var farm = BuildingData.FindById("food_farm");
            if (farm != null)
                colony.Queue.Enqueue(new BuildingProducible(farm));

            Settlements.AddColony(colony);
        }

        Settlements.BuildingCompleted += (colony, buildingId) => BuildingCompleted?.Invoke(colony, buildingId);
        Settlements.PopulationGrew += colony => PopulationGrew?.Invoke(colony);
    }

    public void LoadStations(IReadOnlyList<StationData> stationDatas)
    {
        Stations = new StationSystem();
        foreach (var stationData in stationDatas)
        {
            var station = StateConverter.ToStation(stationData);
            Stations.AddStation(station);
        }
        Stations.ModuleInstalled += (station, module) => ModuleInstalled?.Invoke(station, module);
    }

    public void LoadResearch(IReadOnlyList<EmpireData> empires, GameRandom rng)
    {
        TechRegistry = new TechTreeRegistry();
        Research = new ResearchEngine(TechRegistry);
        _researchStates.Clear();

        foreach (var empire in empires)
        {
            var state = StateConverter.CreateInitialResearchState(
                empire.Id, empire.Affinity, TechRegistry, rng.DeriveChild($"research_{empire.Id}"));
            _researchStates[empire.Id] = state;

            // Auto-start: queue first available subsystem for research.
            if (state.AvailableSubsystems.Count > 0 && state.CurrentProject == null)
                state.CurrentProject = state.AvailableSubsystems.First();
        }

        Research.SubsystemResearched += (empireId, subId) =>
        {
            SubsystemResearched?.Invoke(empireId, subId);

            // Auto-queue next available subsystem.
            var state = _researchStates.GetValueOrDefault(empireId);
            if (state != null && state.CurrentProject == null && state.AvailableSubsystems.Count > 0)
                state.CurrentProject = state.AvailableSubsystems.First();
        };

        Research.TierUnlocked += (empireId, color, category, tier) =>
            TierUnlocked?.Invoke(empireId, color, category, tier);
    }

    // ── Add-after-load helpers (formerly RegisterColony/RegisterStation) ──

    public bool AddColony(Colony colony)
    {
        if (Settlements == null) return false;
        Settlements.AddColony(colony);
        return true;
    }

    public bool AddStation(Station station)
    {
        if (Stations == null) return false;
        Stations.AddStation(station);
        return true;
    }

    // ── Per-tick processing ──────────────────────────────────────

    /// <summary>
    /// Process one fast tick across logic systems. Order matters: movement must run
    /// before salvage so per-system fleet capability reflects current arrivals.
    /// </summary>
    public void Tick(
        float fastDelta,
        IReadOnlyList<FleetData> fleets,
        IReadOnlyDictionary<int, ShipInstanceData> shipsById,
        IReadOnlyDictionary<int, EmpireData> empiresById)
    {
        Movement?.ProcessTick(fastDelta, fleets);
        Salvage?.ProcessTick(fastDelta, fleets, shipsById, empiresById);
    }

    // ── UI/query helpers (formerly on MainScene) ─────────────────

    /// <summary>POI lookup across the galaxy. Returns the POI plus its containing systemId.</summary>
    public POIData? FindPOI(GalaxyData? galaxy, int poiId, out int systemId)
    {
        systemId = -1;
        if (galaxy == null) return null;
        foreach (var sys in galaxy.Systems)
            foreach (var poi in sys.POIs)
                if (poi.Id == poiId) { systemId = sys.Id; return poi; }
        return null;
    }

    public SalvageSiteData? GetSalvageSite(GalaxyData? galaxy, int siteId) =>
        galaxy?.GetSalvageSite(siteId);

    /// <summary>UI helper: capability of all fleets in this POI's system that can perform <paramref name="type"/>.</summary>
    public float GetSystemCapability(
        GalaxyData? galaxy,
        int empireId,
        int poiId,
        SiteActivity type,
        IReadOnlyList<FleetData> fleets,
        IReadOnlyDictionary<int, ShipInstanceData> shipsById)
    {
        if (Salvage == null) return 0f;
        var poi = FindPOI(galaxy, poiId, out int sysId);
        if (poi == null) return 0f;
        return Salvage.ComputeSystemCapability(empireId, sysId, type, fleets, shipsById);
    }

    public int GetSystemActiveCount(GalaxyData? galaxy, int empireId, int poiId, SiteActivity type)
    {
        if (Salvage == null) return 0;
        var poi = FindPOI(galaxy, poiId, out int sysId);
        if (poi == null) return 0;
        return Salvage.CountActiveInSystem(empireId, sysId, type);
    }

    // ── Re-emitted events (subscribed by GameSystemsHost and bridged to EventBus) ──
    public event Action<FleetData, int>? FleetArrived;
    public event Action<FleetData, int>? FleetDeparted;
    public event Action<FleetData>? FleetOrderCompleted;
    public event Action<int, int>? SiteDiscovered;
    public event Action<int, int, float, float>? ScanProgressChanged;
    public event Action<int, int>? SiteScanComplete;
    public event Action<int, int, string, float>? YieldExtracted;
    public event Action<int, int, SiteActivity>? SiteActivityChanged;
    public event Action<int, int>? SiteActivityRateChanged;
    public event Action<int, int, int, float, float>? SiteLayerScanProgressChanged;
    public event Action<int, int, int>? SiteLayerScanned;
    public event Action<int, int, int>? SiteLayerScavenged;
    public event Action<int, int, int>? SiteLayerSkipped;
    public event Action<int, int, int>? SiteResearchUnlocked;
    public event Action<int, int, int, string, float>? SiteDangerTriggered;
    public event Action<int, int, string>? SiteSpecialOutcomeReady;
    public event Action<int, int, SalvageOutcomeProcessor.Resolution>? SiteSpecialOutcomeResolved;
    public event Action<int, string>? SubsystemResearched;
    public event Action<int, PrecursorColor, TechCategory, int>? TierUnlocked;
    public event Action<Colony, string>? BuildingCompleted;
    public event Action<Colony>? PopulationGrew;
    public event Action<Station, StationModule>? ModuleInstalled;
    public event Action<int, ResourceDeposit>? DepositDepleted;
}
