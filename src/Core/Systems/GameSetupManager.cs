using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;
using DerlictEmpires.Core.Ships;

namespace DerlictEmpires.Core.Systems;

/// <summary>
/// Creates empires, assigns starting systems, and spawns starting assets.
/// Pure C# — no Godot dependency.
/// </summary>
public class GameSetupManager
{
    private int _nextEmpireId;
    private int _nextColonyId;
    private int _nextStationId;
    private int _nextFleetId;
    private int _nextShipId;

    public class SetupResult
    {
        public List<EmpireData> Empires { get; set; } = new();
        public List<ColonyData> Colonies { get; set; } = new();
        public List<StationData> Stations { get; set; } = new();
        public List<FleetData> Fleets { get; set; } = new();
        public List<ShipInstanceData> Ships { get; set; } = new();
    }

    /// <summary>
    /// Create a human player empire with the given affinity and origin.
    /// Finds a suitable home system in the galaxy.
    /// </summary>
    public EmpireData CreatePlayerEmpire(
        string name,
        PrecursorColor? affinity,
        Origin origin,
        GalaxyData galaxy,
        SetupResult result,
        GameRandom rng)
    {
        var empire = new EmpireData
        {
            Id = _nextEmpireId++,
            Name = name,
            IsHuman = true,
            Affinity = origin == Origin.FreeRace ? null : affinity,
            Origin = origin,
            Credits = StartingConditions.GetForOrigin(origin).StartingCredits
        };

        var homeSystem = FindHomeSystem(galaxy, affinity, origin, result, rng);
        empire.HomeSystemId = homeSystem.Id;

        SpawnStartingAssets(empire, homeSystem, galaxy, result, rng);
        result.Empires.Add(empire);
        return empire;
    }

    /// <summary>
    /// Create an AI empire with random affinity and origin.
    /// </summary>
    public EmpireData CreateAIEmpire(
        GalaxyData galaxy,
        SetupResult result,
        GameRandom rng)
    {
        var colors = Enum.GetValues<PrecursorColor>();
        var origins = Enum.GetValues<Origin>();

        var affinity = colors[rng.RangeInt(colors.Length)];
        var origin = origins[rng.RangeInt(origins.Length)];

        var empire = new EmpireData
        {
            Id = _nextEmpireId++,
            Name = GenerateEmpireName(rng),
            IsHuman = false,
            Affinity = origin == Origin.FreeRace ? null : affinity,
            Origin = origin,
            Credits = StartingConditions.GetForOrigin(origin).StartingCredits
        };

        var homeSystem = FindHomeSystem(galaxy, affinity, origin, result, rng);
        empire.HomeSystemId = homeSystem.Id;

        SpawnStartingAssets(empire, homeSystem, galaxy, result, rng);
        result.Empires.Add(empire);
        return empire;
    }

    private StarSystemData FindHomeSystem(
        GalaxyData galaxy,
        PrecursorColor? affinity,
        Origin origin,
        SetupResult result,
        GameRandom rng)
    {
        // Prefer systems in the affinity-color arm with a habitable planet
        var usedSystems = new HashSet<int>(result.Empires.Select(e => e.HomeSystemId));

        // First pass: matching arm with habitable planet, not already taken
        var candidates = galaxy.Systems
            .Where(s => !usedSystems.Contains(s.Id))
            .Where(s => s.POIs.Any(p => p.Type == POIType.HabitablePlanet))
            .ToList();

        if (affinity.HasValue)
        {
            var affinityCandidates = candidates
                .Where(s => s.DominantColor == affinity.Value)
                .ToList();
            if (affinityCandidates.Count > 0)
                candidates = affinityCandidates;
        }

        if (candidates.Count == 0)
        {
            // Fallback: any system with a habitable planet
            candidates = galaxy.Systems
                .Where(s => !usedSystems.Contains(s.Id))
                .Where(s => s.POIs.Any(p => p.Type == POIType.HabitablePlanet))
                .ToList();
        }

        if (candidates.Count == 0)
        {
            // Last resort: any unused system
            candidates = galaxy.Systems
                .Where(s => !usedSystems.Contains(s.Id))
                .ToList();
        }

        return candidates[rng.RangeInt(candidates.Count)];
    }

