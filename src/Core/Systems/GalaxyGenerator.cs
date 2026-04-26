using System.Collections.Generic;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Core.Systems;

/// <summary>
/// Orchestrates full galaxy generation: systems, lanes, POIs, salvage sites.
/// Pure C# — no Godot dependencies. Deterministic given the same seed.
/// </summary>
public static class GalaxyGenerator
{
    /// <summary>
    /// Generate a galaxy. <paramref name="salvageRegistry"/> may be null in tests
    /// that don't care about salvage sites; in that case the salvage list is empty.
    /// </summary>
    public static GalaxyData Generate(
        GalaxyGenerationConfig config,
        SalvageRegistry? salvageRegistry = null)
    {
        var rng = new GameRandom(config.Seed);

        var systems = SpiralArmGenerator.Generate(
            config.TotalSystems,
            config.ArmCount,
            config.GalaxyRadius,
            config.MaxLaneLength,
            rng.DeriveChild("arms"));

        var lanes = LaneGenerator.Generate(
            systems,
            config.MaxLaneLength,
            config.MinNeighbors,
            config.MaxNeighbors,
            config.HiddenLaneRatio,
            rng.DeriveChild("lanes"));

        POIGenerator.Generate(systems, config.ArmCount, rng.DeriveChild("pois"));

        var galaxy = new GalaxyData
        {
            Seed = config.Seed,
            Systems = systems,
            Lanes = lanes,
            SalvageSites = new List<SalvageSiteData>(),
            ArmCount = config.ArmCount,
            ArmColorPairs = BuildArmColorPairs(config.ArmCount),
        };

        if (salvageRegistry != null)
            galaxy.SalvageSites = SalvageSiteGenerator.Populate(
                galaxy, salvageRegistry, rng.DeriveChild("salvage"));

        return galaxy;
    }

    /// <summary>
    /// Deterministic neighbor-cycle: arm i = (ArmColors[i % 5], ArmColors[(i+1) % 5]).
    /// Every color appears as both a primary and a secondary across the 5-arm rotation;
    /// fewer arms simply truncate the cycle.
    /// </summary>
    public static List<ArmColorPair> BuildArmColorPairs(int armCount)
    {
        var pairs = new List<ArmColorPair>(armCount);
        var palette = SpiralArmGenerator.ArmColors;
        for (int i = 0; i < armCount; i++)
        {
            var primary = palette[i % palette.Length];
            var secondary = palette[(i + 1) % palette.Length];
            pairs.Add(new ArmColorPair(primary, secondary));
        }
        return pairs;
    }
}
