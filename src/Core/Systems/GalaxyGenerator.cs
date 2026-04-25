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
    public static GalaxyData Generate(GalaxyGenerationConfig config)
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

        var salvageSites = SalvagePlacement.Populate(systems, rng.DeriveChild("salvage"));

        return new GalaxyData
        {
            Seed = config.Seed,
            Systems = systems,
            Lanes = lanes,
            SalvageSites = salvageSites,
            ArmCount = config.ArmCount
        };
    }
}
