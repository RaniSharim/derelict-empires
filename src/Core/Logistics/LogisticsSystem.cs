using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Combat;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Core.Logistics;

/// <summary>
/// Tracks fleet supply consumption and logistics network flow. Pure C#.
/// </summary>
public class LogisticsSystem
{
    public event Action<int, float>? FleetSupplyLow; // fleetId, supplyPercent

    /// <summary>
    /// Calculate supply consumption per tick for a fleet based on its loadout.
    /// Returns (energyCost, partsCost, foodCost).
    /// </summary>
    public static (float energy, float parts, float food) CalculateConsumption(
        int shipCount, float avgWeaponDamage, bool hasShields, bool hasArmor, int crewCount)
    {
        float energy = shipCount * 0.5f; // Base energy per ship
        float parts = shipCount * 0.3f;  // Base parts per ship
        float food = crewCount * 0.1f;   // Food per crew

        // Shields consume energy
        if (hasShields) energy += shipCount * 0.3f;

        // Weapons consume based on type
        energy += avgWeaponDamage * 0.1f; // Energy weapons
        parts += avgWeaponDamage * 0.05f; // Ammo/maintenance

        // Armor consumes parts for repair
        if (hasArmor) parts += shipCount * 0.2f;

        return (energy, parts, food);
    }
}

/// <summary>
/// Represents the supply network of logistics hubs connected by lanes.
/// Supply flows from hubs to fleets with distance-based waste.
/// </summary>
public class LogisticsNetwork
{
    public class LogisticsHub
    {
        public int SystemId { get; set; }
        public int EmpireId { get; set; }
        public float Capacity { get; set; }
        public float UsedCapacity { get; set; }
    }

    private readonly List<LogisticsHub> _hubs = new();

    public IReadOnlyList<LogisticsHub> Hubs => _hubs;

    public void AddHub(LogisticsHub hub) => _hubs.Add(hub);

    /// <summary>
    /// Calculate the supply available to a fleet at a given system.
    /// Supply decreases with hop distance from the nearest hub.
    /// </summary>
    public float CalculateSupplyAt(int empireId, int systemId, GalaxyData galaxy, int maxHops = 5)
    {
        var empireHubs = _hubs.Where(h => h.EmpireId == empireId).ToList();
        if (empireHubs.Count == 0) return 0f;

        float bestSupply = 0f;

        foreach (var hub in empireHubs)
        {
            if (hub.SystemId == systemId)
            {
                bestSupply = MathF.Max(bestSupply, hub.Capacity - hub.UsedCapacity);
                continue;
            }

            // BFS to find hop distance
            int hops = FindHopDistance(hub.SystemId, systemId, galaxy, maxHops);
            if (hops < 0) continue;

            // Waste per hop: 15% loss per hop
            float wasteMultiplier = MathF.Pow(0.85f, hops);
            float available = (hub.Capacity - hub.UsedCapacity) * wasteMultiplier;
            bestSupply = MathF.Max(bestSupply, available);
        }

        return bestSupply;
    }

    /// <summary>BFS hop distance. Returns -1 if unreachable within maxHops.</summary>
    private static int FindHopDistance(int from, int to, GalaxyData galaxy, int maxHops)
    {
        if (from == to) return 0;

        var visited = new HashSet<int> { from };
        var queue = new Queue<(int sysId, int hops)>();
        queue.Enqueue((from, 0));

        while (queue.Count > 0)
        {
            var (current, hops) = queue.Dequeue();
            if (hops >= maxHops) continue;

            foreach (int neighbor in galaxy.GetNeighbors(current))
            {
                if (neighbor == to) return hops + 1;
                if (visited.Add(neighbor))
                    queue.Enqueue((neighbor, hops + 1));
            }
        }

        return -1; // Unreachable
    }
}
