using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Stations;
using DerlictEmpires.Core.Random;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Core.Systems;

/// <summary>
/// Converts between save data models and runtime objects.
/// </summary>
public static class StateConverter
{
    // ── Station ↔ StationData ────────────────────────────────────

    public static Station ToStation(StationData data)
    {
        var station = new Station
        {
            Id = data.Id,
            Name = data.Name,
            OwnerEmpireId = data.OwnerEmpireId,
            SystemId = data.SystemId,
            POIId = data.POIId,
            SizeTier = data.SizeTier,
        };

        foreach (var moduleName in data.InstalledModules)
        {
            var module = CreateModule(moduleName);
            if (module != null)
                station.Modules.Add(module);
        }

        return station;
    }

    public static StationData ToStationData(Station station)
    {
        return new StationData
        {
            Id = station.Id,
            Name = station.Name,
            OwnerEmpireId = station.OwnerEmpireId,
            SystemId = station.SystemId,
            POIId = station.POIId,
            SizeTier = station.SizeTier,
            InstalledModules = station.Modules.Select(m => m.Type.ToString()).ToList(),
        };
    }

    private static StationModule? CreateModule(string typeName)
    {
        return typeName switch
        {
            "Shipyard" => new ShipyardModule(),
            "Defense" => new DefenseModule(),
            "Logistics" => new LogisticsModule(),
            "Trade" => new TradeModule(),
            "Garrison" => new GarrisonModule(),
            "Sensors" => new SensorModule(),
            _ => null
        };
    }

    // ── ResearchState ↔ ResearchSaveData ─────────────────────────

    public static EmpireResearchState ToResearchState(ResearchSaveData data)
    {
        var state = new EmpireResearchState { EmpireId = data.EmpireId, IsCreative = data.IsCreative };

        foreach (var id in data.AvailableSubsystems) state.AvailableSubsystems.Add(id);
        foreach (var id in data.ResearchedSubsystems) state.ResearchedSubsystems.Add(id);
        foreach (var id in data.LockedSubsystems) state.LockedSubsystems.Add(id);
        foreach (var id in data.AvailableSynergies) state.AvailableSynergies.Add(id);
        foreach (var id in data.ResearchedSynergies) state.ResearchedSynergies.Add(id);

        state.CurrentProject = data.CurrentProject;
        state.CurrentProgress = data.CurrentProgress;
        state.Queue.AddRange(data.Queue);
        state.CurrentTierProject = data.CurrentTierProject;
        state.CurrentTierProgress = data.CurrentTierProgress;
        state.TierQueue.AddRange(data.TierQueue);
        state.ImportUnlockedTiers(data.UnlockedTiers);

        return state;
    }

    public static ResearchSaveData ToResearchSaveData(EmpireResearchState state)
    {
        return new ResearchSaveData
        {
            EmpireId = state.EmpireId,
            IsCreative = state.IsCreative,
            AvailableSubsystems = state.AvailableSubsystems.ToList(),
            ResearchedSubsystems = state.ResearchedSubsystems.ToList(),
            LockedSubsystems = state.LockedSubsystems.ToList(),
            AvailableSynergies = state.AvailableSynergies.ToList(),
            ResearchedSynergies = state.ResearchedSynergies.ToList(),
            CurrentProject = state.CurrentProject,
            CurrentProgress = state.CurrentProgress,
            Queue = new List<string>(state.Queue),
            CurrentTierProject = state.CurrentTierProject,
            CurrentTierProgress = state.CurrentTierProgress,
            TierQueue = new List<string>(state.TierQueue),
            UnlockedTiers = state.ExportUnlockedTiers(),
        };
    }

    /// <summary>
    /// Create a fresh research state for a new empire.
    /// Unlocks tier 1 of the empire's affinity color (all categories).
    /// </summary>
    public static EmpireResearchState CreateInitialResearchState(
        int empireId, PrecursorColor? affinity, TechTreeRegistry registry, GameRandom rng)
    {
        var state = new EmpireResearchState { EmpireId = empireId };

        if (affinity == null) return state;

        // Unlock tier 1 of the affinity color in all categories
        var color = affinity.Value;
        foreach (var category in System.Enum.GetValues<TechCategory>())
        {
            var node = registry.GetNode(color, category, 1);
            if (node != null)
            {
                var childRng = rng.DeriveChild(empireId * 100 + (int)category);
                state.UnlockTier(color, category, 1, node, childRng);
            }
        }

        return state;
    }
}
