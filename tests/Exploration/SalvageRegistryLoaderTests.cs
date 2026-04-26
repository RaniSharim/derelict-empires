using System;
using System.IO;
using DerlictEmpires.Core.Exploration;
using Xunit;

namespace DerlictEmpires.Tests.Exploration;

public class SalvageRegistryLoaderTests
{
    private const string MinimalDangers = """
        [{ "id": "damage", "displayName": "Damage", "effectKind": "FleetDamage", "baseSeverity": 5, "perTierBonus": 3 }]
        """;

    private const string MinimalOutcomes = """
        [
          { "id": "repair_station", "action": "RepairStation", "cost": { "BasicComponent": 50 }, "params": { "moduleSlots": "4" } },
          { "id": "recover_derelict", "action": "RecoverDerelict", "cost": { "AdvancedComponent": 5 }, "params": {} }
        ]
        """;

    private const string MinimalType = """
        [{
          "id": "minor_derelict", "displayName": "Minor Derelict",
          "nameTemplates": ["Drifting Hulk"],
          "eligiblePOIWeights": { "DebrisField": 0.3, "AsteroidField": 0.6 },
          "baseScanPerLayer": 80, "layerCountMin": 1, "layerCountMax": 2,
          "layerYieldMin": 30, "layerYieldMax": 80, "componentBias": 0.3,
          "researchChancePerLayer": [0.10, 0.20, 0.30, 0.40, 0.50],
          "dangerTypeIds": ["damage"], "specialOutcomeId": null
        }]
        """;

    [Fact]
    public void Load_RoundTripsValidJson()
    {
        var reg = SalvageRegistry.Load(MinimalType, MinimalDangers, MinimalOutcomes);

        Assert.Single(reg.Types);
        Assert.Single(reg.Dangers);
        Assert.Equal(2, reg.Outcomes.Count);

        var t = reg.GetSiteType("minor_derelict");
        Assert.NotNull(t);
        Assert.Equal("Minor Derelict", t!.DisplayName);
        Assert.Equal(80f, t.BaseScanPerLayer);
        Assert.Equal(2, t.LayerCountMax);
        Assert.Equal(0.3f, t.EligiblePOIWeights["DebrisField"], 3);
        Assert.Single(t.DangerTypeIds);
    }

    [Fact]
    public void Load_RejectsUnknownDangerReference()
    {
        var badType = MinimalType.Replace("\"damage\"", "\"radiation\"");
        var ex = Assert.Throws<InvalidOperationException>(
            () => SalvageRegistry.Load(badType, MinimalDangers, MinimalOutcomes));
        Assert.Contains("unknown danger", ex.Message);
    }

    [Fact]
    public void Load_RejectsUnknownOutcomeReference()
    {
        var badType = MinimalType.Replace("\"specialOutcomeId\": null", "\"specialOutcomeId\": \"nuke_it\"");
        var ex = Assert.Throws<InvalidOperationException>(
            () => SalvageRegistry.Load(badType, MinimalDangers, MinimalOutcomes));
        Assert.Contains("unknown outcome", ex.Message);
    }

    [Fact]
    public void Load_RejectsUnknownPOIType()
    {
        var badType = MinimalType.Replace("DebrisField", "FleetCarrier");
        var ex = Assert.Throws<InvalidOperationException>(
            () => SalvageRegistry.Load(badType, MinimalDangers, MinimalOutcomes));
        Assert.Contains("unknown POIType", ex.Message);
    }

    [Fact]
    public void Load_RejectsDuplicateIds()
    {
        // Wrap two copies of the same single-entry definition in one array.
        var inner = MinimalType.TrimStart('[').TrimEnd(']', '\n', '\r', ' ');
        var dup = "[" + inner + "," + inner + "]";
        var ex = Assert.Throws<InvalidOperationException>(
            () => SalvageRegistry.Load(dup, MinimalDangers, MinimalOutcomes));
        Assert.Contains("duplicate", ex.Message);
    }

    [Fact]
    public void Load_RejectsInvalidLayerRange()
    {
        var badType = MinimalType.Replace("\"layerCountMax\": 2", "\"layerCountMax\": 7");
        var ex = Assert.Throws<InvalidOperationException>(
            () => SalvageRegistry.Load(badType, MinimalDangers, MinimalOutcomes));
        Assert.Contains("layer-count range", ex.Message);
    }

    [Fact]
    public void Load_RejectsEmptyJson()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => SalvageRegistry.Load("", MinimalDangers, MinimalOutcomes));
        Assert.Contains("empty or missing", ex.Message);
    }

    /// <summary>
    /// Loads the actual JSON shipped under <c>resources/data/</c> to catch
    /// authoring drift between the seed files and the registry validator.
    /// Walks up from the test binary to find the project root.
    /// </summary>
    [Fact]
    public void Load_ShippedJsonFiles()
    {
        string root = FindProjectRoot();
        string types = File.ReadAllText(Path.Combine(root, "resources", "data", "salvage_types.json"));
        string dangers = File.ReadAllText(Path.Combine(root, "resources", "data", "salvage_dangers.json"));
        string outcomes = File.ReadAllText(Path.Combine(root, "resources", "data", "salvage_outcomes.json"));

        var reg = SalvageRegistry.Load(types, dangers, outcomes);
        Assert.True(reg.Types.Count >= 7, "expected at least the 7 legacy types");
        Assert.True(reg.Dangers.Count >= 1);
        Assert.True(reg.Outcomes.Count >= 2);
        Assert.NotNull(reg.GetSiteType("old_battlefield"));
    }

    private static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Derlict Empires.csproj")))
            dir = dir.Parent;
        if (dir == null)
            throw new InvalidOperationException("could not locate project root");
        return dir.FullName;
    }
}
