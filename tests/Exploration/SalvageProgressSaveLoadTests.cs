using System.Text.Json;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;
using Xunit;

namespace DerlictEmpires.Tests.Exploration;

/// <summary>
/// Round-trips the per-empire-per-site progress structure through System.Text.Json
/// to lock the save format. Also exercises GameSaveData embedding.
/// </summary>
public class SalvageProgressSaveLoadTests
{
    [Fact]
    public void Progress_RoundTripsThroughJson()
    {
        var original = SalvageSiteProgress.ForSite(empireId: 7, poiId: 42, layerCount: 3);
        original.Activity = SiteActivity.Scanning;
        original.ActiveLayerIndex = 1;
        original.LayerScanProgress[0] = 80f;
        original.LayerScanProgress[1] = 30f;
        original.LayerScanned[0] = true;
        original.LayerScavenged[0] = true;
        original.LayerSkipped[2] = false;
        original.ResearchUnlocked[0] = true;
        original.ResearchSubsystemId[0] = "Red_WeaponsEnergyPropulsion_T1_S0";
        original.DangerTriggered[0] = true;
        original.SpecialOutcomeAvailable = false;
        original.SpecialOutcomeConsumed = false;

        var json = JsonSerializer.Serialize(original);
        var roundTripped = JsonSerializer.Deserialize<SalvageSiteProgress>(json)!;

        Assert.Equal(original.EmpireId, roundTripped.EmpireId);
        Assert.Equal(original.POIId, roundTripped.POIId);
        Assert.Equal(original.Activity, roundTripped.Activity);
        Assert.Equal(original.ActiveLayerIndex, roundTripped.ActiveLayerIndex);
        Assert.Equal(original.LayerCount, roundTripped.LayerCount);
        Assert.Equal(original.LayerScanProgress, roundTripped.LayerScanProgress);
        Assert.Equal(original.LayerScanned, roundTripped.LayerScanned);
        Assert.Equal(original.LayerScavenged, roundTripped.LayerScavenged);
        Assert.Equal(original.LayerSkipped, roundTripped.LayerSkipped);
        Assert.Equal(original.ResearchUnlocked, roundTripped.ResearchUnlocked);
        Assert.Equal(original.ResearchSubsystemId, roundTripped.ResearchSubsystemId);
        Assert.Equal(original.DangerTriggered, roundTripped.DangerTriggered);
    }

    [Fact]
    public void GameSaveData_EmbedsSalvageProgresses()
    {
        var save = new GameSaveData();
        var p = SalvageSiteProgress.ForSite(0, 1, 2);
        p.LayerScanned[0] = true;
        save.SalvageProgresses.Add(p);

        var json = JsonSerializer.Serialize(save);
        var loaded = JsonSerializer.Deserialize<GameSaveData>(json)!;

        Assert.Single(loaded.SalvageProgresses);
        Assert.Equal(2, loaded.SalvageProgresses[0].LayerCount);
        Assert.True(loaded.SalvageProgresses[0].LayerScanned[0]);
        Assert.False(loaded.SalvageProgresses[0].LayerScanned[1]);
    }

    [Fact]
    public void SalvageSite_RoundTripsLayersAndColors()
    {
        var site = new SalvageSiteData
        {
            Id = 4,
            POIId = 9,
            TypeId = "old_battlefield",
            Name = "Battle of Vega",
            Tier = 4,
            Visibility = 50f,
            SpecialOutcomeId = "recover_derelict",
            Colors = new() { PrecursorColor.Red, PrecursorColor.Purple },
        };
        site.Layers.Add(new SalvageLayer
        {
            Index = 0,
            LayerColor = PrecursorColor.Red,
            ResearchTargetTier = 4,
            ResearchUnlockChance = 0.3f,
            DangerTypeId = "damage",
            DangerChance = 0.4f,
            DangerSeverity = 17f,
            ScanDifficulty = 110f,
            Yield = new() { ["Red_BasicComponent"] = 60f },
            RemainingYield = new() { ["Red_BasicComponent"] = 35f },
        });

        var json = JsonSerializer.Serialize(site);
        var rt = JsonSerializer.Deserialize<SalvageSiteData>(json)!;

        Assert.Equal(site.Tier, rt.Tier);
        Assert.Equal(site.TypeId, rt.TypeId);
        Assert.Equal(site.Colors, rt.Colors);
        Assert.Equal(site.SpecialOutcomeId, rt.SpecialOutcomeId);
        Assert.Single(rt.Layers);
        Assert.Equal(35f, rt.Layers[0].RemainingYield["Red_BasicComponent"], 2);
        Assert.Equal(PrecursorColor.Red, rt.Layers[0].LayerColor);
    }
}
