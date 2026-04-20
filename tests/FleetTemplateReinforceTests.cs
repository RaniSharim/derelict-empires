using System.Collections.Generic;
using System.Linq;
using Xunit;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Ships;

namespace DerlictEmpires.Tests;

/// <summary>
/// Reinforcement math for fleet templates: given a template composition and a
/// fleet's current ship roster, compute the deficit (what to build to bring the
/// fleet back to template). Pure function — no Godot dependency.
/// </summary>
public class FleetTemplateReinforceTests
{
    /// <summary>Deficit per design id. Negative numbers mean surplus over template.</summary>
    public static Dictionary<string, int> ComputeDeficit(
        FleetTemplate template,
        IEnumerable<ShipInstanceData> fleetShips)
    {
        var counts = new Dictionary<string, int>();
        foreach (var ship in fleetShips)
        {
            if (string.IsNullOrEmpty(ship.ShipDesignId)) continue;
            counts[ship.ShipDesignId] = counts.GetValueOrDefault(ship.ShipDesignId) + 1;
        }

        var deficit = new Dictionary<string, int>();
        foreach (var entry in template.Entries)
        {
            int have = counts.GetValueOrDefault(entry.DesignId);
            int need = entry.Count - have;
            if (need != 0) deficit[entry.DesignId] = need;
        }
        return deficit;
    }

    [Fact]
    public void MissingShips_ProduceDeficit()
    {
        var tmpl = new FleetTemplate
        {
            Entries = new List<FleetTemplateEntry>
            {
                new() { DesignId = "cruiser", Count = 3 },
                new() { DesignId = "scout", Count = 2 },
            },
        };
        var ships = new[]
        {
            new ShipInstanceData { ShipDesignId = "cruiser" },
            new ShipInstanceData { ShipDesignId = "cruiser" },
            // 1 cruiser short
            new ShipInstanceData { ShipDesignId = "scout" },
            // 1 scout short
        };

        var deficit = ComputeDeficit(tmpl, ships);
        Assert.Equal(1, deficit["cruiser"]);
        Assert.Equal(1, deficit["scout"]);
    }

    [Fact]
    public void FullComposition_YieldsEmptyDeficit()
    {
        var tmpl = new FleetTemplate
        {
            Entries = new List<FleetTemplateEntry>
            {
                new() { DesignId = "cruiser", Count = 2 },
            },
        };
        var ships = new[]
        {
            new ShipInstanceData { ShipDesignId = "cruiser" },
            new ShipInstanceData { ShipDesignId = "cruiser" },
        };

        var deficit = ComputeDeficit(tmpl, ships);
        Assert.Empty(deficit);
    }

    [Fact]
    public void Surplus_ProducesNegativeDeficit()
    {
        var tmpl = new FleetTemplate
        {
            Entries = new List<FleetTemplateEntry>
            {
                new() { DesignId = "cruiser", Count = 1 },
            },
        };
        var ships = new[]
        {
            new ShipInstanceData { ShipDesignId = "cruiser" },
            new ShipInstanceData { ShipDesignId = "cruiser" },
            new ShipInstanceData { ShipDesignId = "cruiser" },
        };

        var deficit = ComputeDeficit(tmpl, ships);
        Assert.Equal(-2, deficit["cruiser"]);
    }

    [Fact]
    public void UnrelatedShips_Ignored()
    {
        var tmpl = new FleetTemplate
        {
            Entries = new List<FleetTemplateEntry>
            {
                new() { DesignId = "cruiser", Count = 1 },
            },
        };
        var ships = new[]
        {
            new ShipInstanceData { ShipDesignId = "fighter" },
            new ShipInstanceData { ShipDesignId = "cruiser" },
        };

        var deficit = ComputeDeficit(tmpl, ships);
        Assert.False(deficit.ContainsKey("fighter"));
        Assert.False(deficit.ContainsKey("cruiser"));
    }
}
