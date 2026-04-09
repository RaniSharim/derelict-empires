using System.Collections.Generic;
using DerlictEmpires.Core.Enums;

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

    /// <summary>Resource stockpile keyed by (Color, ResourceType) encoded as string.</summary>
    public Dictionary<string, float> ResourceStockpile { get; set; } = new();

    /// <summary>Component stockpile keyed by (Color, Tier) encoded as string.</summary>
    public Dictionary<string, float> ComponentStockpile { get; set; } = new();

    /// <summary>Currency balance.</summary>
    public long Credits { get; set; }

    /// <summary>
    /// Generates a stockpile key for a resource.
    /// </summary>
    public static string ResourceKey(PrecursorColor color, ResourceType type) =>
        $"{color}_{type}";

    /// <summary>
    /// Generates a stockpile key for a component.
    /// </summary>
    public static string ComponentKey(PrecursorColor color, ComponentTier tier) =>
        $"{color}_{tier}";

    public float GetResource(PrecursorColor color, ResourceType type) =>
        ResourceStockpile.GetValueOrDefault(ResourceKey(color, type), 0f);

    public void AddResource(PrecursorColor color, ResourceType type, float amount)
    {
        var key = ResourceKey(color, type);
        ResourceStockpile[key] = ResourceStockpile.GetValueOrDefault(key, 0f) + amount;
    }

    public float GetComponent(PrecursorColor color, ComponentTier tier) =>
        ComponentStockpile.GetValueOrDefault(ComponentKey(color, tier), 0f);

    public void AddComponent(PrecursorColor color, ComponentTier tier, float amount)
    {
        var key = ComponentKey(color, tier);
        ComponentStockpile[key] = ComponentStockpile.GetValueOrDefault(key, 0f) + amount;
    }
}