    private void SpawnStartingAssets(
        EmpireData empire,
        StarSystemData homeSystem,
        GalaxyData galaxy,
        SetupResult result,
        GameRandom rng)
    {
        var startAssets = StartingConditions.GetForOrigin(empire.Origin);

        // Give starting resources in affinity color
        if (empire.Affinity.HasValue)
        {
            var c = empire.Affinity.Value;
            empire.AddResource(c, ResourceType.SimpleOre, startAssets.StartingSimpleOre);
            empire.AddResource(c, ResourceType.AdvancedOre, startAssets.StartingAdvancedOre);
            empire.AddResource(c, ResourceType.SimpleEnergy, startAssets.StartingSimpleEnergy);
            empire.AddResource(c, ResourceType.AdvancedEnergy, startAssets.StartingAdvancedEnergy);
            empire.AddResource(c, ResourceType.BasicComponent, startAssets.StartingBasicComponents);
            empire.AddResource(c, ResourceType.AdvancedComponent, startAssets.StartingAdvancedComponents);
        }
        else
        {
            // Free Race: small amount of each color
            foreach (var c in Enum.GetValues<PrecursorColor>())
            {
                empire.AddResource(c, ResourceType.SimpleOre, startAssets.StartingSimpleOre / 5f);
                empire.AddResource(c, ResourceType.SimpleEnergy, startAssets.StartingSimpleEnergy / 5f);
                empire.AddResource(c, ResourceType.BasicComponent, startAssets.StartingBasicComponents / 5f);
            }
        }

        // Create home colony on the habitable planet
        var habitablePoi = homeSystem.POIs.FirstOrDefault(p => p.Type == POIType.HabitablePlanet);
        if (habitablePoi != null)
        {
            var colony = new ColonyData
            {
                Id = _nextColonyId++,
                Name = $"{empire.Name} Prime",
                OwnerEmpireId = empire.Id,
                SystemId = homeSystem.Id,
                POIId = habitablePoi.Id,
                PlanetSize = habitablePoi.PlanetSize,
                Population = 3,
                Happiness = 75f
            };
            result.Colonies.Add(colony);
        }

        // Create starting station with shipyard
        var station = new StationData
        {
            Id = _nextStationId++,
            Name = $"{empire.Name} Starport",
            OwnerEmpireId = empire.Id,
            SystemId = homeSystem.Id,
            POIId = habitablePoi?.Id ?? homeSystem.POIs[0].Id,
            SizeTier = 1,
            InstalledModules = new List<string> { "Shipyard" }
        };
        result.Stations.Add(station);

        // Create starting fleet with ships
        var fleet = new FleetData
        {
            Id = _nextFleetId++,
            Name = $"{empire.Name} Home Fleet",
            OwnerEmpireId = empire.Id,
            CurrentSystemId = homeSystem.Id,
            Speed = 10f
        };

        foreach (var template in startAssets.Ships)
        {
            var ship = new ShipInstanceData
            {
                Id = _nextShipId++,
                Name = template.Name,
                OwnerEmpireId = empire.Id,
                SizeClass = template.SizeClass,
                Role = template.Role,
                MaxHp = template.Hp,
                CurrentHp = template.Hp,
                FleetId = fleet.Id
            };
            result.Ships.Add(ship);
            fleet.ShipIds.Add(ship.Id);
        }

        result.Fleets.Add(fleet);
    }

    private static readonly string[] EmpireNames =
    {
        "Terran Compact", "Void Collective", "Iron Hegemony", "Starlight Union",
        "Crimson Domain", "Azure Syndicate", "Verdant Pact", "Golden League",
        "Obsidian Order", "Scrap Lords", "Frontier Alliance", "Deep Core Remnant",
        "Nebula Wanderers", "Forge Imperium", "Silent Network", "Drift Nomads",
        "Beacon Republic", "Shade Conclave", "Solar Cooperative", "Voidborn Clan",
        "Steel Communion", "Crystal Monarchs", "Bio-League", "Transit Federation",
        "Enigma Circle", "Rust Brotherhood", "Data Consortium", "Genome Collective",
        "Trade Nexus", "Shadow Parliament"
    };

    private string GenerateEmpireName(GameRandom rng) =>
        EmpireNames[rng.RangeInt(EmpireNames.Length)];

