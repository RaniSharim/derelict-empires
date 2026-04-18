using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Ships;

namespace DerlictEmpires.Core.Models;

/// <summary>Runtime data for a player or AI empire.</summary>
public class EmpireData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsHuman { get; set; }

    /// <summary>The precursor color this empire has affinity with. Null for FreeRace.</summary>
    public PrecursorColor? Affinity { get; set; }
    public Origin Origin { get; set; }

    /// <summary>The home system assigned at game start.</summary>
    public int HomeSystemId { get; set; } = -1;

    /// <summary>
    /// Unified resource stockpile keyed by (Color, ResourceType) encoded as string.
    /// Covers all 30 resources: 5 colors × 6 types (Ore, Energy, Components).
    /// </summary>
    public Dictionary<string, float> ResourceStockpile { get; set; } = new();

    /// <summary>Currency balance.</summary>
    public long Credits { get; set; }

    /// <summary>Food stockpile (universal, not color-tied).</summary>
    public float Food { get; set; }

    /// <summary>Saved ship designs and fleet templates authored by this empire.</summary>
    public EmpireDesignState DesignState { get; set; } = new();

    public static string ResourceKey(PrecursorColor color, ResourceType type) =>
        $"{color}_{type}";

    public float GetResource(PrecursorColor color, ResourceType type) =>
        ResourceStockpile.GetValueOrDefault(ResourceKey(color, type), 0f);

    public void AddResource(PrecursorColor color, ResourceType type, float amount)
    {
        var key = ResourceKey(color, type);
        ResourceStockpile[key] = ResourceStockpile.GetValueOrDefault(key, 0f) + amount;
    }

    public bool SpendResource(PrecursorColor color, ResourceType type, float amount)
    {
        var key = ResourceKey(color, type);
        float current = ResourceStockpile.GetValueOrDefault(key, 0f);
        if (current < amount) return false;
        ResourceStockpile[key] = current - amount;
        return true;
    }
}
