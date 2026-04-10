using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Core.Events;

public enum VictoryType { PrecursorMystery, Domination, Economic, None }

/// <summary>
/// Checks victory conditions each slow tick. Pure C#.
/// </summary>
public static class VictoryConditionChecker
{
    public struct VictoryResult
    {
        public VictoryType Type;
        public int WinnerEmpireId;
    }

    /// <summary>
    /// Check all victory conditions. Returns the first one met, or None.
    /// </summary>
    public static VictoryResult Check(IReadOnlyList<EmpireData> empires, GameState gameState)
    {
        if (gameState != GameState.Playing) return new VictoryResult { Type = VictoryType.None };

        // Domination: only one empire has colonies/stations
        var aliveEmpires = empires.Where(e =>
            e.ResourceStockpile.Count > 0 || e.HomeSystemId >= 0).ToList();

        if (aliveEmpires.Count == 1)
        {
            return new VictoryResult
            {
                Type = VictoryType.Domination,
                WinnerEmpireId = aliveEmpires[0].Id
            };
        }

        // Economic: empire with > 10000 credits
        foreach (var empire in empires)
        {
            if (empire.Credits >= 10000)
            {
                return new VictoryResult
                {
                    Type = VictoryType.Economic,
                    WinnerEmpireId = empire.Id
                };
            }
        }

        return new VictoryResult { Type = VictoryType.None };
    }
}
