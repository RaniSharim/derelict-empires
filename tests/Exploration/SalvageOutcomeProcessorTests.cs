using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;
using Xunit;

namespace DerlictEmpires.Tests.Exploration;

public class SalvageOutcomeProcessorTests
{
    private static SalvageRegistry MakeRegistry()
    {
        const string types = """
            [{
              "id": "ship_graveyard", "displayName": "Ship Graveyard",
              "nameTemplates": ["Graveyard"],
              "eligiblePOIWeights": { "ShipGraveyard": 1.0 },
              "baseScanPerLayer": 100, "layerCountMin": 1, "layerCountMax": 1,
              "layerYieldMin": 30, "layerYieldMax": 60, "componentBias": 0.4,
              "researchChancePerLayer": [0.1], "dangerTypeIds": ["damage"],
              "specialOutcomeId": "recover_derelict"
            },{
              "id": "abandoned_station", "displayName": "Abandoned Station",
              "nameTemplates": ["Station"],
              "eligiblePOIWeights": { "AbandonedStation": 1.0 },
              "baseScanPerLayer": 100, "layerCountMin": 1, "layerCountMax": 1,
              "layerYieldMin": 30, "layerYieldMax": 60, "componentBias": 0.4,
              "researchChancePerLayer": [0.1], "dangerTypeIds": ["damage"],
              "specialOutcomeId": "repair_station"
            }]
            """;
        const string dangers = """
            [{ "id": "damage", "displayName": "Damage", "effectKind": "FleetDamage", "baseSeverity": 5, "perTierBonus": 3 }]
            """;
        const string outcomes = """
            [
              { "id": "repair_station", "action": "RepairStation", "cost": { "BasicComponent": 50 }, "params": { "moduleSlots": "4" } },
              { "id": "recover_derelict", "action": "RecoverDerelict", "cost": { "AdvancedComponent": 5 }, "params": { "sizeClass": "Frigate" } }
            ]
            """;
        return SalvageRegistry.Load(types, dangers, outcomes);
    }

    private static (SalvageSiteData site, SalvageSiteProgress progress) MakeReadySite(string outcomeId)
    {
        var site = new SalvageSiteData
        {
            Id = 1,
            POIId = 42,
            TypeId = "abandoned_station",
            Name = "Test Site",
            Tier = 2,
            Colors = new List<PrecursorColor> { PrecursorColor.Red },
            SpecialOutcomeId = outcomeId,
        };
        site.Layers.Add(new SalvageLayer { Index = 0 });
        var p = SalvageSiteProgress.ForSite(0, 42, 1);
        p.LayerScanned[0] = true;
        p.LayerScavenged[0] = true;
        p.ActiveLayerIndex = 1;
        p.SpecialOutcomeAvailable = true;
        return (site, p);
    }

    [Fact]
    public void RepairStation_DeductsCostAndReturnsStationSpec()
    {
        var (site, p) = MakeReadySite("repair_station");
        var empire = new EmpireData { Id = 0, Name = "Player" };
        empire.ResourceStockpile["Red_BasicComponent"] = 100f;

        var res = SalvageOutcomeProcessor.Resolve(empire, site, p, MakeRegistry(), systemId: 7);
        Assert.True(res.Success);
        Assert.Equal(SalvageOutcomeProcessor.OutcomeKind.RepairStation, res.Kind);
        Assert.NotNull(res.Station);
        Assert.Equal(7, res.Station!.Value.SystemId);
        Assert.Equal(42, res.Station.Value.POIId);
        Assert.Equal(4, res.Station.Value.ModuleSlots);
        Assert.Equal(PrecursorColor.Red, res.Station.Value.PrimaryColor);
        Assert.Equal(50f, empire.ResourceStockpile["Red_BasicComponent"], 2);
        Assert.True(p.SpecialOutcomeConsumed);
        Assert.False(p.SpecialOutcomeAvailable);
    }

