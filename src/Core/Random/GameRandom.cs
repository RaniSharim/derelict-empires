using System;
using System.Collections.Generic;

namespace DerlictEmpires.Core.Random;

/// <summary>
/// Deterministic seeded random number generator for all game randomization.
/// Wraps System.Random (non-crypto). Same seed always produces the same sequence.
/// </summary>
public sealed class GameRandom
{
    private readonly System.Random _rng;
    private readonly int _seed;

    public int Seed => _seed;

    public GameRandom(int seed)
    {
        _seed = seed;
        _rng = new System.Random(seed);
    }

    /// <summary>Returns a random int in [min, max) (exclusive upper bound).</summary>
    public int RangeInt(int min, int max) => _rng.Next(min, max);

    /// <summary>Returns a random int in [0, max) (exclusive upper bound).</summary>
    public int RangeInt(int max) => _rng.Next(max);

    /// <summary>Returns a random float in [min, max).</summary>
    public float RangeFloat(float min, float max) =>
        min + (float)_rng.NextDouble() * (max - min);

    /// <summary>Returns a random float in [0, 1).</summary>
    public float NextFloat() => (float)_rng.NextDouble();

    /// <summary>Returns a random double in [0, 1).</summary>
    public double NextDouble() => _rng.NextDouble();

    /// <summary>Returns true with the given probability [0, 1].</summary>
    public bool Chance(float probability) => NextFloat() < probability;

    /// <summary>Picks a random element from the list.</summary>
    public T Pick<T>(IReadOnlyList<T> items) => items[RangeInt(items.Count)];

    /// <summary>
    /// Picks an index based on weights. Higher weight = higher probability.
    /// Returns -1 if weights are empty or all zero.
    /// </summary>
    public int WeightedChoice(IReadOnlyList<float> weights)
    {
        if (weights.Count == 0) return -1;

        float total = 0f;
        for (int i = 0; i < weights.Count; i++)
            total += weights[i];

        if (total <= 0f) return -1;

        float roll = RangeFloat(0f, total);
        float cumulative = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative)
                return i;
        }

        return weights.Count - 1;
    }

    /// <summary>Fisher-Yates shuffle in place.</summary>
    public void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = RangeInt(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Derives a child GameRandom with a deterministic seed based on this RNG's seed
    /// combined with a differentiator. Useful for subsystem-specific RNG streams.
    /// </summary>
    public GameRandom DeriveChild(int differentiator) =>
        new GameRandom(HashCombine(_seed, differentiator));

    /// <summary>
    /// Derives a child GameRandom using a string differentiator (hashed).
    /// Uses FNV-1a — <c>string.GetHashCode()</c> is randomized per-process in .NET 5+, which would
    /// break determinism across runs.
    /// </summary>
    public GameRandom DeriveChild(string differentiator) =>
        new GameRandom(HashCombine(_seed, FnvHash(differentiator)));

    private static int HashCombine(int a, int b)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + a;
            hash = hash * 31 + b;
            return hash;
        }
    }

    /// <summary>FNV-1a 32-bit. Stable across processes and .NET versions.</summary>
    private static int FnvHash(string s)
    {
        unchecked
        {
            uint hash = 2166136261u;
            foreach (char c in s)
                hash = (hash ^ c) * 16777619u;
            return (int)hash;
        }
    }
}
