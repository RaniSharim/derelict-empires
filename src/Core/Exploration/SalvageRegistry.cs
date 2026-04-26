using System;
using System.Collections.Generic;
using System.Text.Json;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// Loaded catalog of authored salvage data — site types, dangers, outcomes.
/// Pure C# / System.Text.Json so it works in unit tests without Godot.
/// The Godot-side <c>DataLoader</c> autoload reads the JSON files via
/// <c>FileAccess</c> and calls <see cref="Load"/>.
/// </summary>
public class SalvageRegistry
{
    private readonly Dictionary<string, SalvageSiteTypeDef> _types = new();
    private readonly Dictionary<string, SalvageDangerTypeDef> _dangers = new();
    private readonly Dictionary<string, SalvageOutcomeDef> _outcomes = new();

    public IReadOnlyDictionary<string, SalvageSiteTypeDef> Types => _types;
    public IReadOnlyDictionary<string, SalvageDangerTypeDef> Dangers => _dangers;
    public IReadOnlyDictionary<string, SalvageOutcomeDef> Outcomes => _outcomes;

    public SalvageSiteTypeDef? GetSiteType(string id) => _types.GetValueOrDefault(id);
    public SalvageDangerTypeDef? GetDanger(string id) => _dangers.GetValueOrDefault(id);
    public SalvageOutcomeDef? GetOutcome(string id) => _outcomes.GetValueOrDefault(id);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static SalvageRegistry Load(string typesJson, string dangersJson, string outcomesJson)
    {
        var reg = new SalvageRegistry();

        foreach (var def in Deserialize<SalvageSiteTypeDef[]>(typesJson, "salvage_types"))
            ValidateAndAdd(reg._types, def, def.Id, "salvage type");

        foreach (var def in Deserialize<SalvageDangerTypeDef[]>(dangersJson, "salvage_dangers"))
            ValidateAndAdd(reg._dangers, def, def.Id, "danger type");

        foreach (var def in Deserialize<SalvageOutcomeDef[]>(outcomesJson, "salvage_outcomes"))
            ValidateAndAdd(reg._outcomes, def, def.Id, "outcome");

        // Cross-reference validation — fail loudly at load instead of NRE'ing at gen time.
        foreach (var type in reg._types.Values)
        {
            foreach (var dangerId in type.DangerTypeIds)
                if (!reg._dangers.ContainsKey(dangerId))
                    throw new InvalidOperationException(
                        $"salvage type '{type.Id}' references unknown danger '{dangerId}'");

            if (type.SpecialOutcomeId != null && !reg._outcomes.ContainsKey(type.SpecialOutcomeId))
                throw new InvalidOperationException(
                    $"salvage type '{type.Id}' references unknown outcome '{type.SpecialOutcomeId}'");

            foreach (var poiKey in type.EligiblePOIWeights.Keys)
                if (!Enum.TryParse<POIType>(poiKey, out _))
                    throw new InvalidOperationException(
                        $"salvage type '{type.Id}' has unknown POIType '{poiKey}'");

            if (type.LayerCountMin < 1
                || type.LayerCountMax > SalvageSiteGenerator.MaxLayers
                || type.LayerCountMax < type.LayerCountMin)
                throw new InvalidOperationException(
                    $"salvage type '{type.Id}' has invalid layer-count range " +
                    $"[{type.LayerCountMin}, {type.LayerCountMax}] (max allowed: {SalvageSiteGenerator.MaxLayers})");
        }
        return reg;
    }

    private static T Deserialize<T>(string json, string label) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException($"{label}.json is empty or missing");
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOpts)
                ?? throw new InvalidOperationException($"{label}.json deserialized to null");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"{label}.json parse error: {ex.Message}", ex);
        }
    }

    private static void ValidateAndAdd<TDef>(
        Dictionary<string, TDef> dict, TDef def, string id, string label)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException($"{label} entry missing 'id'");
        if (dict.ContainsKey(id))
            throw new InvalidOperationException($"duplicate {label} id '{id}'");
        dict[id] = def;
    }
}
