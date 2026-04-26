using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// Resolves a salvage site's special outcome (RepairStation, RecoverDerelict)
/// into a structured <see cref="Resolution"/>. Pure C# — does not touch
/// StationSystem or DerelictShip storage. Callers (GameSystems / MainScene)
/// apply the resolution to live runtime collections.
///
/// Validation order:
///   1. Site must have a non-null SpecialOutcomeId pointing at a known outcome.
///   2. Site progress must report SpecialOutcomeAvailable && !Consumed.
///   3. Empire must afford the cost (bare names like "BasicComponent" resolve
///      against the site's primary color; fully qualified keys map directly).
/// </summary>
public static class SalvageOutcomeProcessor
{
    public enum OutcomeKind
    {
        None,
        RepairStation,
        RecoverDerelict,
    }

    public readonly struct Resolution
    {
        public readonly bool Success;
        public readonly OutcomeKind Kind;
        public readonly string? FailureReason;
        public readonly Dictionary<string, float>? CostPaid;
        public readonly StationSpec? Station;
        public readonly DerelictShip? Derelict;

        public Resolution(bool success, OutcomeKind kind, string? failureReason = null,
                          Dictionary<string, float>? cost = null,
                          StationSpec? station = null, DerelictShip? derelict = null)
        {
            Success = success; Kind = kind; FailureReason = failureReason;
            CostPaid = cost; Station = station; Derelict = derelict;
        }

        public static Resolution Failure(string reason) =>
            new(false, OutcomeKind.None, reason);
    }

    public readonly record struct StationSpec(
        int OwnerEmpireId, int SystemId, int POIId,
        int ModuleSlots, PrecursorColor PrimaryColor);

    public static Resolution Resolve(
        EmpireData empire,
        SalvageSiteData site,
        SalvageSiteProgress progress,
        SalvageRegistry registry,
        int systemId)
    {
        if (string.IsNullOrEmpty(site.SpecialOutcomeId))
            return Resolution.Failure("site has no special outcome");
        var def = registry.GetOutcome(site.SpecialOutcomeId!);
        if (def == null)
            return Resolution.Failure($"unknown outcome '{site.SpecialOutcomeId}'");
        if (progress.SpecialOutcomeConsumed)
            return Resolution.Failure("outcome already consumed");
        if (!progress.SpecialOutcomeAvailable)
            return Resolution.Failure("outcome not yet available");

        // Resolve cost keys against the site's primary color when bare.
        var primary = site.Color;
        var resolvedCost = new Dictionary<string, float>(def.Cost.Count);
        foreach (var kv in def.Cost)
        {
            string key = ResolveResourceKey(kv.Key, primary);
            resolvedCost[key] = resolvedCost.GetValueOrDefault(key) + kv.Value;
        }

        // Affordability check.
        foreach (var kv in resolvedCost)
        {
            float have = empire.ResourceStockpile.GetValueOrDefault(kv.Key);
            if (have + 1e-4f < kv.Value)
                return Resolution.Failure($"insufficient {kv.Key} ({have:F0}/{kv.Value:F0})");
        }

        // Deduct.
        foreach (var kv in resolvedCost)
            empire.ResourceStockpile[kv.Key] = empire.ResourceStockpile.GetValueOrDefault(kv.Key) - kv.Value;

        progress.SpecialOutcomeAvailable = false;
        progress.SpecialOutcomeConsumed = true;

        // Build the resolution payload by action discriminator.
        return def.Action switch
        {
            "RepairStation" => BuildStationResolution(def, empire, site, systemId, resolvedCost),
            "RecoverDerelict" => BuildDerelictResolution(def, empire, site, systemId, resolvedCost),
            _ => new Resolution(false, OutcomeKind.None,
                                failureReason: $"unknown action '{def.Action}'",
                                cost: resolvedCost),
        };
    }

    private static Resolution BuildStationResolution(
        SalvageOutcomeDef def, EmpireData empire, SalvageSiteData site,
        int systemId, Dictionary<string, float> cost)
    {
        int slots = ParseInt(def, "moduleSlots", fallback: 4);
        var spec = new StationSpec(
            OwnerEmpireId: empire.Id,
            SystemId: systemId,
            POIId: site.POIId,
            ModuleSlots: slots,
            PrimaryColor: site.Color);
        return new Resolution(true, OutcomeKind.RepairStation, cost: cost, station: spec);
    }

    private static Resolution BuildDerelictResolution(
        SalvageOutcomeDef def, EmpireData empire, SalvageSiteData site,
        int systemId, Dictionary<string, float> cost)
    {
        var sizeStr = def.Params.GetValueOrDefault("sizeClass", "Frigate");
        if (!System.Enum.TryParse<ShipSizeClass>(sizeStr, out var size))
            size = ShipSizeClass.Frigate;

        var derelict = new DerelictShip
        {
            Id = site.Id, // sentinel — host overrides with a fresh id
            Name = $"Recovered {site.Name}",
            Color = site.Color,
            TechTier = site.Tier,
            SizeClass = size,
            Condition = 100f,
            POIId = site.POIId,
        };
        return new Resolution(true, OutcomeKind.RecoverDerelict, cost: cost, derelict: derelict);
    }

    private static string ResolveResourceKey(string key, PrecursorColor primary)
    {
        if (key.Contains('_')) return key; // already fully qualified
        if (System.Enum.TryParse<ResourceType>(key, out var t))
            return EmpireData.ResourceKey(primary, t);
        return key;
    }

    private static int ParseInt(SalvageOutcomeDef def, string paramKey, int fallback)
    {
        if (!def.Params.TryGetValue(paramKey, out var s)) return fallback;
        return int.TryParse(s, out var v) ? v : fallback;
    }
}