    /// <summary>
    /// MVP salvage-loop seed: one human empire, two single-ship fleets (Scout + Salvager)
    /// both docked at the home system. No colony/station/AI rivals.
    /// </summary>
    public EmpireData CreateMvpPlayerEmpire(
        string name,
        PrecursorColor affinity,
        Origin origin,
        GalaxyData galaxy,
        SetupResult result,
        GameRandom rng)
    {
        var empire = new EmpireData
        {
            Id = _nextEmpireId++,
            Name = name,
            IsHuman = true,
            Affinity = affinity,
            Origin = origin,
            Credits = StartingConditions.GetForOrigin(origin).StartingCredits,
        };

        var home = FindHomeSystem(galaxy, affinity, origin, result, rng);
        empire.HomeSystemId = home.Id;

        // Seed starting resources in affinity color.
        var starting = StartingConditions.GetForOrigin(origin);
        empire.AddResource(affinity, ResourceType.SimpleOre, starting.StartingSimpleOre);
        empire.AddResource(affinity, ResourceType.SimpleEnergy, starting.StartingSimpleEnergy);
        empire.AddResource(affinity, ResourceType.BasicComponent, starting.StartingBasicComponents);

        // Scout fleet — single scout ship.
        var scoutFleet = MakeFleet(empire.Id, "Scout Alpha", home.Id, MvpShipDesigns.Scout.Speed);
        var scout = MakeShip(empire.Id, "Scout Alpha", MvpShipDesigns.Scout, ShipSizeClass.Corvette, scoutFleet.Id, 60);
        scoutFleet.ShipIds.Add(scout.Id);
        result.Ships.Add(scout);
        result.Fleets.Add(scoutFleet);

        // Salvager fleet — single salvager ship.
        var salvagerFleet = MakeFleet(empire.Id, "Salvager Bravo", home.Id, MvpShipDesigns.Salvager.Speed);
        var salvager = MakeShip(empire.Id, "Salvager Bravo", MvpShipDesigns.Salvager, ShipSizeClass.Frigate, salvagerFleet.Id, 80);
        salvagerFleet.ShipIds.Add(salvager.Id);
        result.Ships.Add(salvager);
        result.Fleets.Add(salvagerFleet);

        // Destroyer fleet — warship so the player can engage the neighbouring hostile.
        var destroyerFleet = MakeFleet(empire.Id, "Destroyer Vanguard", home.Id, MvpShipDesigns.Destroyer.Speed);
        var destroyer = MakeShip(empire.Id, "Destroyer Vanguard", MvpShipDesigns.Destroyer, ShipSizeClass.Destroyer, destroyerFleet.Id, 220);
        destroyerFleet.ShipIds.Add(destroyer.Id);
        result.Ships.Add(destroyer);
        result.Fleets.Add(destroyerFleet);

        result.Empires.Add(empire);
        return empire;
    }

    /// <summary>
    /// MVP: seed a lightweight hostile AI empire with a single warship fleet in a system
    /// adjacent to the player's home. Falls back to the home system if no neighbours exist.
    /// </summary>
    public EmpireData? CreateMvpHostileNeighbor(
        EmpireData player,
        GalaxyData galaxy,
        SetupResult result,
        GameRandom rng)
    {
        var home = galaxy.GetSystem(player.HomeSystemId);
        if (home == null) return null;

        var neighborIds = galaxy.GetNeighbors(home.Id).ToList();
        int hostileSystemId = neighborIds.Count > 0
            ? neighborIds[rng.RangeInt(neighborIds.Count)]
            : home.Id;

        var hostile = new EmpireData
        {
            Id = _nextEmpireId++,
            Name = "Red Raiders",
            IsHuman = false,
            Affinity = PrecursorColor.Red,
            Origin = Origin.Warriors,
            HomeSystemId = hostileSystemId,
        };

        var raiderFleet = MakeFleet(hostile.Id, "Raider Doom", hostileSystemId, MvpShipDesigns.Destroyer.Speed);
        var raider = MakeShip(hostile.Id, "Raider Doom", MvpShipDesigns.Destroyer, ShipSizeClass.Destroyer, raiderFleet.Id, 220);
        raiderFleet.ShipIds.Add(raider.Id);
        result.Ships.Add(raider);
        result.Fleets.Add(raiderFleet);

        result.Empires.Add(hostile);
        return hostile;
    }

    private FleetData MakeFleet(int empireId, string name, int systemId, float speed) => new()
    {
        Id = _nextFleetId++,
        Name = name,
        OwnerEmpireId = empireId,
        CurrentSystemId = systemId,
        Speed = speed,
    };

    private ShipInstanceData MakeShip(int empireId, string name, ShipDesign design, ShipSizeClass size, int fleetId, int hp) => new()
    {
        Id = _nextShipId++,
        Name = name,
        OwnerEmpireId = empireId,
        SizeClass = size,
        Role = design.Name,
        ShipDesignId = design.Id,
        MaxHp = hp,
        CurrentHp = hp,
        FleetId = fleetId,
    };
}
