using System;
using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Core.Systems;

/// <summary>
/// Generates star system positions in a spiral galaxy pattern with distinct,
/// non-overlapping arms.
///
/// Key design choices:
/// 1. Low winding (~0.4 turns) prevents arms from overlapping.
/// 2. Arm width is clamped to a fraction of the inter-arm angular gap, so jitter
///    can never push a star into a neighboring arm.
/// 3. Density along each arm uses a power-curve bias toward the inner region,
///    giving a natural "thick core, thin rim" look.
/// 4. Core blob uses Gaussian falloff rather than uniform disk, so it blends
///    smoothly into the arm bases.
/// </summary>
public static class SpiralArmGenerator
{
    // ── Precursor color assignment per arm ──
    private static readonly PrecursorColor[] ArmColors =
    {
        PrecursorColor.Red,
        PrecursorColor.Blue,
        PrecursorColor.Green,
        PrecursorColor.Gold,
        PrecursorColor.Purple
    };

    private static readonly PrecursorColor[] AllColors =
        (PrecursorColor[])Enum.GetValues(typeof(PrecursorColor));

    // ── Spiral shape defaults ──
    private const float ArmTurns = 0.4f;           // full revolutions per arm (low = distinct arms)
    private const float SpiralTightness = 0.4f;     // logarithmic spiral parameter
    private const float ArmWidthFraction = 0.35f;   // fraction of inter-arm angular gap
    private const float DensityBiasExponent = 1.6f;  // power-curve bias toward inner arm
    private const float CoreRadiusFraction = 0.15f;  // core extends to 15% of galaxy radius
    private const float CoreFraction = 0.20f;        // 20% of systems in core
    private const int MaxRejections = 30;

    public static List<StarSystemData> Generate(
        int totalSystems,
        int armCount,
        float galaxyRadius,
        GameRandom rng)
    {
        var systems = new List<StarSystemData>();
        int nextId = 0;
        float minStarDistance = galaxyRadius * 0.025f;

        int coreSystems = Math.Max(10, (int)MathF.Round(totalSystems * CoreFraction));
        int armSystems = totalSystems - coreSystems;
        int perArm = armSystems / armCount;
        int remainder = armSystems - (perArm * armCount);

        // 1. Generate core blob (Gaussian falloff)
        var coreRng = rng.DeriveChild("core");
        GenerateCore(coreRng, systems, coreSystems, galaxyRadius, minStarDistance, ref nextId);

        // 2. Generate spiral arms
        for (int arm = 0; arm < armCount; arm++)
        {
            int count = perArm + (arm < remainder ? 1 : 0);
            var armRng = rng.DeriveChild(arm + 100);
            GenerateArm(armRng, systems, arm, armCount, count, galaxyRadius, minStarDistance, ref nextId);
        }

        return systems;
    }

    /// <summary>
    /// Core blob using 2D Gaussian (Box-Muller) — density peaks at center and
    /// fades smoothly into the arm bases. No hard disk edge.
    /// </summary>
    private static void GenerateCore(
        GameRandom rng,
        List<StarSystemData> systems,
        int count,
        float galaxyRadius,
        float minStarDistance,
        ref int nextId)
    {
        float coreRadius = galaxyRadius * CoreRadiusFraction;
        // σ chosen so ~95% of samples fall within coreRadius (2σ rule)
        float sigma = coreRadius / 2.0f;

        for (int i = 0; i < count; i++)
        {
            float x = 0, z = 0;
            bool placed = false;

            for (int attempt = 0; attempt < MaxRejections; attempt++)
            {
                // Box-Muller transform for 2D Gaussian
                float u1 = MathF.Max(rng.NextFloat(), 1e-6f);
                float u2 = rng.NextFloat();
                float mag = sigma * MathF.Sqrt(-2f * MathF.Log(u1));
                float angle = u2 * MathF.Tau;

                x = mag * MathF.Cos(angle);
                z = mag * MathF.Sin(angle);

                // Hard clamp to galaxy radius
                if (MathF.Sqrt(x * x + z * z) > galaxyRadius) continue;

                if (!TooClose(systems, x, z, minStarDistance))
                {
                    placed = true;
                    break;
                }
            }

            if (placed)
            {
                systems.Add(new StarSystemData
                {
                    Id = nextId++,
                    Name = GenerateStarName(rng, nextId),
                    PositionX = x,
                    PositionZ = z,
                    ArmIndex = -1,
                    IsCore = true,
                    DominantColor = AllColors[rng.RangeInt(AllColors.Length)],
                    RadialPosition = MathF.Sqrt(x * x + z * z) / galaxyRadius
                });
            }
        }
    }

