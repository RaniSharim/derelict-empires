using System;
using System.Collections.Generic;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// Depletion-curve math for extracting resources from a salvage site.
/// Pure C#, deterministic. Plan §8.3.
/// </summary>
public static class ExtractionCalculator
{
    /// <summary>
    /// Compute per-resource yield for one extraction tick.
    /// Remaining fraction raised to the depletion exponent — first 50% extracts fast,
    /// the last 10% drags. Never reaches zero.
    /// </summary>
    /// <param name="totalYield">Per-resource original yield.</param>
    /// <param name="remainingYield">Per-resource remaining yield (current state).</param>
    /// <param name="extractionStrength">Aggregate extraction strength of the fleet.</param>
    /// <param name="depletionExponent">Curve exponent (0.5 default).</param>
    /// <param name="delta">Tick delta in seconds (slow tick → 1.0).</param>
    /// <returns>Per-resource amount to extract this tick.</returns>
    public static Dictionary<string, float> PerTickYield(
        IReadOnlyDictionary<string, float> totalYield,
        IReadOnlyDictionary<string, float> remainingYield,
        float extractionStrength,
        float depletionExponent,
        float delta)
    {
        var result = new Dictionary<string, float>();
        if (totalYield.Count == 0 || extractionStrength <= 0f || delta <= 0f)
            return result;

        float totalSum = 0f;
        foreach (var v in totalYield.Values) totalSum += v;
        if (totalSum <= 0f) return result;

        foreach (var kv in totalYield)
        {
            float total = kv.Value;
            float remaining = remainingYield.GetValueOrDefault(kv.Key);
            if (remaining <= 0f) continue;

            float frac = Math.Clamp(remaining / total, 0f, 1f);
            float curve = MathF.Pow(frac, depletionExponent);

            // Proportional distribution across resources by their share of total.
            float share = total / totalSum;
            float amount = extractionStrength * curve * share * delta;

            // Cap at remaining so we don't overdraw in a single tick.
            amount = MathF.Min(amount, remaining);
            if (amount > 0f)
                result[kv.Key] = amount;
        }

        return result;
    }
}
