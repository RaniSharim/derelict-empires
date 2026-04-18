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

    /// <summary>
    /// Build a single-system galaxy with `poiCount` salvage sites all of the same type.
    /// Scan difficulty 100, each POI yields 100 Red_SimpleOre.
    /// Fleets are owned by empire 0 and docked in the home system by default.
    /// </summary>
    private static Fixture MakeFixture(int poiCount, int fleetScouts, int fleetSalvagers)
    {
        var galaxy = new GalaxyData();
        var system = new StarSystemData { Id = 0, Name = "Home" };
        galaxy.Systems.Add(system);

        var poiIds = new int[poiCount];
        for (int i = 0; i < poiCount; i++)
        {
            int poiId = i + 1;
            int siteId = i;
            var site = new SalvageSiteData
            {
                Id = siteId,
                POIId = poiId,
                Type = SalvageSiteType.MinorDerelict,
                Color = PrecursorColor.Red,
                ScanDifficulty = 100f,
                TotalYield = new Dictionary<string, float> { ["Red_SimpleOre"] = 100f },
                RemainingYield = new Dictionary<string, float> { ["Red_SimpleOre"] = 100f },
                DepletionCurveExponent = 0.5f,
            };
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
    public void RequestScan_RequiresDiscoveredState()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 1, fleetSalvagers: 0);
        // Undiscovered → refuses
        Assert.False(f.Salvage.RequestActivity(0, f.PoiIds[0], SiteActivity.Scanning));

        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0] });
        Assert.True(f.Salvage.RequestActivity(0, f.PoiIds[0], SiteActivity.Scanning));
        Assert.Equal(SiteActivity.Scanning, f.Salvage.GetActivity(0, f.PoiIds[0]));
    }

    [Fact]
    public void RequestExtract_RequiresSurveyedState()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 0, fleetSalvagers: 1);
        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0] });
        Assert.False(f.Salvage.RequestActivity(0, f.PoiIds[0], SiteActivity.Extracting));

        f.Exploration.SurveyPOI(0, f.PoiIds[0], 100);
        Assert.True(f.Salvage.RequestActivity(0, f.PoiIds[0], SiteActivity.Extracting));
    }

    [Fact]
    public void SingleScan_ReceivesFullCapability()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 1, fleetSalvagers: 0);
        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0] });
        f.Salvage.RequestActivity(0, f.PoiIds[0], SiteActivity.Scanning);

        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);
        Assert.Equal(10f, f.Exploration.GetScanProgress(0, f.PoiIds[0]), 2);
    }

    [Fact]
    public void TwoSimultaneousScans_SplitCapability()
    {
        var f = MakeFixture(poiCount: 2, fleetScouts: 1, fleetSalvagers: 0);
        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0], f.PoiIds[1] });
        f.Salvage.RequestActivity(0, f.PoiIds[0], SiteActivity.Scanning);
        f.Salvage.RequestActivity(0, f.PoiIds[1], SiteActivity.Scanning);

        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);
        // 10 total scan capacity split two ways = 5 each.
        Assert.Equal(5f, f.Exploration.GetScanProgress(0, f.PoiIds[0]), 2);
        Assert.Equal(5f, f.Exploration.GetScanProgress(0, f.PoiIds[1]), 2);
    }

    [Fact]
    public void ThreeSimultaneousScans_SplitEvenly()
    {
        var f = MakeFixture(poiCount: 3, fleetScouts: 1, fleetSalvagers: 0);
        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0], f.PoiIds[1], f.PoiIds[2] });
        foreach (int id in f.PoiIds)
            f.Salvage.RequestActivity(0, id, SiteActivity.Scanning);

        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);
        float expected = 10f / 3f;
        foreach (int id in f.PoiIds)
            Assert.Equal(expected, f.Exploration.GetScanProgress(0, id), 2);
    }

    [Fact]
    public void CancellingOneScan_RemainingSpeedUp()
    {
        var f = MakeFixture(poiCount: 3, fleetScouts: 1, fleetSalvagers: 0);
        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0], f.PoiIds[1], f.PoiIds[2] });
        foreach (int id in f.PoiIds)
            f.Salvage.RequestActivity(0, id, SiteActivity.Scanning);

        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);  // each at 10/3
        f.Salvage.RequestActivity(0, f.PoiIds[2], SiteActivity.None);       // cancel one
        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);  // remaining at 10/2

        float expected = 10f / 3f + 10f / 2f;
        Assert.Equal(expected, f.Exploration.GetScanProgress(0, f.PoiIds[0]), 2);
        Assert.Equal(expected, f.Exploration.GetScanProgress(0, f.PoiIds[1]), 2);
    }

    [Fact]
    public void NoCapableFleetInSystem_ProgressFrozen()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 1, fleetSalvagers: 0);
        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0] });
        f.Salvage.RequestActivity(0, f.PoiIds[0], SiteActivity.Scanning);
        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);
        float before = f.Exploration.GetScanProgress(0, f.PoiIds[0]);

        // Move the scout out of the system mid-scan.
        f.Fleets[0].CurrentSystemId = -1;
        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);

        Assert.Equal(before, f.Exploration.GetScanProgress(0, f.PoiIds[0]), 2);
        // Activity remains toggled on.
        Assert.Equal(SiteActivity.Scanning, f.Salvage.GetActivity(0, f.PoiIds[0]));
    }

    [Fact]
    public void ScoutReturns_ResumesWherItLeftOff()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 1, fleetSalvagers: 0);
        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0] });
        f.Salvage.RequestActivity(0, f.PoiIds[0], SiteActivity.Scanning);
        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);

        f.Fleets[0].CurrentSystemId = -1;
        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);  // stalled

        f.Fleets[0].CurrentSystemId = 0;
        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);  // resumed

        Assert.Equal(20f, f.Exploration.GetScanProgress(0, f.PoiIds[0]), 2);
    }

    [Fact]
    public void ScanCompletion_FlipsToSurveyedAndClearsActivity()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 1, fleetSalvagers: 0);
        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0] });
        f.Salvage.RequestActivity(0, f.PoiIds[0], SiteActivity.Scanning);

        // 10 ticks × 10 scan = 100 — hits ScanDifficulty.
        for (int i = 0; i < 10; i++)
            f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);

        Assert.Equal(ExplorationState.Surveyed, f.Exploration.GetState(0, f.PoiIds[0]));
        Assert.Equal(SiteActivity.None, f.Salvage.GetActivity(0, f.PoiIds[0]));
    }

    [Fact]
    public void ExtractTick_CreditsEmpireAndDepletesSite()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 0, fleetSalvagers: 1);
        f.Exploration.SurveyPOI(0, f.PoiIds[0], 100);
        f.Salvage.RequestActivity(0, f.PoiIds[0], SiteActivity.Extracting);

        var site = f.Galaxy.SalvageSites[0];
        float beforeRemain = site.RemainingYield["Red_SimpleOre"];
        float beforeStockpile = f.Empire.ResourceStockpile.GetValueOrDefault("Red_SimpleOre", 0f);

        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);

        float extracted = beforeRemain - site.RemainingYield["Red_SimpleOre"];
        Assert.True(extracted > 0f, $"expected positive extraction, got {extracted}");
        Assert.Equal(extracted,
            f.Empire.ResourceStockpile["Red_SimpleOre"] - beforeStockpile, 3);
    }

    [Fact]
    public void MixedFleet_ContributesToBothScanAndExtractInSystem()
    {
        // One POI being scanned, one being extracted, in same system. One fleet with mixed design.
        // Build custom fixture: poi 1 Discovered, poi 2 Surveyed.
        var f = MakeFixture(poiCount: 2, fleetScouts: 0, fleetSalvagers: 0);
        // Add a single "mixed" fleet with a custom ship design.
        var mixedRegistry = new Dictionary<string, ShipDesign>
        {
            ["mixed"] = new ShipDesign { Id = "mixed", ScanStrength = 4f, ExtractionStrength = 6f },
        };
        f.Salvage = new SalvageSystem(f.Galaxy, f.Exploration, mixedRegistry);
        var ship = new ShipInstanceData { Id = 0, ShipDesignId = "mixed", FleetId = 0 };
        f.ShipsById = new Dictionary<int, ShipInstanceData> { [0] = ship };
        f.Fleets = new List<FleetData>
        {
            new() { Id = 0, Name = "Mixed", OwnerEmpireId = 0, CurrentSystemId = 0, ShipIds = new() { 0 } },
        };

        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0] });
        f.Exploration.SurveyPOI(0, f.PoiIds[1], 100);

        Assert.True(f.Salvage.RequestActivity(0, f.PoiIds[0], SiteActivity.Scanning));
        Assert.True(f.Salvage.RequestActivity(0, f.PoiIds[1], SiteActivity.Extracting));

        var site1 = f.Galaxy.SalvageSites[1];
        float beforeRemain = site1.RemainingYield["Red_SimpleOre"];

        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);

        Assert.Equal(4f, f.Exploration.GetScanProgress(0, f.PoiIds[0]), 2);
        Assert.True(site1.RemainingYield["Red_SimpleOre"] < beforeRemain);
    }

    [Fact]
    public void EmpireIsolation_CapacityNotShared()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 1, fleetSalvagers: 0);
        var empireB = new EmpireData { Id = 1, Name = "Other" };
        f.EmpiresById[1] = empireB;
        // Empire B has a scout in the same system, but it cannot contribute to empire A's scan.
        var otherShip = new ShipInstanceData { Id = 99, ShipDesignId = ScoutDesignId, FleetId = 99 };
        f.ShipsById[99] = otherShip;
        f.Fleets.Add(new FleetData
        {
            Id = 99, Name = "OtherScout", OwnerEmpireId = 1, CurrentSystemId = 0, ShipIds = new() { 99 },
        });

        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0] });
        f.Salvage.RequestActivity(0, f.PoiIds[0], SiteActivity.Scanning);
        f.Salvage.ProcessTick(1.0f, f.Fleets, f.ShipsById, f.EmpiresById);

        // Still only empire A's 10, not 20.
        Assert.Equal(10f, f.Exploration.GetScanProgress(0, f.PoiIds[0]), 2);
    }

    [Fact]
    public void ActivityChangedEvent_FiresOnToggle()
    {
        var f = MakeFixture(poiCount: 1, fleetScouts: 1, fleetSalvagers: 0);
        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0] });

        int fired = 0;
        SiteActivity lastActivity = SiteActivity.None;
        f.Salvage.ActivityChanged += (_, _, a) => { fired++; lastActivity = a; };

        f.Salvage.RequestActivity(0, f.PoiIds[0], SiteActivity.Scanning);
        Assert.Equal(1, fired);
        Assert.Equal(SiteActivity.Scanning, lastActivity);

        f.Salvage.RequestActivity(0, f.PoiIds[0], SiteActivity.None);
        Assert.Equal(2, fired);
        Assert.Equal(SiteActivity.None, lastActivity);
    }

    [Fact]
    public void RateChangedEvent_FiresForSiblingsOnStart()
    {
        var f = MakeFixture(poiCount: 3, fleetScouts: 1, fleetSalvagers: 0);
        f.Exploration.DiscoverSystem(0, 0, new List<int> { f.PoiIds[0], f.PoiIds[1], f.PoiIds[2] });

        f.Salvage.RequestActivity(0, f.PoiIds[0], SiteActivity.Scanning);
        f.Salvage.RequestActivity(0, f.PoiIds[1], SiteActivity.Scanning);

        var affected = new HashSet<int>();
        f.Salvage.ActivityRateChanged += (_, poiId) => affected.Add(poiId);

        f.Salvage.RequestActivity(0, f.PoiIds[2], SiteActivity.Scanning);

        // Every site in the sibling set should be notified.
        Assert.Contains(f.PoiIds[0], affected);
        Assert.Contains(f.PoiIds[1], affected);
        Assert.Contains(f.PoiIds[2], affected);
    }
}
