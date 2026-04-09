using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Core.Systems;

/// <summary>
/// Orchestrates full galaxy generation: systems, lanes, POIs.
/// Pure C# — no Godot dependencies. Deterministic given the same seed.
/// </summary>
public static class GalaxyGenerator
{
    public static GalaxyData Generate(GalaxyGenerationConfig config)
    {
        var rng = new GameRandom(config.Seed);

        // Step 1: Generate star system positions in spiral arm layout
        var systems = SpiralArmGenerator.Generate(
            config.TotalSystems,
            config.ArmCount,
            config.GalaxyRadius,
            rng.DeriveChild("arms"));

        // Step 2: Generate navigable lanes between systems
        var lanes = LaneGenerator.Generate(
            systems,
            config.MaxLaneLength,
            config.MinNeighbors,
            config.MaxNeighbors,
            config.HiddenLaneRatio,
            rng.DeriveChild("lanes"));

        // Step 3: Generate POIs for each system
        POIGenerator.Generate(systems, config.ArmCount, rng.DeriveChild("pois"));

        return new GalaxyData
        {
            Seed = config.Seed,
            Systems = systems,
            Lanes = lanes,
            ArmCount = config.ArmCount
        };
    }
}
