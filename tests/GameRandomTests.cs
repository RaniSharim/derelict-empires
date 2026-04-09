using Xunit;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Tests;

public class GameRandomTests
{
    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        var rng1 = new GameRandom(42);
        var rng2 = new GameRandom(42);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(rng1.RangeInt(1000), rng2.RangeInt(1000));
        }
    }

    [Fact]
    public void DifferentSeed_ProducesDifferentSequence()
    {
        var rng1 = new GameRandom(42);
        var rng2 = new GameRandom(99);

        bool anyDifferent = false;
        for (int i = 0; i < 100; i++)
        {
            if (rng1.RangeInt(1000) != rng2.RangeInt(1000))
            {
                anyDifferent = true;
                break;
            }
        }
        Assert.True(anyDifferent);
    }

    [Fact]
    public void RangeInt_StaysInBounds()
    {
        var rng = new GameRandom(123);
        for (int i = 0; i < 1000; i++)
        {
            int val = rng.RangeInt(5, 10);
            Assert.InRange(val, 5, 9); // exclusive upper bound
        }
    }

    [Fact]
    public void RangeFloat_StaysInBounds()
    {
        var rng = new GameRandom(123);
        for (int i = 0; i < 1000; i++)
        {
            float val = rng.RangeFloat(-5f, 5f);
            Assert.InRange(val, -5f, 5f);
        }
    }

    [Fact]
    public void Chance_RespectsProbability()
    {
        var rng = new GameRandom(42);
        int trueCount = 0;
        int trials = 10000;

        for (int i = 0; i < trials; i++)
        {
            if (rng.Chance(0.3f)) trueCount++;
        }

        // Should be roughly 30% ± 3%
        double ratio = (double)trueCount / trials;
        Assert.InRange(ratio, 0.25, 0.35);
    }

    [Fact]
    public void Pick_ReturnsElementFromList()
    {
        var rng = new GameRandom(42);
        var items = new[] { "a", "b", "c", "d" };

        for (int i = 0; i < 100; i++)
        {
            string picked = rng.Pick(items);
            Assert.Contains(picked, items);
        }
    }

    [Fact]
    public void WeightedChoice_RespectsWeights()
    {
        var rng = new GameRandom(42);
        var weights = new float[] { 0f, 0f, 1f, 0f }; // Only index 2 has weight

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(2, rng.WeightedChoice(weights));
        }
    }

    [Fact]
    public void WeightedChoice_EmptyWeights_ReturnsNegativeOne()
    {
        var rng = new GameRandom(42);
        Assert.Equal(-1, rng.WeightedChoice(Array.Empty<float>()));
    }

    [Fact]
    public void WeightedChoice_AllZero_ReturnsNegativeOne()
    {
        var rng = new GameRandom(42);
        Assert.Equal(-1, rng.WeightedChoice(new float[] { 0f, 0f, 0f }));
    }

    [Fact]
    public void Shuffle_PreservesAllElements()
    {
        var rng = new GameRandom(42);
        var list = new List<int> { 1, 2, 3, 4, 5 };
        rng.Shuffle(list);

        Assert.Equal(5, list.Count);
        Assert.Contains(1, list);
        Assert.Contains(2, list);
        Assert.Contains(3, list);
        Assert.Contains(4, list);
        Assert.Contains(5, list);
    }

    [Fact]
    public void Shuffle_SameSeed_SameResult()
    {
        var list1 = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var list2 = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        new GameRandom(42).Shuffle(list1);
        new GameRandom(42).Shuffle(list2);

        Assert.Equal(list1, list2);
    }

    [Fact]
    public void DeriveChild_Deterministic()
    {
        var parent1 = new GameRandom(42);
        var parent2 = new GameRandom(42);

        var child1 = parent1.DeriveChild(7);
        var child2 = parent2.DeriveChild(7);

        for (int i = 0; i < 50; i++)
        {
            Assert.Equal(child1.RangeInt(1000), child2.RangeInt(1000));
        }
    }

    [Fact]
    public void DeriveChild_DifferentDifferentiator_DifferentSequence()
    {
        var parent = new GameRandom(42);
        var childA = parent.DeriveChild(1);
        var childB = parent.DeriveChild(2);

        bool anyDifferent = false;
        for (int i = 0; i < 100; i++)
        {
            if (childA.RangeInt(1000) != childB.RangeInt(1000))
            {
                anyDifferent = true;
                break;
            }
        }
        Assert.True(anyDifferent);
    }

    [Fact]
    public void DeriveChild_StringKey_Deterministic()
    {
        var rng1 = new GameRandom(42).DeriveChild("galaxy");
        var rng2 = new GameRandom(42).DeriveChild("galaxy");

        for (int i = 0; i < 50; i++)
        {
            Assert.Equal(rng1.NextFloat(), rng2.NextFloat());
        }
    }
}
