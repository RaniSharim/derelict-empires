using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Ships;
using DerlictEmpires.Core.Systems;
using Xunit;

namespace DerlictEmpires.Tests;

public class EmpireDesignStateTests
{
    [Fact]
    public void CreateDesign_GeneratesStableId()
    {
        var state = new EmpireDesignState();

        var d1 = state.CreateDesign("cruiser_weapons");
        var d2 = state.CreateDesign("corvette_fast");

        Assert.Equal("design_1", d1.Id);
        Assert.Equal("design_2", d2.Id);
        Assert.Equal(2, state.Designs.Count);
    }

    [Fact]
    public void RemoveDesign_ReturnsFalseWhenMissing()
    {
        var state = new EmpireDesignState();
        state.CreateDesign("cruiser_weapons");

        Assert.False(state.RemoveDesign("design_nonexistent"));
        Assert.True(state.RemoveDesign("design_1"));
        Assert.Empty(state.Designs);
    }

    [Fact]
    public void CreateTemplate_GeneratesStableId()
    {
        var state = new EmpireDesignState();

        var t1 = state.CreateTemplate("Frontier Striker");
        var t2 = state.CreateTemplate();

        Assert.Equal("template_1", t1.Id);
        Assert.Equal("template_2", t2.Id);
        Assert.Equal("Frontier Striker", t1.Name);
    }

    [Fact]
    public void TemplatesUsingDesign_FindsAllReferences()
    {
        var state = new EmpireDesignState();
        var brawler = state.CreateDesign("cruiser_weapons", "Brawler Cruiser");
        var scout = state.CreateDesign("corvette_fast", "Scout");

        var t1 = state.CreateTemplate("Strike Team");
        t1.Entries.Add(new FleetTemplateEntry { DesignId = brawler.Id, Count = 3 });
        t1.Entries.Add(new FleetTemplateEntry { DesignId = scout.Id, Count = 2 });

        var t2 = state.CreateTemplate("Recon");
        t2.Entries.Add(new FleetTemplateEntry { DesignId = scout.Id, Count = 4 });

        var matches = state.TemplatesUsingDesign(scout.Id);

        Assert.Equal(2, matches.Count);
        Assert.Contains(t1, matches);
        Assert.Contains(t2, matches);
    }

    [Fact]
    public void SaveLoadRoundTrip_PreservesDesignsAndTemplates()
    {
        var save = new GameSaveData { MasterSeed = 42 };
        var empire = new EmpireData { Id = 1, Name = "Test" };
        var brawler = empire.DesignState.CreateDesign("cruiser_weapons", "Brawler Mk II");
        brawler.SlotFills = new() { "red_plasma_t1", "red_plasma_t1", "" };

        var tmpl = empire.DesignState.CreateTemplate("Frontier Striker");
        tmpl.Entries.Add(new FleetTemplateEntry { DesignId = brawler.Id, Count = 3 });
        tmpl.RoleDefaults[Core.Enums.FleetRole.Brawler] = Disposition.Charge;

        save.Empires.Add(empire);

        var json = SaveLoadManager.ToJson(save);
        var restored = SaveLoadManager.FromJson(json);

        Assert.Single(restored.Empires);
        var restoredState = restored.Empires[0].DesignState;
        Assert.Single(restoredState.Designs);
        Assert.Equal("design_1", restoredState.Designs[0].Id);
        Assert.Equal("Brawler Mk II", restoredState.Designs[0].Name);
        Assert.Equal(3, restoredState.Designs[0].SlotFills.Count);

        Assert.Single(restoredState.Templates);
        Assert.Equal("Frontier Striker", restoredState.Templates[0].Name);
        Assert.Single(restoredState.Templates[0].Entries);
        Assert.Equal(brawler.Id, restoredState.Templates[0].Entries[0].DesignId);
        Assert.Equal(3, restoredState.Templates[0].Entries[0].Count);
        Assert.Equal(Disposition.Charge, restoredState.Templates[0].RoleDefaults[Core.Enums.FleetRole.Brawler]);
    }

    [Fact]
    public void NextIndexes_PreservedAcrossRoundTrip()
    {
        var save = new GameSaveData();
        var empire = new EmpireData { Id = 1 };
        empire.DesignState.CreateDesign("cruiser_weapons");
        empire.DesignState.CreateDesign("corvette_fast");
        empire.DesignState.RemoveDesign("design_1");
        save.Empires.Add(empire);

        var restored = SaveLoadManager.FromJson(SaveLoadManager.ToJson(save));
        var nextDesign = restored.Empires[0].DesignState.CreateDesign("frigate_fast");

        // After two creations and one removal, next index should still be 3.
        Assert.Equal("design_3", nextDesign.Id);
    }
}