    /// <summary>
    /// Generates stars along a single spiral arm with angular-width clamping
    /// to prevent arm overlap, and power-curve density bias.
    /// </summary>
    private static void GenerateArm(
        GameRandom rng,
        List<StarSystemData> systems,
        int armIndex,
        int armCount,
        int count,
        float galaxyRadius,
        float minStarDistance,
        ref int nextId)
    {
        float armOffset = (float)armIndex / armCount * MathF.Tau;
        float minR = galaxyRadius * CoreRadiusFraction * 0.8f; // arms start just inside core edge
        float maxR = galaxyRadius;

        // Angular gap between adjacent arms — arm width is clamped to a fraction of this
        float interArmAngle = MathF.Tau / armCount;
        float maxAngularHalfWidth = interArmAngle * ArmWidthFraction * 0.5f;

        var armColor = ArmColors[armIndex % ArmColors.Length];

        for (int i = 0; i < count; i++)
        {
            float x = 0, z = 0;
            bool placed = false;

            for (int attempt = 0; attempt < MaxRejections; attempt++)
            {
                // Sample t ∈ [0, 1] with power-curve bias toward 0 (inner arm)
                float tRaw = rng.NextFloat();
                float t = MathF.Pow(tRaw, DensityBiasExponent);

                // Radius: logarithmic spiral with tightness control
                float r = minR * MathF.Pow(maxR / minR, MathF.Pow(t, 1f + SpiralTightness));

                // Angle along spiral
                float spiralAngle = armOffset + t * ArmTurns * MathF.Tau;

                // Perpendicular jitter — tapers from full at core to 60% at rim
                float taperFactor = Lerp(1.0f, 0.6f, t);
                float angularHalfWidth = maxAngularHalfWidth * taperFactor;

                // Triangular distribution for peaked spread along arm spine
                float spread = (rng.NextFloat() + rng.NextFloat()) / 2f;
                spread = (spread - 0.5f) * 2f; // remap to [-1, 1]
                float angularOffset = spread * angularHalfWidth;

                float finalAngle = spiralAngle + angularOffset;

                // Small radial scatter for organic feel
                float radialJitter = r * 0.04f * (rng.NextFloat() - 0.5f) * 2f;
                r += radialJitter;

                x = r * MathF.Cos(finalAngle);
                z = r * MathF.Sin(finalAngle);

                if (MathF.Sqrt(x * x + z * z) > galaxyRadius) continue;

                if (!TooClose(systems, x, z, minStarDistance))
                {
                    placed = true;
                    break;
                }
            }

            if (placed)
            {
                // Color: near core = more mixed, near rim = more arm-dominant
                float tApprox = (MathF.Sqrt(x * x + z * z) - minR) / (maxR - minR);
                tApprox = Math.Clamp(tApprox, 0f, 1f);
                float armColorChance = Lerp(0.50f, 0.90f, tApprox);

                PrecursorColor color;
                if (rng.Chance(armColorChance))
                    color = armColor;
                else
                    color = AllColors[rng.RangeInt(AllColors.Length)];

                systems.Add(new StarSystemData
                {
                    Id = nextId++,
                    Name = GenerateStarName(rng, nextId),
                    PositionX = x,
                    PositionZ = z,
                    ArmIndex = armIndex,
                    IsCore = false,
                    DominantColor = color,
                    RadialPosition = MathF.Sqrt(x * x + z * z) / galaxyRadius
                });
            }
        }
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static bool TooClose(List<StarSystemData> systems, float x, float z, float minDist)
    {
        float minDistSq = minDist * minDist;
        foreach (var sys in systems)
        {
            float dx = sys.PositionX - x;
            float dz = sys.PositionZ - z;
            if (dx * dx + dz * dz < minDistSq)
                return true;
        }
        return false;
    }

    // ── Star naming ────────────────────────────────────────────────────

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
