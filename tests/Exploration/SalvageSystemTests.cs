using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Ships;
using Xunit;

namespace DerlictEmpires.Tests.Exploration;

public class SalvageSystemTests
{
    // ── Fixture helpers ──────────────────────────────────────────

    private const string ScoutDesignId = "scout_mk1";
    private const string SalvagerDesignId = "salvager_mk1";

    private static Dictionary<string, ShipDesign> DesignRegistry(
        float scanStrength = 10f,
        float extractionStrength = 15f)
    {
        return new Dictionary<string, ShipDesign>
        {
            [ScoutDesignId] = new ShipDesign
            {
                Id = ScoutDesignId, ScanStrength = scanStrength, ExtractionStrength = 1f,
            },
            [SalvagerDesignId] = new ShipDesign
            {
                Id = SalvagerDesignId, ScanStrength = 2f, ExtractionStrength = extractionStrength,
            },
        };
    }

    /// <summary>Build a single-layer site whose layer scan difficulty is 100 and yield is 100 Red ore.</summary>
    private static SalvageSiteData MakeSite(int siteId, int poiId, int layerCount = 1, float scanPerLayer = 100f, float yieldPerLayer = 100f)
    {
        var site = new SalvageSiteData
        {
            Id = siteId,
            POIId = poiId,
            TypeId = "minor_derelict",
            Name = $"Site {siteId}",
            Tier = 1,
            Colors = new List<PrecursorColor> { PrecursorColor.Red },
            DepletionCurveExponent = 0.5f,
        };
        for (int i = 0; i < layerCount; i++)
        {
            site.Layers.Add(new SalvageLayer
            {
                Index = i,
                LayerColor = PrecursorColor.Red,
                ResearchTargetTier = 1,
                ResearchUnlockChance = 0f, // deterministic in tests
                Yield = new Dictionary<string, float> { ["Red_SimpleOre"] = yieldPerLayer },
                RemainingYield = new Dictionary<string, float> { ["Red_SimpleOre"] = yieldPerLayer },
                ScanDifficulty = scanPerLayer,
                DangerTypeId = "damage",
                DangerChance = 0f,
                DangerSeverity = 0f,
            });
        }
        return site;
    }

    private sealed class Fixture
    {
        public GalaxyData Galaxy = null!;
        public ExplorationManager Exploration = null!;
        public SalvageSystem Salvage = null!;
        public EmpireData Empire = null!;
        public Dictionary<int, EmpireData> EmpiresById = new();
        public Dictionary<int, ShipInstanceData> ShipsById = new();
        public List<FleetData> Fleets = new();
        public int HomeSystemId;
        public int[] PoiIds = null!;
    }

    private static Fixture MakeFixture(int poiCount, int fleetScouts, int fleetSalvagers, int layersPerSite = 1)
    {
        var galaxy = new GalaxyData();
        var system = new StarSystemData { Id = 0, Name = "Home" };
        galaxy.Systems.Add(system);

        var poiIds = new int[poiCount];
        for (int i = 0; i < poiCount; i++)
        {
            int poiId = i + 1;
            int siteId = i;
            var site = MakeSite(siteId, poiId, layersPerSite);
            galaxy.SalvageSites.Add(site);
            system.POIs.Add(new POIData { Id = poiId, Name = $"POI{poiId}", SalvageSiteId = siteId });
            poiIds[i] = poiId;
        }

        var exploration = new ExplorationManager();
        var salvage = new SalvageSystem(galaxy, exploration, DesignRegistry());
        var empire = new EmpireData { Id = 0, Name = "Player", IsHuman = true };

        var fix = new Fixture
        {
            Galaxy = galaxy,
            Exploration = exploration,
            Salvage = salvage,
            Empire = empire,
            EmpiresById = new Dictionary<int, EmpireData> { [0] = empire },
            HomeSystemId = 0,
            PoiIds = poiIds,
        };

        int shipId = 0;
        int fleetId = 0;
        for (int i = 0; i < fleetScouts; i++)
        {
            var ship = new ShipInstanceData { Id = shipId, ShipDesignId = ScoutDesignId, FleetId = fleetId };
            fix.ShipsById[shipId] = ship;
            fix.Fleets.Add(new FleetData
            {
                Id = fleetId, Name = $"Scout{i + 1}", OwnerEmpireId = empire.Id,
                CurrentSystemId = 0, ShipIds = new List<int> { shipId },
            });
            shipId++; fleetId++;
        }
        for (int i = 0; i < fleetSalvagers; i++)
        {
            var ship = new ShipInstanceData { Id = shipId, ShipDesignId = SalvagerDesignId, FleetId = fleetId };
            fix.ShipsById[shipId] = ship;
            fix.Fleets.Add(new FleetData
            {
                Id = fleetId, Name = $"Salvager{i + 1}", OwnerEmpireId = empire.Id,
                CurrentSystemId = 0, ShipIds = new List<int> { shipId },
            });
            shipId++; fleetId++;
        }
        return fix;
    }

    // ── Tests ────────────────────────────────────────────────────