    [Fact]
    public void RecoverDerelict_BuildsDerelictWithSiteColorAndTier()
    {
        var (site, p) = MakeReadySite("recover_derelict");
        site.TypeId = "ship_graveyard";
        var empire = new EmpireData { Id = 0 };
        empire.ResourceStockpile["Red_AdvancedComponent"] = 10f;

        var res = SalvageOutcomeProcessor.Resolve(empire, site, p, MakeRegistry(), systemId: 3);
        Assert.True(res.Success);
        Assert.Equal(SalvageOutcomeProcessor.OutcomeKind.RecoverDerelict, res.Kind);
        Assert.NotNull(res.Derelict);
        Assert.Equal(PrecursorColor.Red, res.Derelict!.Color);
        Assert.Equal(2, res.Derelict.TechTier);
        Assert.Equal(ShipSizeClass.Frigate, res.Derelict.SizeClass);
        Assert.Equal(5f, empire.ResourceStockpile["Red_AdvancedComponent"], 2);
    }

    [Fact]
    public void Insufficient_FailsAndDoesNotConsume()
    {
        var (site, p) = MakeReadySite("repair_station");
        var empire = new EmpireData { Id = 0 };
        empire.ResourceStockpile["Red_BasicComponent"] = 10f; // need 50

        var res = SalvageOutcomeProcessor.Resolve(empire, site, p, MakeRegistry(), systemId: 0);
        Assert.False(res.Success);
        Assert.Contains("insufficient", res.FailureReason);
        Assert.False(p.SpecialOutcomeConsumed);
        Assert.True(p.SpecialOutcomeAvailable);
        Assert.Equal(10f, empire.ResourceStockpile["Red_BasicComponent"], 2);
    }

    [Fact]
    public void NotAvailable_Fails()
    {
        var (site, p) = MakeReadySite("repair_station");
        p.SpecialOutcomeAvailable = false;
        var empire = new EmpireData { Id = 0 };
        empire.ResourceStockpile["Red_BasicComponent"] = 100f;

        var res = SalvageOutcomeProcessor.Resolve(empire, site, p, MakeRegistry(), systemId: 0);
        Assert.False(res.Success);
        Assert.Contains("not yet available", res.FailureReason);
    }

    [Fact]
    public void AlreadyConsumed_Fails()
    {
        var (site, p) = MakeReadySite("repair_station");
        p.SpecialOutcomeConsumed = true;
        var empire = new EmpireData { Id = 0 };
        empire.ResourceStockpile["Red_BasicComponent"] = 100f;

        var res = SalvageOutcomeProcessor.Resolve(empire, site, p, MakeRegistry(), systemId: 0);
        Assert.False(res.Success);
        Assert.Contains("already consumed", res.FailureReason);
    }

    [Fact]
    public void FullyQualifiedCostKey_NotRebound()
    {
        // Override the registry with a cost that names a specific color.
        const string outcomes = """
            [{ "id": "repair_station", "action": "RepairStation",
                "cost": { "Blue_BasicComponent": 20 },
                "params": { "moduleSlots": "4" } }]
            """;
        var reg = SalvageRegistry.Load(
            "[{\"id\":\"x\",\"displayName\":\"X\",\"nameTemplates\":[\"X\"]," +
            "\"eligiblePOIWeights\":{\"AbandonedStation\":1.0},\"baseScanPerLayer\":100," +
            "\"layerCountMin\":1,\"layerCountMax\":1,\"layerYieldMin\":1,\"layerYieldMax\":1," +
            "\"componentBias\":0.5,\"researchChancePerLayer\":[0.1],\"dangerTypeIds\":[\"damage\"]," +
            "\"specialOutcomeId\":\"repair_station\"}]",
            "[{\"id\":\"damage\",\"displayName\":\"D\",\"effectKind\":\"FleetDamage\",\"baseSeverity\":5,\"perTierBonus\":3}]",
            outcomes);

        var (site, p) = MakeReadySite("repair_station");
        site.Colors = new() { PrecursorColor.Red };
        var empire = new EmpireData { Id = 0 };
        empire.ResourceStockpile["Blue_BasicComponent"] = 30f;

        var res = SalvageOutcomeProcessor.Resolve(empire, site, p, reg, systemId: 0);
        Assert.True(res.Success);
        Assert.Equal(10f, empire.ResourceStockpile["Blue_BasicComponent"], 2);
    }
}
