using System;
using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Core.Systems;

/// <summary>
/// Generates 3-5 points of interest per star system.
/// Core systems get more debris/graveyards; rim systems get more asteroids/habitables.
/// Ensures at least one habitable planet per arm.
/// </summary>
public static class POIGenerator
{
    public static void Generate(List<StarSystemData> systems, int armCount, GameRandom rng)
    {
        var armHasHabitable = new bool[armCount];
        int nextPoiId = 0;

        foreach (var system in systems)
        {
            var sysRng = rng.DeriveChild(system.Id);
            int poiCount = sysRng.RangeInt(3, 6); // 3-5

            for (int i = 0; i < poiCount; i++)
            {
                var poi = GeneratePOI(system, sysRng, ref nextPoiId);
                system.POIs.Add(poi);

                if (poi.Type == POIType.HabitablePlanet && system.ArmIndex >= 0)
                    armHasHabitable[system.ArmIndex] = true;
            }
        }

        // Ensure at least one habitable planet per arm
        for (int arm = 0; arm < armCount; arm++)
        {
            if (armHasHabitable[arm]) continue;

            // Find a system in this arm and force a habitable planet
            var armSystems = systems.FindAll(s => s.ArmIndex == arm);
            if (armSystems.Count == 0) continue;

            var target = armSystems[rng.RangeInt(armSystems.Count)];
            // Replace the first non-habitable POI or add one
            bool replaced = false;
            foreach (var poi in target.POIs)
            {
                if (poi.Type != POIType.HabitablePlanet)
                {
                    poi.Type = POIType.HabitablePlanet;
                    poi.Name = $"Habitable World {arm + 1}";
                    poi.PlanetSize = PlanetSize.Medium;
                    poi.DominantColor = target.DominantColor;
                    replaced = true;
                    break;
                }
            }

            if (!replaced)
            {
                target.POIs.Add(new POIData
                {
                    Id = nextPoiId++,
                    Name = $"Habitable World {arm + 1}",
                    Type = POIType.HabitablePlanet,
                    PlanetSize = PlanetSize.Medium,
                    DominantColor = target.DominantColor
                });
            }
        }
    }

    private static POIData GeneratePOI(StarSystemData system, GameRandom rng, ref int nextId)
    {
        var type = PickPOIType(system, rng);
        var poi = new POIData
        {
            Id = nextId++,
            Type = type,
            DominantColor = system.DominantColor,
            Terrain = PickTerrain(rng)
        };

        switch (type)
        {
            case POIType.HabitablePlanet:
                poi.PlanetSize = PickPlanetSize(rng);
                poi.Name = GeneratePlanetName(rng, "Habitable");
                break;
            case POIType.BarrenPlanet:
                poi.PlanetSize = PickPlanetSize(rng);
                poi.Name = GeneratePlanetName(rng, "Barren");
                break;
            case POIType.AsteroidField:
                poi.Name = $"Asteroid Field {rng.RangeInt(100, 999)}";
                break;
            case POIType.DebrisField:
                poi.Name = $"Debris Field {rng.RangeInt(100, 999)}";
                break;
            case POIType.AbandonedStation:
                poi.Name = $"Abandoned Station {rng.RangeInt(10, 99)}";
                break;
            case POIType.ShipGraveyard:
                poi.Name = $"Ship Graveyard {rng.RangeInt(10, 99)}";
                break;
            case POIType.Megastructure:
                poi.Name = $"Precursor Megastructure";
                break;
        }

        // Generate resource deposits for applicable POI types
        GenerateDeposits(poi, system, rng);

        return poi;
    }

    private static POIType PickPOIType(StarSystemData system, GameRandom rng)
    {
        // Core systems: more debris, graveyards, stations
        // Rim systems: more asteroids, habitables
        float[] weights;

        if (system.IsCore)
        {
            weights = new float[]
            {
                0.05f, // HabitablePlanet — scarce in core
                0.15f, // BarrenPlanet
                0.10f, // AsteroidField
                0.25f, // DebrisField — dense in core
                0.15f, // AbandonedStation
                0.25f, // ShipGraveyard — dense in core
                0.05f  // Megastructure — rare
            };
        }
        else if (system.RadialPosition > 0.7f)
        {
            // Outer rim
            weights = new float[]
            {
                0.20f, // HabitablePlanet — more pristine at rim
                0.20f, // BarrenPlanet
                0.30f, // AsteroidField — especially common in rim
                0.10f, // DebrisField
                0.10f, // AbandonedStation
                0.08f, // ShipGraveyard
                0.02f  // Megastructure
            };
        }
        else
        {
            // Mid-galaxy
            weights = new float[]
            {
                0.12f, // HabitablePlanet
                0.18f, // BarrenPlanet
                0.20f, // AsteroidField
                0.18f, // DebrisField
                0.15f, // AbandonedStation
                0.14f, // ShipGraveyard
                0.03f  // Megastructure
            };
        }

        int idx = rng.WeightedChoice(weights);
        return (POIType)idx;
    }

