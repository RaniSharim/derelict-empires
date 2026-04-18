using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// Places salvage sites on eligible POIs during galaxy generation.
/// Pure C#. Runs after POIGenerator.
/// </summary>
public static class SalvagePlacement
{
    /// <summary>Fraction of eligible POIs that spawn a salvage site.</summary>
    public const float SalvageChance = 0.4f;

    /// <summary>Bias toward the hosting system's dominant color.</summary>
    public const float AlignedColorChance = 0.7f;

    public static List<SalvageSiteData> Populate(
        List<StarSystemData> systems, GameRandom rng)
    {
        var sites = new List<SalvageSiteData>();

        foreach (var system in systems)
        {
            foreach (var poi in system.POIs)
            {
                if (!IsEligible(poi.Type)) continue;
                if (!rng.Chance(SalvageChance)) continue;

                var siteType = PickSiteType(poi.Type, rng);
                var primaryColor = PickColor(system.DominantColor, rng);

                var site = new SalvageSiteData
                {
                    Id = sites.Count,
                    POIId = poi.Id,
                    Type = siteType,
                    Color = primaryColor,
                    ScanDifficulty = SalvageYieldTable.Get(siteType).ScanDifficulty,
                    DepletionCurveExponent = 0.5f,
                };
                site.TotalYield = SalvageYieldTable.GenerateYield(siteType, primaryColor, rng);
                site.RemainingYield = new Dictionary<string, float>(site.TotalYield);

                sites.Add(site);
                poi.SalvageSiteId = site.Id;
            }
        }

        return sites;
    }

    private static bool IsEligible(POIType type) => type switch
    {
        POIType.HabitablePlanet => false,
        POIType.BarrenPlanet    => false,
        _                       => true,
    };

    private static SalvageSiteType PickSiteType(POIType poiType, GameRandom rng)
    {
        // POI type biases the site type: debris fields host debris-field salvage, etc.
        return poiType switch
        {
            POIType.DebrisField      => rng.Chance(0.7f) ? SalvageSiteType.DebrisField : SalvageSiteType.MinorDerelict,
            POIType.ShipGraveyard    => rng.Chance(0.7f) ? SalvageSiteType.ShipGraveyard : SalvageSiteType.FailedSalvagerWreck,
            POIType.AbandonedStation => rng.Chance(0.5f) ? SalvageSiteType.DesperationProject : SalvageSiteType.MajorPrecursorSite,
            POIType.AsteroidField    => rng.Chance(0.6f) ? SalvageSiteType.MinorDerelict : SalvageSiteType.DebrisField,
            POIType.Megastructure    => rng.Chance(0.5f) ? SalvageSiteType.MajorPrecursorSite : SalvageSiteType.PrecursorIntersection,
            _                        => SalvageSiteType.MinorDerelict,
        };
    }

    private static PrecursorColor PickColor(PrecursorColor? systemColor, GameRandom rng)
    {
        if (systemColor.HasValue && rng.Chance(AlignedColorChance))
            return systemColor.Value;
        return (PrecursorColor)rng.RangeInt(5);
    }
}
