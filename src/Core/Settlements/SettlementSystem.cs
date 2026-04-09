using System;
using System.Collections.Generic;
using DerlictEmpires.Core.Production;

namespace DerlictEmpires.Core.Settlements;

/// <summary>
/// Processes all colonies each slow tick: production queues, happiness, growth.
/// Pure C#.
/// </summary>
public class SettlementSystem
{
    public event Action<Colony, string>? BuildingCompleted; // colony, buildingId
    public event Action<Colony>? PopulationGrew;
    public event Action<Colony>? PopulationShrunk;

    private readonly List<Colony> _colonies = new();
    private readonly List<Outpost> _outposts = new();

    public IReadOnlyList<Colony> Colonies => _colonies;
    public IReadOnlyList<Outpost> Outposts => _outposts;

    public void AddColony(Colony colony)
    {
        _colonies.Add(colony);
        colony.Queue.ItemCompleted += item => OnBuildingCompleted(colony, item);
    }

    public void AddOutpost(Outpost outpost)
    {
        _outposts.Add(outpost);
    }

    /// <summary>Process one slow tick for all settlements.</summary>
    public void ProcessTick(float tickDelta)
    {
        foreach (var colony in _colonies)
        {
            // 1. Update happiness
            colony.Happiness = HappinessCalculator.Calculate(colony);

            // 2. Process production queue
            int prodPoints = (int)colony.EffectiveProductionOutput;
            if (prodPoints > 0 && !colony.Queue.IsEmpty)
                colony.Queue.ProcessTick(prodPoints);

            // 3. Process population growth
            bool grew = PopGrowthCalculator.ProcessTick(colony, tickDelta);
            if (grew)
            {
                PopulationGrew?.Invoke(colony);
                // Re-auto-allocate if using auto
                PopAllocationManager.AutoAllocate(colony);
            }
        }
    }

    private void OnBuildingCompleted(Colony colony, IProducible item)
    {
        colony.Buildings.Add(item.Id);
        BuildingCompleted?.Invoke(colony, item.Id);
    }
}
