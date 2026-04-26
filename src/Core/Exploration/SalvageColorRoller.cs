using System;
using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// Implements the salvage-site color roll:
///
///   - Arm-biased weights [primary 0.30, secondary 0.30, others 0.10 each] (sums to 0.90).
///   - Core mix: blend toward uniform [0.20×5] using radial position. <c>s = 0</c> at the
///     core radius, <c>s = 1</c> at the rim. weight = lerp(uniform, armBias, s).
///   - 10% "extra" roll appends a second color via the same picker.
///   - If a second extra hits while picking color #2, bump tier by +1 instead of adding
///     a third color (bounded recursion — prevents runaway).
///
/// Pure C#, deterministic given a <see cref="GameRandom"/>.
/// </summary>
public static class SalvageColorRoller
{
    public const float ExtraRollChance = 0.10f;
    public const float ArmBiasPrimaryWeight   = 0.30f;
    public const float ArmBiasSecondaryWeight = 0.30f;
    public const float ArmBiasOtherWeight     = 0.10f;
    public const float UniformWeight          = 0.20f;

    public readonly struct Result
    {
        public readonly List<PrecursorColor> Colors;
        public readonly int TierBonus;
        public Result(List<PrecursorColor> colors, int tierBonus)
        {
            Colors = colors;
            TierBonus = tierBonus;
        }
    }

    /// <summary>
    /// Roll a site's color list and any tier bonus from the recursive 10% rule.
    /// </summary>
    /// <param name="armPair">Arm color pair, or null for core (no arm).</param>
    /// <param name="radialPosition">Normalized [0=center, 1=rim].</param>
    /// <param name="rng">Deterministic RNG.</param>
    public static Result Roll(ArmColorPair? armPair, float radialPosition, GameRandom rng)
    {
        var colors = new List<PrecursorColor> { PickOne(armPair, radialPosition, rng) };
        int tierBonus = 0;

        if (rng.Chance(ExtraRollChance))
        {
            var second = PickOne(armPair, radialPosition, rng);
            // Avoid duplicate primary→primary multi-color reading as "still single color"
            // — re-roll up to 3 times to prefer a different color, but accept duplicates
            // as a fallback so determinism is preserved when only one color has any weight.
            for (int i = 0; i < 3 && second == colors[0]; i++)
                second = PickOne(armPair, radialPosition, rng);
            colors.Add(second);

            if (rng.Chance(ExtraRollChance))
                tierBonus = 1;
        }

        return new Result(colors, tierBonus);
    }

    /// <summary>Pick one color via the lerped weights. Public for testability.</summary>
    public static PrecursorColor PickOne(ArmColorPair? armPair, float radialPosition, GameRandom rng)
    {
        Span<float> weights = stackalloc float[5];
        BuildWeights(armPair, radialPosition, weights);
        return (PrecursorColor)WeightedPick(weights, rng);
    }

    /// <summary>
    /// Compute the per-color weight vector. <paramref name="weights"/> is length 5,
    /// indexed by <see cref="PrecursorColor"/>. Weights are NOT normalized to sum
    /// to 1 — <see cref="WeightedPick"/> handles totals internally.
    /// </summary>
    public static void BuildWeights(ArmColorPair? armPair, float radialPosition, Span<float> weights)
    {
        if (weights.Length != 5) throw new ArgumentException("weights must be length 5", nameof(weights));

        if (!armPair.HasValue)
        {
            for (int i = 0; i < 5; i++) weights[i] = UniformWeight;
            return;
        }

        // s = 0 at core boundary, 1 at rim. Inside the core blob (radial < CoreRadiusFraction)
        // we still go uniform, matching "more mixed near core".
        float s = (radialPosition - SpiralArmGenerator.CoreRadiusFraction)
                  / (1f - SpiralArmGenerator.CoreRadiusFraction);
        s = Math.Clamp(s, 0f, 1f);

        int primIdx = (int)armPair.Value.Primary;
        int secIdx  = (int)armPair.Value.Secondary;

        for (int i = 0; i < 5; i++)
        {
            float armBias =
                i == primIdx ? ArmBiasPrimaryWeight :
                i == secIdx  ? ArmBiasSecondaryWeight :
                               ArmBiasOtherWeight;
            weights[i] = UniformWeight * (1f - s) + armBias * s;
        }
    }

    private static int WeightedPick(ReadOnlySpan<float> weights, GameRandom rng)
    {
        float total = 0f;
        for (int i = 0; i < weights.Length; i++) total += weights[i];
        if (total <= 0f) return 0;

        float roll = rng.RangeFloat(0f, total);
        float cum = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            cum += weights[i];
            if (roll < cum) return i;
        }
        return weights.Length - 1;
    }
}