    private static PlanetSize PickPlanetSize(GameRandom rng)
    {
        float[] weights = { 0.30f, 0.35f, 0.20f, 0.12f, 0.03f };
        int idx = rng.WeightedChoice(weights);
        return idx switch
        {
            0 => PlanetSize.Small,
            1 => PlanetSize.Medium,
            2 => PlanetSize.Large,
            3 => PlanetSize.Prime,
            4 => PlanetSize.Exceptional,
            _ => PlanetSize.Medium
        };
    }

    private static TerrainModifier PickTerrain(GameRandom rng)
    {
        if (rng.Chance(0.7f)) return TerrainModifier.None; // 70% no modifier
        float[] weights = { 0.25f, 0.25f, 0.25f, 0.25f };
        int idx = rng.WeightedChoice(weights);
        return idx switch
        {
            0 => TerrainModifier.NebulaPocket,
            1 => TerrainModifier.RadiationZone,
            2 => TerrainModifier.GravityAnomaly,
            3 => TerrainModifier.DenseAsteroidCluster,
            _ => TerrainModifier.None
        };
    }

    private static void GenerateDeposits(POIData poi, StarSystemData system, GameRandom rng)
    {
        var color = poi.DominantColor ?? PrecursorColor.Red;

        switch (poi.Type)
        {
            case POIType.HabitablePlanet:
                // Habitable planets have food-like resources (simple energy/parts of their color)
                AddDeposit(poi, color, ResourceType.SimpleEnergy, rng, 500f, 800f, 2f, 4f);
                if (rng.Chance(0.5f))
                    AddDeposit(poi, color, ResourceType.BasicComponent, rng, 200f, 400f, 1f, 2f);
                break;

            case POIType.BarrenPlanet:
                AddDeposit(poi, color, ResourceType.BasicComponent, rng, 300f, 600f, 1.5f, 3f);
                if (rng.Chance(0.3f))
                    AddDeposit(poi, color, ResourceType.SimpleEnergy, rng, 100f, 300f, 0.5f, 1.5f);
                break;

            case POIType.AsteroidField:
                AddDeposit(poi, color, ResourceType.BasicComponent, rng, 600f, 1200f, 3f, 5f);
                if (rng.Chance(0.2f))
                    AddDeposit(poi, color, ResourceType.AdvancedComponent, rng, 50f, 150f, 0.3f, 0.8f);
                break;

            case POIType.DebrisField:
                AddDeposit(poi, color, ResourceType.BasicComponent, rng, 200f, 500f, 2f, 4f);
                AddDeposit(poi, color, ResourceType.SimpleEnergy, rng, 100f, 300f, 1f, 3f);
                if (rng.Chance(0.4f))
                    AddDeposit(poi, color, ResourceType.AdvancedComponent, rng, 30f, 100f, 0.2f, 0.5f);
                break;

            case POIType.AbandonedStation:
                AddDeposit(poi, color, ResourceType.AdvancedEnergy, rng, 50f, 200f, 0.3f, 1f);
                AddDeposit(poi, color, ResourceType.AdvancedComponent, rng, 50f, 200f, 0.3f, 1f);
                break;

            case POIType.ShipGraveyard:
                AddDeposit(poi, color, ResourceType.BasicComponent, rng, 400f, 800f, 3f, 6f);
                AddDeposit(poi, color, ResourceType.AdvancedComponent, rng, 80f, 250f, 0.5f, 1.5f);
                if (rng.Chance(0.3f))
                    AddDeposit(poi, color, ResourceType.AdvancedEnergy, rng, 30f, 100f, 0.2f, 0.6f);
                break;

            case POIType.Megastructure:
                // Megastructures have rich deposits of all types
                AddDeposit(poi, color, ResourceType.AdvancedEnergy, rng, 200f, 500f, 1f, 3f);
                AddDeposit(poi, color, ResourceType.AdvancedComponent, rng, 200f, 500f, 1f, 3f);
                AddDeposit(poi, color, ResourceType.SimpleEnergy, rng, 500f, 1000f, 3f, 6f);
                AddDeposit(poi, color, ResourceType.BasicComponent, rng, 500f, 1000f, 3f, 6f);
                break;
        }
    }

    private static void AddDeposit(
        POIData poi, PrecursorColor color, ResourceType type,
        GameRandom rng, float minAmt, float maxAmt, float minRate, float maxRate)
    {
        float amount = rng.RangeFloat(minAmt, maxAmt);
        poi.Deposits.Add(new ResourceDeposit
        {
            Color = color,
            Type = type,
            TotalAmount = amount,
            RemainingAmount = amount,
            BaseExtractionRate = rng.RangeFloat(minRate, maxRate)
        });
    }

    private static readonly string[] PlanetNames =
    {
        "Proxima", "Kepler", "Trappist", "Gliese", "Ross", "Wolf",
        "Barnard", "Luyten", "Lalande", "Sirius", "Altair", "Vega",
        "Rigel", "Deneb", "Antares", "Betelgeuse", "Pollux", "Arcturus"
    };

    private static string GeneratePlanetName(GameRandom rng, string prefix)
    {
        string name = PlanetNames[rng.RangeInt(PlanetNames.Length)];
        string numeral = rng.RangeInt(1, 8).ToString();
        return $"{name} {numeral}";
    }
}
