using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Production;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Core.Stations;

/// <summary>
/// Manages all stations: construction, module installation, and precursor station interactions.
/// Pure C#.
/// </summary>
public class StationSystem
{
    public event Action<Station>? StationConstructed;
    public event Action<Station, StationModule>? ModuleInstalled;
    public event Action<PrecursorStation, int>? PrecursorStationClaimed; // station, empireId
    public event Action<PrecursorStation>? PrecursorStationRepaired;
    public event Action<PrecursorStation, int, int>? PrecursorStationScavenged; // station, basic, advanced

    private readonly List<Station> _stations = new();
    private readonly List<StationConstructionJob> _pendingConstructions = new();
    private int _nextStationId;

    public IReadOnlyList<Station> Stations => _stations;

    public StationSystem(int startingId = 0)
    {
        _nextStationId = startingId;
    }

    public void AddStation(Station station)
    {
        _stations.Add(station);
        if (station.Id >= _nextStationId)
            _nextStationId = station.Id + 1;
    }

    /// <summary>Get all stations owned by an empire.</summary>
    public List<Station> GetStationsForEmpire(int empireId) =>
        _stations.Where(s => s.OwnerEmpireId == empireId).ToList();

    /// <summary>
    /// Start construction of a new station at a POI.
    /// Returns a construction job that needs production points invested.
    /// </summary>
    public StationConstructionJob StartConstruction(
        int empireId, int systemId, int poiId, int builderShipId, int constructionCost = 200)
    {
        var station = new Station
        {
            Id = _nextStationId++,
            Name = $"Station {_nextStationId}",
            OwnerEmpireId = empireId,
            SystemId = systemId,
            POIId = poiId,
            SizeTier = 1,
            IsConstructed = false,
            ConstructionProgress = 0f
        };
        _stations.Add(station);

        var job = new StationConstructionJob
        {
            StationId = station.Id,
            SystemId = systemId,
            POIId = poiId,
            BuilderShipId = builderShipId,
            ProductionCost = constructionCost
        };
        _pendingConstructions.Add(job);
        return job;
    }

    /// <summary>
    /// Invest production points into pending station constructions.
    /// Call each slow tick with the builder ship's production capacity.
    /// </summary>
    public void ProcessConstructionTick(int builderShipId, int productionPoints)
    {
        var job = _pendingConstructions.FirstOrDefault(j => j.BuilderShipId == builderShipId);
        if (job == null) return;

        // Find the station being built
        var station = _stations.FirstOrDefault(s => s.Id == job.StationId);
        if (station == null) return;

        // Simple progress tracking
        station.ConstructionProgress += (float)productionPoints / job.ProductionCost;

        if (station.ConstructionProgress >= 1.0f)
        {
            station.ConstructionProgress = 1.0f;
            station.IsConstructed = true;
            _pendingConstructions.Remove(job);
            StationConstructed?.Invoke(station);
        }
    }

    /// <summary>Queue a module for installation at a station.</summary>
    public bool QueueModuleInstall(Station station, StationModule module)
    {
        if (!station.CanInstallModule(module)) return false;

        var job = new ModuleInstallJob
        {
            StationId = station.Id,
            ModuleType = module.Type.ToString(),
            Module = module,
            ProductionCost = module.InstallCost,
            DisplayName = $"Install {module.DisplayName}"
        };
        station.ModuleQueue.Enqueue(job);
        return true;
    }

    /// <summary>Process module installation queues for all stations. Call each slow tick.</summary>
    public void ProcessModuleTick(int productionPoints)
    {
        foreach (var station in _stations)
        {
            if (!station.IsConstructed || station.ModuleQueue.IsEmpty) continue;

            var completed = station.ModuleQueue.ProcessTick(productionPoints);
            foreach (var item in completed)
            {
                if (item is ModuleInstallJob installJob && installJob.Module != null)
                {
                    station.InstallModule(installJob.Module);
                    ModuleInstalled?.Invoke(station, installJob.Module);
                }
            }
        }
    }

    /// <summary>Upgrade a station's size tier (requires tech check externally).</summary>
    public bool UpgradeSize(Station station)
    {
        if (station.SizeTier >= 5) return false;
        station.SizeTier++;
        station.BaseHp += 100;
        return true;
    }

    // === Precursor Station Interactions ===

    public bool ClaimPrecursorStation(PrecursorStation station, int empireId)
    {
        if (!station.Claim(empireId)) return false;
        PrecursorStationClaimed?.Invoke(station, empireId);
        return true;
    }

    public bool RepairPrecursorStation(PrecursorStation station)
    {
        if (!station.CompleteRepair()) return false;
        PrecursorStationRepaired?.Invoke(station);
        return true;
    }

    public (int basic, int advanced) ScavengePrecursorStation(PrecursorStation station)
    {
        var yield = station.Scavenge();
        PrecursorStationScavenged?.Invoke(station, yield.basic, yield.advanced);
        return yield;
    }

    /// <summary>
    /// Generate a precursor station with random modules based on color and tier.
    /// </summary>
    public static PrecursorStation GeneratePrecursorStation(
        int id, PrecursorColor color, int techTier, int systemId, int poiId, GameRandom rng)
    {
        var station = new PrecursorStation
        {
            Id = id,
            Name = $"Precursor Station ({color})",
            OwnerEmpireId = -1,
            SystemId = systemId,
            POIId = poiId,
            Color = color,
            TechTier = techTier,
            SizeTier = Math.Min(techTier, 5),
            BaseHp = 200 + techTier * 100,
            HazardLevel = 0.1f + techTier * 0.1f,
            RepairCost = 200 + techTier * 100,
            ScavengeYieldBasic = 10 + techTier * 5,
            ScavengeYieldAdvanced = techTier * 2,
        };

        // Add some precursor modules
        int moduleCount = Math.Min(station.MaxModuleSlots, rng.RangeInt(2, station.MaxModuleSlots + 1));
        var moduleTypes = new[] {
            StationModuleType.Defense, StationModuleType.Sensors,
            StationModuleType.Logistics, StationModuleType.Shipyard
        };

        for (int i = 0; i < moduleCount; i++)
        {
            var type = moduleTypes[rng.RangeInt(moduleTypes.Length)];
            StationModule module = type switch
            {
                StationModuleType.Defense => new DefenseModule
                    { WeaponDamage = 10 + techTier * 5, ShieldHp = 30 + techTier * 20, ArmorHp = 20 + techTier * 10 },
                StationModuleType.Sensors => new SensorModule
                    { DetectionRange = Math.Min(techTier, 3), SensorPower = 20 + techTier * 10 },
                StationModuleType.Logistics => new LogisticsModule
                    { SupplyCapacity = 30 + techTier * 20, RangeExtension = 1 },
                StationModuleType.Shipyard => new ShipyardModule
                    { MaxShipClass = Math.Min(techTier + 1, 6), ProductionRate = 1.0f + techTier * 0.2f },
                _ => new DefenseModule()
            };

            if (station.CanInstallModule(module))
                station.Modules.Add(module);
        }

        return station;
    }
}
