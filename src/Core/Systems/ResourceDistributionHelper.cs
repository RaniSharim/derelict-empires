using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Core.Systems;

/// <summary>
/// Utility for setting up initial extraction assignments at an empire's home colony.
/// Creates assignments for all deposits at the home system's POIs.
/// </summary>
public static class ResourceDistributionHelper
{
    /// <summary>
    /// Create extraction assignments for all deposits at the given system's POIs.
    /// </summary>
    public static List<ExtractionAssignment> CreateHomeExtractions(
        int empireId,
        StarSystemData homeSystem,
        int startingAssignmentId = 0)
    {
        var assignments = new List<ExtractionAssignment>();
        int nextId = startingAssignmentId;

        foreach (var poi in homeSystem.POIs)
        {
            for (int depositIdx = 0; depositIdx < poi.Deposits.Count; depositIdx++)
            {
                assignments.Add(new ExtractionAssignment
                {
                    Id = nextId++,
                    OwnerEmpireId = empireId,
                    SystemId = homeSystem.Id,
                    POIId = poi.Id,
                    DepositIndex = depositIdx,
                    EfficiencyMultiplier = 1.0f,
                    WorkerCount = 1
                });
            }
        }

        return assignments;
    }

    /// <summary>
    /// Get a summary of resource deposits at a system, grouped by color and type.
    /// </summary>
    public static Dictionary<string, (float total, float remaining, float rate)> GetSystemResourceSummary(
        StarSystemData system)
    {
        var summary = new Dictionary<string, (float total, float remaining, float rate)>();

        foreach (var poi in system.POIs)
        foreach (var deposit in poi.Deposits)
        {
            var key = EmpireData.ResourceKey(deposit.Color, deposit.Type);
            var existing = summary.GetValueOrDefault(key);
            summary[key] = (
                existing.total + deposit.TotalAmount,
                existing.remaining + deposit.RemainingAmount,
                existing.rate + deposit.BaseExtractionRate
            );
        }

        return summary;
    }
}
