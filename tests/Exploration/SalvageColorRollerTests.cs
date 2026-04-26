using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;
using Xunit;

namespace DerlictEmpires.Tests.Exploration;

public class SalvageColorRollerTests
{
    private static readonly ArmColorPair RedBlue =
        new(PrecursorColor.Red, PrecursorColor.Blue);

    [Fact]
    public void NoArmPair_FallsBackToUniformWeights()
    {
        System.Span<float> w = stackalloc float[5];
        SalvageColorRoller.BuildWeights(armPair: null, radialPosition: 0.5f, w);
        for (int i = 0; i < 5; i++)
            Assert.Equal(SalvageColorRoller.UniformWeight, w[i], 5);
    }

    [Fact]
    public void Rim_FullyArmBiased()
    {
        System.Span<float> w = stackalloc float[5];
        SalvageColorRoller.BuildWeights(RedBlue, radialPosition: 1.0f, w);
        Assert.Equal(SalvageColorRoller.ArmBiasPrimaryWeight,   w[(int)PrecursorColor.Red],    5);
        Assert.Equal(SalvageColorRoller.ArmBiasSecondaryWeight, w[(int)PrecursorColor.Blue],   5);
        Assert.Equal(SalvageColorRoller.ArmBiasOtherWeight,     w[(int)PrecursorColor.Green],  5);
        Assert.Equal(SalvageColorRoller.ArmBiasOtherWeight,     w[(int)PrecursorColor.Gold],   5);
        Assert.Equal(SalvageColorRoller.ArmBiasOtherWeight,     w[(int)PrecursorColor.Purple], 5);
    }

    [Fact]
    public void InsideCoreRadius_FullyUniformEvenForArmSystem()
    {
        // Inside core blob, arm systems shouldn't exist anyway, but defend the math.
        System.Span<float> w = stackalloc float[5];
        SalvageColorRoller.BuildWeights(RedBlue, radialPosition: 0.05f, w);
        for (int i = 0; i < 5; i++)
            Assert.Equal(SalvageColorRoller.UniformWeight, w[i], 5);
    }

    [Fact]
    public void MidGalaxy_HalfBlend()
    {
        // Between core (0.15) and rim (1.0): pick s=0.5 → radial = 0.575
        System.Span<float> w = stackalloc float[5];
        SalvageColorRoller.BuildWeights(RedBlue, radialPosition: 0.575f, w);
        // Primary at s=0.5: 0.20 * 0.5 + 0.30 * 0.5 = 0.25
        Assert.Equal(0.25f, w[(int)PrecursorColor.Red], 3);
        // Other at s=0.5: 0.20 * 0.5 + 0.10 * 0.5 = 0.15
        Assert.Equal(0.15f, w[(int)PrecursorColor.Green], 3);
    }

    [Fact]
    public void DistributionSampling_RimMatchesArmBiasWithinTolerance()
    {
        var rng = new GameRandom(seed: 12345);
        var counts = new int[5];
        const int n = 20000;
        for (int i = 0; i < n; i++)
        {
            var c = SalvageColorRoller.PickOne(RedBlue, radialPosition: 1.0f, rng);
            counts[(int)c]++;
        }

        // Expected proportions: Red 30/90, Blue 30/90, others 10/90.
        // 20k samples → Red ≈ 6667 (tol ±300), other ≈ 2222 (tol ±200).
        Assert.InRange(counts[(int)PrecursorColor.Red],   6300, 7100);
        Assert.InRange(counts[(int)PrecursorColor.Blue],  6300, 7100);
        Assert.InRange(counts[(int)PrecursorColor.Green], 1900, 2600);
        Assert.InRange(counts[(int)PrecursorColor.Gold],  1900, 2600);
        Assert.InRange(counts[(int)PrecursorColor.Purple], 1900, 2600);
    }

    [Fact]
    public void DistributionSampling_CoreFallback_IsApproximatelyUniform()
    {
        var rng = new GameRandom(seed: 999);
        var counts = new int[5];
        const int n = 20000;
        for (int i = 0; i < n; i++)
        {
            var c = SalvageColorRoller.PickOne(armPair: null, radialPosition: 0.0f, rng);
            counts[(int)c]++;
        }
        // Each ≈ 4000 ± 250 (a couple % from uniform).
        for (int i = 0; i < 5; i++)
            Assert.InRange(counts[i], 3700, 4300);
    }

    [Fact]
    public void Roll_AlwaysReturnsAtLeastOneColor()
    {
        var rng = new GameRandom(seed: 1);
        for (int i = 0; i < 100; i++)
        {
            var r = SalvageColorRoller.Roll(RedBlue, 0.8f, rng);
            Assert.NotEmpty(r.Colors);
            Assert.True(r.Colors.Count <= 2, $"got {r.Colors.Count} colors");
        }
    }

    [Fact]
    public void Roll_TierBonusOnlyAppliesAfterMultiColor()
    {
        var rng = new GameRandom(seed: 7);
        bool sawMulti = false;
        bool sawTierBonus = false;
        bool sawTierBonusWithoutMulti = false;
        for (int i = 0; i < 5000; i++)
        {
            var r = SalvageColorRoller.Roll(RedBlue, 0.8f, rng);
            if (r.Colors.Count > 1) sawMulti = true;
            if (r.TierBonus > 0)
            {
                sawTierBonus = true;
                if (r.Colors.Count == 1) sawTierBonusWithoutMulti = true;
            }
        }
        Assert.True(sawMulti, "expected some multi-color rolls in 5000 trials");
        Assert.True(sawTierBonus, "expected some tier bonus rolls in 5000 trials");
        Assert.False(sawTierBonusWithoutMulti, "tier bonus should require multi-color");
    }

    [Fact]
    public void Roll_MultiColorRate_IsApproximatelyTenPercent()
    {
        var rng = new GameRandom(seed: 42);
        int multi = 0;
        const int n = 10000;
        for (int i = 0; i < n; i++)
        {
            var r = SalvageColorRoller.Roll(RedBlue, 0.8f, rng);
            if (r.Colors.Count > 1) multi++;
        }
        // 10% ± 1.5%.
        Assert.InRange(multi, 850, 1150);
    }

    [Fact]
    public void Roll_DeterministicForSameSeed()
    {
        var a = new GameRandom(seed: 12321);
        var b = new GameRandom(seed: 12321);
        for (int i = 0; i < 50; i++)
        {
            var ra = SalvageColorRoller.Roll(RedBlue, 0.7f, a);
            var rb = SalvageColorRoller.Roll(RedBlue, 0.7f, b);
            Assert.Equal(ra.TierBonus, rb.TierBonus);
            Assert.Equal(ra.Colors, rb.Colors);
        }
    }
}
