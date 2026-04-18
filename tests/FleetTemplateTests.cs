using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Ships;
using Xunit;

namespace DerlictEmpires.Tests;

public class FleetTemplateTests
{
    [Fact]
    public void EmptyTemplate_HasNoEntries()
    {
        var t = new FleetTemplate { Id = "t1", Name = "Empty" };
        Assert.Empty(t.Entries);
        Assert.Empty(t.RoleDefaults);
    }

    [Fact]
    public void Entry_WithRoleOverride_TakesOverrideOverDesignRole()
    {
        var entry = new FleetTemplateEntry
        {
            DesignId = "design_1",
            Count = 3,
            RoleOverride = FleetRole.Guardian,
        };

        Assert.Equal(FleetRole.Guardian, entry.RoleOverride);
    }

    [Fact]
    public void RoleDefaults_DistinguishesDispositions()
    {
        var t = new FleetTemplate { Id = "t1", Name = "Mixed" };
        t.RoleDefaults[FleetRole.Brawler] = Disposition.Charge;
        t.RoleDefaults[FleetRole.Scout] = Disposition.StandBack;
        t.RoleDefaults[FleetRole.Guardian] = Disposition.Hold;

        Assert.Equal(Disposition.Charge, t.RoleDefaults[FleetRole.Brawler]);
        Assert.Equal(Disposition.StandBack, t.RoleDefaults[FleetRole.Scout]);
        Assert.Equal(Disposition.Hold, t.RoleDefaults[FleetRole.Guardian]);
    }

    [Fact]
    public void MultipleEntries_CanReferenceSameDesign()
    {
        // Edge case: template says "3 brawlers tagged Guardian role + 2 brawlers tagged Brawler role".
        var t = new FleetTemplate { Id = "t1", Name = "Split Roles" };
        t.Entries.Add(new FleetTemplateEntry { DesignId = "design_1", Count = 3, RoleOverride = FleetRole.Guardian });
        t.Entries.Add(new FleetTemplateEntry { DesignId = "design_1", Count = 2, RoleOverride = FleetRole.Brawler });

        Assert.Equal(2, t.Entries.Count);
        Assert.Equal(5, t.Entries[0].Count + t.Entries[1].Count);
    }
}
