using System.Linq;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Models;

/// <summary>
/// Static definition of a resource type. 30 total (5 colors × 6 types).
/// Three layers per color: Ore (mined), Energy (refined/generated), Components (manufactured/salvaged).
/// </summary>
public class ResourceDefinition
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public PrecursorColor Color { get; set; }
    public ResourceType Type { get; set; }
    public ResourceRarity Rarity { get; set; }

    /// <summary>All 30 resource definitions (DESIGN_V2 §1.2).</summary>
    public static readonly ResourceDefinition[] All = GenerateAll();

    private static ResourceDefinition[] GenerateAll()
    {
        return new[]
        {
            // Red (Crimson Forge)
            new ResourceDefinition { Id = "red_simple_ore", DisplayName = "Basalt Weave", Description = "Reinforced heat-resistant mineral weaving", Color = PrecursorColor.Red, Type = ResourceType.SimpleOre, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "red_advanced_ore", DisplayName = "Neutronium Slag", Description = "Dense protective armor plating ore", Color = PrecursorColor.Red, Type = ResourceType.AdvancedOre, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "red_simple_energy", DisplayName = "Plasma Embers", Description = "Residual thermal energy harvested from forge remnants", Color = PrecursorColor.Red, Type = ResourceType.SimpleEnergy, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "red_advanced_energy", DisplayName = "Fusion Cores", Description = "Intact precursor reactor fuel cells", Color = PrecursorColor.Red, Type = ResourceType.AdvancedEnergy, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "red_basic_component", DisplayName = "Scrap Iron", Description = "Salvageable metals and structural alloys", Color = PrecursorColor.Red, Type = ResourceType.BasicComponent, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "red_advanced_component", DisplayName = "Forge Matrices", Description = "Precision-machined industrial templates", Color = PrecursorColor.Red, Type = ResourceType.AdvancedComponent, Rarity = ResourceRarity.Rare },

            // Blue (Azure Lattice)
            new ResourceDefinition { Id = "blue_simple_ore", DisplayName = "Silica Threads", Description = "Glassy synthetic conductor lines", Color = PrecursorColor.Blue, Type = ResourceType.SimpleOre, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "blue_advanced_ore", DisplayName = "Superconductor Mesh", Description = "Frigid energy transfer network", Color = PrecursorColor.Blue, Type = ResourceType.AdvancedOre, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "blue_simple_energy", DisplayName = "Signal Residue", Description = "Ambient electromagnetic traces from data networks", Color = PrecursorColor.Blue, Type = ResourceType.SimpleEnergy, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "blue_advanced_energy", DisplayName = "Quantum Resonance", Description = "Stabilized quantum field generators", Color = PrecursorColor.Blue, Type = ResourceType.AdvancedEnergy, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "blue_basic_component", DisplayName = "Data Chips", Description = "Fragmentary storage media and circuit boards", Color = PrecursorColor.Blue, Type = ResourceType.BasicComponent, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "blue_advanced_component", DisplayName = "Lattice Crystals", Description = "Perfectly structured computing substrates", Color = PrecursorColor.Blue, Type = ResourceType.AdvancedComponent, Rarity = ResourceRarity.Rare },

            // Green (Verdant Synthesis)
            new ResourceDefinition { Id = "green_simple_ore", DisplayName = "Resin Extract", Description = "Hardened biological sap", Color = PrecursorColor.Green, Type = ResourceType.SimpleOre, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "green_advanced_ore", DisplayName = "Living Carapace", Description = "Self-healing hull armor", Color = PrecursorColor.Green, Type = ResourceType.AdvancedOre, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "green_simple_energy", DisplayName = "Bio-Luminance", Description = "Bioluminescent organisms that convert light to power", Color = PrecursorColor.Green, Type = ResourceType.SimpleEnergy, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "green_advanced_energy", DisplayName = "Genesis Catalysts", Description = "Concentrated life-force accelerants", Color = PrecursorColor.Green, Type = ResourceType.AdvancedEnergy, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "green_basic_component", DisplayName = "Organic Polymers", Description = "Biological structural compounds and fibres", Color = PrecursorColor.Green, Type = ResourceType.BasicComponent, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "green_advanced_component", DisplayName = "Genetic Templates", Description = "Complete precursor genome sequences and growth patterns", Color = PrecursorColor.Green, Type = ResourceType.AdvancedComponent, Rarity = ResourceRarity.Rare },

            // Gold (Golden Ascendancy)
            new ResourceDefinition { Id = "gold_simple_ore", DisplayName = "Gold Leaf", Description = "Highly ductile solar reflectors", Color = PrecursorColor.Gold, Type = ResourceType.SimpleOre, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "gold_advanced_ore", DisplayName = "Aegis Plating", Description = "Impenetrable ornamental shielding", Color = PrecursorColor.Gold, Type = ResourceType.AdvancedOre, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "gold_simple_energy", DisplayName = "Solar Dust", Description = "Stellar particle collections from trade route beacons", Color = PrecursorColor.Gold, Type = ResourceType.SimpleEnergy, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "gold_advanced_energy", DisplayName = "Hyperlane Essence", Description = "Concentrated spatial-fold energy from jump gate remnants", Color = PrecursorColor.Gold, Type = ResourceType.AdvancedEnergy, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "gold_basic_component", DisplayName = "Navigation Fragments", Description = "Partial star charts and basic guidance hardware", Color = PrecursorColor.Gold, Type = ResourceType.BasicComponent, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "gold_advanced_component", DisplayName = "Transit Matrices", Description = "Complete hyperdrive calibration assemblies", Color = PrecursorColor.Gold, Type = ResourceType.AdvancedComponent, Rarity = ResourceRarity.Rare },

            // Purple (Obsidian Covenant)
            new ResourceDefinition { Id = "purple_simple_ore", DisplayName = "Obsidian Glass", Description = "Shattered but sharp crystalline edges", Color = PrecursorColor.Purple, Type = ResourceType.SimpleOre, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "purple_advanced_ore", DisplayName = "Void Crystal", Description = "Indestructible dark dimension matter", Color = PrecursorColor.Purple, Type = ResourceType.AdvancedOre, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "purple_simple_energy", DisplayName = "Void Whispers", Description = "Faint exotic energy traces from covenant sites", Color = PrecursorColor.Purple, Type = ResourceType.SimpleEnergy, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "purple_advanced_energy", DisplayName = "Dark Matter Cores", Description = "Stabilized dark matter power sources", Color = PrecursorColor.Purple, Type = ResourceType.AdvancedEnergy, Rarity = ResourceRarity.Rare },
            new ResourceDefinition { Id = "purple_basic_component", DisplayName = "Exotic Fragments", Description = "Unusual material samples with anomalous properties", Color = PrecursorColor.Purple, Type = ResourceType.BasicComponent, Rarity = ResourceRarity.Common },
            new ResourceDefinition { Id = "purple_advanced_component", DisplayName = "Consciousness Shards", Description = "Psionic crystalline structures with encoded awareness", Color = PrecursorColor.Purple, Type = ResourceType.AdvancedComponent, Rarity = ResourceRarity.Rare },
        };
    }

    public static ResourceDefinition? Find(PrecursorColor color, ResourceType type) =>
        All.FirstOrDefault(r => r.Color == color && r.Type == type);

    public static ResourceDefinition? FindById(string id) =>
        All.FirstOrDefault(r => r.Id == id);
}
