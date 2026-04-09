using System;
using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Core.Systems;

/// <summary>
/// Generates star system positions in a spiral arm galaxy layout.
/// Core blob in the center, configurable spiral arms extending outward.
/// </summary>
public static class SpiralArmGenerator
{
    /// <summary>
    /// Assigns a precursor color to each arm index.
    /// With 4 arms, we use Red, Blue, Green, Gold. Purple appears in the core.
    /// With more arms, colors cycle.
    /// </summary>
    private static readonly PrecursorColor[] ArmColors =
    {
        PrecursorColor.Red,
        PrecursorColor.Blue,
        PrecursorColor.Green,
        PrecursorColor.Gold,
        PrecursorColor.Purple
    };

    public static List<StarSystemData> Generate(
        int totalSystems,
        int armCount,
        float galaxyRadius,
        GameRandom rng)
    {
        var systems = new List<StarSystemData>();
        int nextId = 0;

        // ~20% of systems go in the core blob
        int coreSystems = Math.Max(10, totalSystems / 5);
        int armSystems = totalSystems - coreSystems;
        int systemsPerArm = armSystems / armCount;

        // Generate core blob
        var coreRng = rng.DeriveChild("core");
        float coreRadius = galaxyRadius * 0.2f;
        for (int i = 0; i < coreSystems; i++)
        {
            var pos = PoissonDiskPoint(coreRng, coreRadius);
            systems.Add(new StarSystemData
            {
                Id = nextId++,
                Name = GenerateStarName(coreRng, nextId),
                PositionX = pos.x,
                PositionZ = pos.z,
                ArmIndex = -1,
                IsCore = true,
                DominantColor = ArmColors[coreRng.RangeInt(ArmColors.Length)],
                RadialPosition = MathF.Sqrt(pos.x * pos.x + pos.z * pos.z) / galaxyRadius
            });
        }

        // Generate spiral arms
        for (int arm = 0; arm < armCount; arm++)
        {
            var armRng = rng.DeriveChild(arm + 100);
            float armAngleOffset = (2f * MathF.PI / armCount) * arm;
            var armColor = ArmColors[arm % ArmColors.Length];

            int count = (arm < armCount - 1) ? systemsPerArm : (armSystems - systemsPerArm * (armCount - 1));

            for (int i = 0; i < count; i++)
            {
                float t = (float)(i + 1) / count; // 0..1 along the arm (0=core, 1=rim)
                var pos = SpiralPoint(armRng, t, armAngleOffset, coreRadius, galaxyRadius);

                systems.Add(new StarSystemData
                {
                    Id = nextId++,
                    Name = GenerateStarName(armRng, nextId),
                    PositionX = pos.x,
                    PositionZ = pos.z,
                    ArmIndex = arm,
                    IsCore = false,
                    DominantColor = PickArmColor(armRng, armColor, t),
                    RadialPosition = MathF.Sqrt(pos.x * pos.x + pos.z * pos.z) / galaxyRadius
                });
            }
        }

        return systems;
    }

    private static (float x, float z) SpiralPoint(
        GameRandom rng, float t, float armAngleOffset,
        float coreRadius, float galaxyRadius)
    {
        // Logarithmic spiral: r increases exponentially with angle
        float minR = coreRadius * 1.1f;
        float maxR = galaxyRadius * 0.95f;
        float radius = minR + (maxR - minR) * t;

        // Spiral winds ~1.5 full turns from core to rim
        float spiralWinds = 1.5f;
        float angle = armAngleOffset + t * spiralWinds * 2f * MathF.PI;

        // Perpendicular jitter — narrows toward the rim
        float armWidth = galaxyRadius * 0.08f * (1f - t * 0.5f);
        float jitterAngle = rng.RangeFloat(-1f, 1f) * armWidth / radius;
        float jitterRadius = rng.RangeFloat(-armWidth, armWidth) * 0.3f;

        angle += jitterAngle;
        radius += jitterRadius;

        float x = radius * MathF.Cos(angle);
        float z = radius * MathF.Sin(angle);

        return (x, z);
    }

    private static (float x, float z) PoissonDiskPoint(GameRandom rng, float radius)
    {
        // Simple rejection sampling for uniform disk distribution
        float angle = rng.RangeFloat(0f, 2f * MathF.PI);
        float r = radius * MathF.Sqrt(rng.NextFloat());
        return (r * MathF.Cos(angle), r * MathF.Sin(angle));
    }

    /// <summary>
    /// Near the core, colors mix more. Near the rim, the arm's color dominates.
    /// </summary>
    private static PrecursorColor PickArmColor(GameRandom rng, PrecursorColor armColor, float t)
    {
        // t near 0 = close to core (50% arm color), t near 1 = rim (90% arm color)
        float armChance = 0.5f + 0.4f * t;
        if (rng.Chance(armChance))
            return armColor;
        return ArmColors[rng.RangeInt(ArmColors.Length)];
    }

    private static readonly string[] StarPrefixes =
    {
        "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta",
        "Iota", "Kappa", "Lambda", "Mu", "Nu", "Xi", "Omicron", "Pi",
        "Rho", "Sigma", "Tau", "Upsilon", "Phi", "Chi", "Psi", "Omega"
    };

    private static readonly string[] StarSuffixes =
    {
        "Centauri", "Draconis", "Orionis", "Cygni", "Lyrae", "Aquilae",
        "Serpentis", "Leonis", "Tauri", "Geminorum", "Andromedae", "Persei",
        "Cassiopeiae", "Crucis", "Carinae", "Eridani", "Scorpii", "Sagittarii",
        "Phoenicis", "Hydrae", "Pavonis", "Tucanae", "Gruis", "Volantis"
    };

    private static string GenerateStarName(GameRandom rng, int id)
    {
        string prefix = StarPrefixes[rng.RangeInt(StarPrefixes.Length)];
        string suffix = StarSuffixes[rng.RangeInt(StarSuffixes.Length)];
        return $"{prefix} {suffix}";
    }
}
