using System.Collections.Generic;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Centralized icon path registry for faction emblems and resource types.
/// All SVGs are white-on-transparent from game-icons.net (CC BY 3.0, Lorc &amp; Delapouite).
/// Tint at runtime with faction color via modulate or shader.
/// </summary>
public static class IconMapping
{
    // ── Faction Emblems ──────────────────────────────────────────────
    public static readonly Dictionary<PrecursorColor, string> FactionEmblem = new()
    {
        [PrecursorColor.Red]    = "res://assets/icons/factions/crimson_forge.svg",    // gear-hammer (lorc)
        [PrecursorColor.Blue]   = "res://assets/icons/factions/azure_lattice.svg",    // radial-balance (lorc)
        [PrecursorColor.Green]  = "res://assets/icons/factions/verdant_synthesis.svg", // techno-heart (lorc)
        [PrecursorColor.Gold]   = "res://assets/icons/factions/golden_ascendancy.svg", // star-gate (delapouite)
        [PrecursorColor.Purple] = "res://assets/icons/factions/obsidian_covenant.svg", // black-hole-bolas (lorc)
    };

    // ── Resource Icons ───────────────────────────────────────────────
    // 6 universal shapes, tinted per-faction to produce 30 distinct cells.
    public static readonly Dictionary<ResourceType, string> Resource = new()
    {
        [ResourceType.SimpleEnergy]       = "res://assets/icons/resources/energy_basic.svg",      // double-ringed-orb (lorc)
        [ResourceType.AdvancedEnergy]     = "res://assets/icons/resources/energy_advanced.svg",    // orbital-rays (lorc)
        [ResourceType.SimpleParts]        = "res://assets/icons/resources/parts_basic.svg",        // cannister (lorc)
        [ResourceType.AdvancedParts]      = "res://assets/icons/resources/parts_advanced.svg",     // processor (lorc)
        [ResourceType.SimpleMaterials]    = "res://assets/icons/resources/materials_basic.svg",    // metal-bar (lorc)
        [ResourceType.AdvancedMaterials]  = "res://assets/icons/resources/materials_advanced.svg", // metal-scales (lorc)
    };

    // ── Faction Display Names ────────────────────────────────────────
    public static readonly Dictionary<PrecursorColor, string> FactionName = new()
    {
        [PrecursorColor.Red]    = "Crimson Forge",
        [PrecursorColor.Blue]   = "Azure Lattice",
        [PrecursorColor.Green]  = "Verdant Synthesis",
        [PrecursorColor.Gold]   = "Golden Ascendancy",
        [PrecursorColor.Purple] = "Obsidian Covenant",
    };
}