    [Fact]
    public void RequestScan_RequiresAtLeastDiscovered()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 1, fleetSalvagers: 0);
        Assert.False(f.Salvage.RequestScan(0, f.PoiIds[0]));

        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0] });
        Assert.True(f.Salvage.RequestScan(0, f.PoiIds[0]));
        Assert.Equal(SiteActivity.Scanning, f.Salvage.GetActivity(0, f.PoiIds[0]));
    }

    [Fact]
    public void RequestScavenge_RequiresLayerScanned()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 0, fleetSalvagers: 1);
        f.Exploration.SurveyPOI(0, f.PoiIds[0], 100);
        Assert.False(f.Salvage.RequestScavenge(0, f.PoiIds[0]));

        // Manually mark the active layer scanned (skip the scan tick loop).
        f.Salvage.RequestScan(0, f.PoiIds[0]);
        var p = f.Salvage.GetProgress(0, f.PoiIds[0])!;
        p.LayerScanned[0] = true;
        p.Activity = SiteActivity.None;

        Assert.True(f.Salvage.RequestScavenge(0, f.PoiIds[0]));
        Assert.Equal(SiteActivity.Extracting, f.Salvage.GetActivity(0, f.PoiIds[0]));
    }

    [Fact]
    public void SingleScan_ReceivesFullCapability()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 1, fleetSalvagers: 0);
        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0] });
        f.Salvage.RequestScan(0, f.PoiIds[0]);

        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);
        var progress = f.Salvage.GetProgress(0, f.PoiIds[0])!;
        Assert.Equal(10f, progress.LayerScanProgress[0], 2);
    }

    [Fact]
    public void TwoSimultaneousScans_SplitCapability()
    {
        var f = MakeFixture(poiCount: 2, fleetScouts: 1, fleetSalvagers: 0);
        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0], f.PoiIds[1] });
        f.Salvage.RequestScan(0, f.PoiIds[0]);
        f.Salvage.RequestScan(0, f.PoiIds[1]);

        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);
        var p0 = f.Salvage.GetProgress(0, f.PoiIds[0])!;
        var p1 = f.Salvage.GetProgress(0, f.PoiIds[1])!;
        Assert.Equal(5f, p0.LayerScanProgress[0], 2);
        Assert.Equal(5f, p1.LayerScanProgress[0], 2);
    }

    [Fact]
    public void NoCapableFleetInSystem_ProgressFrozen()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 1, fleetSalvagers: 0);
        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0] });
        f.Salvage.RequestScan(0, f.PoiIds[0]);
        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);
        float before = f.Salvage.GetProgress(0, f.PoiIds[0])!.LayerScanProgress[0];

        f.Fleets[0].CurrentSystemId = -1;
        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);

        Assert.Equal(before, f.Salvage.GetProgress(0, f.PoiIds[0])!.LayerScanProgress[0], 2);
        Assert.Equal(SiteActivity.Scanning, f.Salvage.GetActivity(0, f.PoiIds[0]));
    }

    [Fact]
    public void ScanCompletion_FlipsLayerScannedAndStopsActivity()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 1, fleetSalvagers: 0);
        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0] });
        f.Salvage.RequestScan(0, f.PoiIds[0]);

        // 10 ticks × 10 scan = 100, hits the layer's ScanDifficulty.
        for (int i = 0; i < 10; i++)
            f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);

        var p = f.Salvage.GetProgress(0, f.PoiIds[0])!;
        Assert.True(p.LayerScanned[0]);
        Assert.Equal(SiteActivity.None, f.Salvage.GetActivity(0, f.PoiIds[0]));
    }

    [Fact]
    public void Scavenge_DepletesLayerYieldAndAdvancesIndex()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 0, fleetSalvagers: 1, layersPerSite: 2);
        f.Exploration.SurveyPOI(0, f.PoiIds[0], 100);
        // Mark layer 0 scanned to skip the scan loop.
        f.Salvage.RequestScan(0, f.PoiIds[0]);
        var p = f.Salvage.GetProgress(0, f.PoiIds[0])!;
        p.LayerScanned[0] = true;
        p.Activity = SiteActivity.None;
        f.Salvage.RequestScavenge(0, f.PoiIds[0]);

        var site = f.Galaxy.SalvageSites[0];
        // Tick repeatedly until the layer is depleted.
        for (int i = 0; i < 50 && !p.LayerScavenged[0]; i++)
            f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);

        Assert.True(p.LayerScavenged[0]);
        Assert.Equal(1, p.ActiveLayerIndex);
        Assert.True(f.Empire.ResourceStockpile["Red_SimpleOre"] > 0f);
        Assert.True(site.Layers[0].RemainingYield["Red_SimpleOre"] < 0.5f);
    }

    [Fact]
    public void Skip_TerminatesLayerWithoutYield()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 1, fleetSalvagers: 0, layersPerSite: 2);
        f.Exploration.SurveyPOI(0, f.PoiIds[0], 100);
        f.Salvage.RequestScan(0, f.PoiIds[0]);
        var p = f.Salvage.GetProgress(0, f.PoiIds[0])!;
        p.LayerScanned[0] = true;
        p.Activity = SiteActivity.None;

        Assert.True(f.Salvage.RequestSkip(0, f.PoiIds[0]));
        Assert.True(p.LayerSkipped[0]);
        Assert.Equal(1, p.ActiveLayerIndex);
        Assert.Equal(0f, f.Empire.ResourceStockpile.GetValueOrDefault("Red_SimpleOre"));
    }

    [Fact]
    public void EmpireIsolation_CapacityNotShared()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 1, fleetSalvagers: 0);
        var empireB = new EmpireData { Id = 1, Name = "Other" };
        f.EmpiresById[1] = empireB;
        var otherShip = new ShipInstanceData { Id = 99, ShipDesignId = ScoutDesignId, FleetId = 99 };
        f.ShipsById[99] = otherShip;
        f.Fleets.Add(new FleetData
        {
            Id = 99, Name = "OtherScout", OwnerEmpireId = 1, CurrentSystemId = 0, ShipIds = new() { 99 },
        });

        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0] });
        f.Salvage.RequestScan(0, f.PoiIds[0]);
        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);

        Assert.Equal(10f, f.Salvage.GetProgress(0, f.PoiIds[0])!.LayerScanProgress[0], 2);
    }

    [Fact]
    public void ActivityChangedEvent_FiresOnRequest()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 1, fleetSalvagers: 0);
        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0] });

        int fired = 0;
        SiteActivity last = SiteActivity.None;
        f.Salvage.ActivityChanged += (_, _, a) => { fired++; last = a; };

        f.Salvage.RequestScan(0, f.PoiIds[0]);
        Assert.Equal(1, fired);
        Assert.Equal(SiteActivity.Scanning, last);

        f.Salvage.RequestStop(0, f.PoiIds[0]);
        Assert.Equal(2, fired);
        Assert.Equal(SiteActivity.None, last);
    }

    [Fact]
    public void DangerRoll_FiresOnceWhenScavengeStarts()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 0, fleetSalvagers: 1);
        f.Exploration.SurveyPOI(0, f.PoiIds[0], 100);
        // Set danger chance to 1.0 so the roll always lands.
        var layer = f.Galaxy.SalvageSites[0].Layers[0];
        layer.DangerChance = 1.0f;
        layer.DangerSeverity = 7f;

        f.Salvage.RequestScan(0, f.PoiIds[0]);
        var p = f.Salvage.GetProgress(0, f.PoiIds[0])!;
        p.LayerScanned[0] = true;
        p.Activity = SiteActivity.None;

        int triggered = 0;
        string? observedDanger = null;
        float observedSeverity = 0f;
        f.Salvage.DangerTriggered += (_, _, _, danger, sev) =>
        {
            triggered++;
            observedDanger = danger;
            observedSeverity = sev;
        };

        f.Salvage.RequestScavenge(0, f.PoiIds[0]);
        Assert.Equal(1, triggered);
        Assert.Equal("damage", observedDanger);
        Assert.Equal(7f, observedSeverity, 2);

        // Stopping and re-starting scavenge does not re-roll the danger.
        f.Salvage.RequestStop(0, f.PoiIds[0]);
        f.Salvage.RequestScavenge(0, f.PoiIds[0]);
        Assert.Equal(1, triggered);
    }

    [Fact]
    public void ResearchUnlockEvent_FiresWhenScanCompletesAndChanceLands()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 1, fleetSalvagers: 0);
        f.Galaxy.SalvageSites[0].Layers[0].ResearchUnlockChance = 1.0f;
        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0] });
        f.Salvage.RequestScan(0, f.PoiIds[0]);

        int unlocked = 0;
        f.Salvage.ResearchUnlocked += (_, _, _) => unlocked++;

        for (int i = 0; i < 10; i++)
            f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);

        Assert.Equal(1, unlocked);
    }

    [Fact]
    public void SpecialOutcomeReady_FiresAfterAllLayersTerminal()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 1, fleetSalvagers: 1, layersPerSite: 2);
        f.Galaxy.SalvageSites[0].SpecialOutcomeId = "repair_station";
        f.Exploration.SurveyPOI(0, f.PoiIds[0], 100);

        // Mark both layers scanned, skip both.
        f.Salvage.RequestScan(0, f.PoiIds[0]);
        var p = f.Salvage.GetProgress(0, f.PoiIds[0])!;
        p.LayerScanned[0] = true;
        p.LayerScanned[1] = true;
        p.Activity = SiteActivity.None;

        string? readyOutcome = null;
        f.Salvage.SpecialOutcomeReady += (_, _, oid) => readyOutcome = oid;

        f.Salvage.RequestSkip(0, f.PoiIds[0]); // layer 0
        f.Salvage.RequestSkip(0, f.PoiIds[0]); // layer 1
        Assert.Equal("repair_station", readyOutcome);
        Assert.True(p.SpecialOutcomeAvailable);
    }
}
