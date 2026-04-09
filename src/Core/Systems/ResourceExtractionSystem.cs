using System;
using System.Collections.Generic;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Core.Systems;

/// <summary>
/// Processes resource extraction each slow tick. Pure C#.
/// Extracts from deposits based on assignments, depletes them, and adds to empire stockpiles.
/// </summary>
public class ResourceExtractionSystem
{
    public event Action<int, ResourceDeposit>? DepositDepleted; // empireId, deposit
    public event Action<int, string, float>? ResourceExtracted; // empireId, resourceKey, amount

    private readonly List<ExtractionAssignment> _assignments = new();
    private readonly Dictionary<int, POIData> _poiLookup = new();

    /// <summary>Register a POI for lookup by ID.</summary>
    public void RegisterPOI(POIData poi)
    {
        _poiLookup[poi.Id] = poi;
    }

    /// <summary>Register all POIs from a galaxy.</summary>
    public void RegisterGalaxy(GalaxyData galaxy)
    {
        foreach (var sys in galaxy.Systems)
        foreach (var poi in sys.POIs)
            _poiLookup[poi.Id] = poi;
    }

    /// <summary>Add an extraction assignment.</summary>
    public void AddAssignment(ExtractionAssignment assignment)
    {
        _assignments.Add(assignment);
    }

    /// <summary>Remove all assignments for an empire at a specific POI.</summary>
    public void RemoveAssignments(int empireId, int poiId)
    {
        _assignments.RemoveAll(a => a.OwnerEmpireId == empireId && a.POIId == poiId);
    }

    /// <summary>Get all active assignments for an empire.</summary>
    public IReadOnlyList<ExtractionAssignment> GetAssignments(int empireId) =>
        _assignments.FindAll(a => a.OwnerEmpireId == empireId);

    /// <summary>Get all active assignments.</summary>
    public IReadOnlyList<ExtractionAssignment> AllAssignments => _assignments;

    /// <summary>
    /// Process one slow tick of extraction.
    /// </summary>
    public void ProcessTick(float tickDelta, IReadOnlyList<EmpireData> empires)
    {
        var empireLookup = new Dictionary<int, EmpireData>();
        foreach (var e in empires)
            empireLookup[e.Id] = e;

        for (int i = _assignments.Count - 1; i >= 0; i--)
        {
            var assignment = _assignments[i];
            if (!_poiLookup.TryGetValue(assignment.POIId, out var poi)) continue;
            if (assignment.DepositIndex < 0 || assignment.DepositIndex >= poi.Deposits.Count) continue;
            if (!empireLookup.TryGetValue(assignment.OwnerEmpireId, out var empire)) continue;

            var deposit = poi.Deposits[assignment.DepositIndex];
            if (deposit.RemainingAmount <= 0f) continue;

            // Calculate extraction amount
            float rate = deposit.BaseExtractionRate
                * assignment.EfficiencyMultiplier
                * assignment.WorkerCount
                * tickDelta;

            float extracted = Math.Min(rate, deposit.RemainingAmount);
            deposit.RemainingAmount -= extracted;

            // Add to empire stockpile
            empire.AddResource(deposit.Color, deposit.Type, extracted);
            var key = EmpireData.ResourceKey(deposit.Color, deposit.Type);
            ResourceExtracted?.Invoke(empire.Id, key, extracted);

            // Check depletion
            if (deposit.RemainingAmount <= 0f)
            {
                deposit.RemainingAmount = 0f;
                DepositDepleted?.Invoke(empire.Id, deposit);
                _assignments.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Calculate per-tick income for an empire (for UI display).
    /// Returns a dictionary of resourceKey -> amount per tick.
    /// </summary>
    public Dictionary<string, float> CalculateIncome(int empireId, float tickDelta)
    {
        var income = new Dictionary<string, float>();

        foreach (var assignment in _assignments)
        {
            if (assignment.OwnerEmpireId != empireId) continue;
            if (!_poiLookup.TryGetValue(assignment.POIId, out var poi)) continue;
            if (assignment.DepositIndex < 0 || assignment.DepositIndex >= poi.Deposits.Count) continue;

            var deposit = poi.Deposits[assignment.DepositIndex];
            if (deposit.RemainingAmount <= 0f) continue;

            float rate = deposit.BaseExtractionRate
                * assignment.EfficiencyMultiplier
                * assignment.WorkerCount
                * tickDelta;

            float projected = Math.Min(rate, deposit.RemainingAmount);
            var key = EmpireData.ResourceKey(deposit.Color, deposit.Type);
            income[key] = income.GetValueOrDefault(key) + projected;
        }

        return income;
    }
}
