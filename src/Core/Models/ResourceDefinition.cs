using System.Linq;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Models;

/// <summary>
/// Static definition of a raw resource type. 30 total (5 colors × 6 types).
/// This is a plain C# class; the [GlobalClass] Godot Resource version
/// will wrap this for editor/inspector integration.
/// </summary>
public class ResourceDefinition
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public PrecursorColor Color { get; set; }
    public ResourceType Type { get; set; }
    public ResourceRarity Rarity { get; set; }

    /// <summary>All 30 resource definitions from DESIGN.md §3.2.</summary>
    public static readonly ResourceDefinition[] All = GenerateAll();

    private static ResourceDefinition[] GenerateAll()
    {
        return new[]
        {
            // Red (Crimson Forge)
            new ResourceDefinition { Id = "red_simple_energy", DisplayName = "Plasma Embers", Description = "Residual thermal energy harvested from forge remnants", Color = PrecursorColor.Red, Type = ResourceType.SimpleEnergy, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "red_advanced_energy", DisplayName = "Fusion Cores", Description = "Intact precursor reactor fuel cells", Color = PrecursorColor.Red, Type = ResourceType.AdvancedEnergy, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "red_simple_materials", DisplayName = "Basalt Weave", Description = "Reinforced heat-resistant weaving", Color = PrecursorColor.Red, Type = ResourceType.SimpleMaterials, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "red_advanced_materials", DisplayName = "Neutronium Slag", Description = "Dense protective armor plating", Color = PrecursorColor.Red, Type = ResourceType.AdvancedMaterials, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "red_simple_parts", DisplayName = "Scrap Iron", Description = "Salvageable metals and structural alloys", Color = PrecursorColor.Red, Type = ResourceType.SimpleParts, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "red_advanced_parts", DisplayName = "Forge Matrices", Description = "Precision-machined industrial templates", Color = PrecursorColor.Red, Type = ResourceType.AdvancedParts, Rarity = ResourceRarity.Rare },

            // Blue (Azure Lattice)
            new ResourceDefinition { Id = "blue_simple_energy", DisplayName = "Signal Residue", Description = "Ambient electromagnetic traces from data networks", Color = PrecursorColor.Blue, Type = ResourceType.SimpleEnergy, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "blue_advanced_energy", DisplayName = "Quantum Resonance", Description = "Stabilized quantum field generators", Color = PrecursorColor.Blue, Type = ResourceType.AdvancedEnergy, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "blue_simple_materials", DisplayName = "Silica Threads", Description = "Glassy synthetic conductor lines", Color = PrecursorColor.Blue, Type = ResourceType.SimpleMaterials, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "blue_advanced_materials", DisplayName = "Superconductor Mesh", Description = "Frigid energy transfer network", Color = PrecursorColor.Blue, Type = ResourceType.AdvancedMaterials, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "blue_simple_parts", DisplayName = "Data Chips", Description = "Fragmentary storage media and circuit boards", Color = PrecursorColor.Blue, Type = ResourceType.SimpleParts, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "blue_advanced_parts", DisplayName = "Lattice Crystals", Description = "Perfectly structured computing substrates", Color = PrecursorColor.Blue, Type = ResourceType.AdvancedParts, Rarity = ResourceRarity.Rare },

            // Green (Verdant Synthesis)
            new ResourceDefinition { Id = "green_simple_energy", DisplayName = "Bio-Luminance", Description = "Bioluminescent organisms that convert light to power", Color = PrecursorColor.Green, Type = ResourceType.SimpleEnergy, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "green_advanced_energy", DisplayName = "Genesis Catalysts", Description = "Concentrated life-force accelerants", Color = PrecursorColor.Green, Type = ResourceType.AdvancedEnergy, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "green_simple_materials", DisplayName = "Resin Extract", Description = "Hardened biological sap", Color = PrecursorColor.Green, Type = ResourceType.SimpleMaterials, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "green_advanced_materials", DisplayName = "Living Carapace", Description = "Self-healing hull armor", Color = PrecursorColor.Green, Type = ResourceType.AdvancedMaterials, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "green_simple_parts", DisplayName = "Organic Polymers", Description = "Biological structural compounds and fibres", Color = PrecursorColor.Green, Type = ResourceType.SimpleParts, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "green_advanced_parts", DisplayName = "Genetic Templates", Description = "Complete precursor genome sequences and growth patterns", Color = PrecursorColor.Green, Type = ResourceType.AdvancedParts, Rarity = ResourceRarity.Rare },

            // Gold (Golden Ascendancy)
            new ResourceDefinition { Id = "gold_simple_energy", DisplayName = "Solar Dust", Description = "Stellar particle collections from trade route beacons", Color = PrecursorColor.Gold, Type = ResourceType.SimpleEnergy, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "gold_advanced_energy", DisplayName = "Hyperlane Essence", Description = "Concentrated spatial-fold energy from jump gate remnants", Color = PrecursorColor.Gold, Type = ResourceType.AdvancedEnergy, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "gold_simple_materials", DisplayName = "Gold Leaf", Description = "Highly ductile solar reflectors", Color = PrecursorColor.Gold, Type = ResourceType.SimpleMaterials, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "gold_advanced_materials", DisplayName = "Aegis Plating", Description = "Impenetrable ornamental shielding", Color = PrecursorColor.Gold, Type = ResourceType.AdvancedMaterials, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "gold_simple_parts", DisplayName = "Navigation Fragments", Description = "Partial star charts and basic guidance hardware", Color = PrecursorColor.Gold, Type = ResourceType.SimpleParts, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "gold_advanced_parts", DisplayName = "Transit Matrices", Description = "Complete hyperdrive calibration assemblies", Color = PrecursorColor.Gold, Type = ResourceType.AdvancedParts, Rarity = ResourceRarity.Rare },

            // Purple (Obsidian Covenant)
            new ResourceDefinition { Id = "purple_simple_energy", DisplayName = "Void Whispers", Description = "Faint exotic energy traces from covenant sites", Color = PrecursorColor.Purple, Type = ResourceType.SimpleEnergy, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "purple_advanced_energy", DisplayName = "Dark Matter Cores", Description = "Stabilized dark matter power sources", Color = PrecursorColor.Purple, Type = ResourceType.AdvancedEnergy, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "purple_simple_materials", DisplayName = "Obsidian Glass", Description = "Shattered but sharp crystalline edges", Color = PrecursorColor.Purple, Type = ResourceType.SimpleMaterials, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "purple_advanced_materials", DisplayName = "Void Crystal", Description = "Indestructible dark dimension matter", Color = PrecursorColor.Purple, Type = ResourceType.AdvancedMaterials, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "purple_simple_parts", DisplayName = "Exotic Fragments", Description = "Unusual material samples with anomalous properties", Color = PrecursorColor.Purple, Type = ResourceType.SimpleParts, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "purple_advanced_parts", DisplayName = "Consciousness Shards", Description = "Psionic crystalline structures with encoded awareness", Color = PrecursorColor.Purple, Type = ResourceType.AdvancedParts, Rarity = ResourceRarity.Rare },
        };
    }

    public static ResourceDefinition? Find(PrecursorColor color, ResourceType type) =>
        All.FirstOrDefault(r => r.Color == color && r.Type == type);

    public static ResourceDefinition? FindById(string id) =>
        All.FirstOrDefault(r => r.Id == id);
}
