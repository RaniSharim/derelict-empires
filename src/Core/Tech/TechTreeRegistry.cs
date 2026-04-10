using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Tech;

/// <summary>
/// Static registry of all tech nodes, subsystems, and synergy techs.
/// Generates the full 150-node tree programmatically.
/// </summary>
public class TechTreeRegistry
{
    public List<TechNodeData> Nodes { get; } = new();
    public List<SubsystemData> Subsystems { get; } = new();
    public List<SynergyTechData> Synergies { get; } = new();

    private readonly Dictionary<string, TechNodeData> _nodeById = new();
    private readonly Dictionary<string, SubsystemData> _subsystemById = new();

    public TechTreeRegistry()
    {
        GenerateNodes();
        GenerateSynergies();
    }

    public TechNodeData? GetNode(string id) => _nodeById.GetValueOrDefault(id);
    public SubsystemData? GetSubsystem(string id) => _subsystemById.GetValueOrDefault(id);

    public TechNodeData? GetNode(PrecursorColor color, TechCategory category, int tier) =>
        Nodes.FirstOrDefault(n => n.Color == color && n.Category == category && n.Tier == tier);

    public IEnumerable<TechNodeData> GetNodesForColor(PrecursorColor color) =>
        Nodes.Where(n => n.Color == color);

    public IEnumerable<TechNodeData> GetNodesForCategory(PrecursorColor color, TechCategory category) =>
        Nodes.Where(n => n.Color == color && n.Category == category).OrderBy(n => n.Tier);

    private void GenerateNodes()
    {
        int subsystemId = 0;
        foreach (var color in Enum.GetValues<PrecursorColor>())
        foreach (var category in Enum.GetValues<TechCategory>())
        for (int tier = 1; tier <= 6; tier++)
        {
            var nodeId = $"{color}_{category}_T{tier}";
            var node = new TechNodeData
            {
                Id = nodeId,
                Color = color,
                Category = category,
                Tier = tier,
                ResearchCost = GetTierCost(tier)
            };

            // Generate 3 subsystems per node
            for (int sub = 0; sub < 3; sub++)
            {
                var subId = $"{nodeId}_S{sub}";
                var subsystem = new SubsystemData
                {
                    Id = subId,
                    DisplayName = GenerateSubsystemName(color, category, tier, sub),
                    Description = $"Tier {tier} {category} subsystem ({color})",
                    Color = color,
                    Category = category,
                    Tier = tier,
                    ResearchCost = GetSubsystemCost(tier)
                };
                Subsystems.Add(subsystem);
                _subsystemById[subId] = subsystem;
                node.SubsystemIds.Add(subId);
                subsystemId++;
            }

            Nodes.Add(node);
            _nodeById[nodeId] = node;
        }
    }

    private void GenerateSynergies()
    {
        // 10 synergy combos from DESIGN.md §5.4
        Synergies.AddRange(new[]
        {
            new SynergyTechData { Id = "synergy_red_blue", DisplayName = "Precision Targeting", Description = "Enhanced weapon accuracy through sensor integration", ColorA = PrecursorColor.Red, ColorB = PrecursorColor.Blue, RequiredTierA = 3, RequiredTierB = 3, ResearchCost = 200 },
            new SynergyTechData { Id = "synergy_red_green", DisplayName = "Regenerative Armor", Description = "Self-repairing hull plating using biological processes", ColorA = PrecursorColor.Red, ColorB = PrecursorColor.Green, RequiredTierA = 3, RequiredTierB = 3, ResearchCost = 200 },
            new SynergyTechData { Id = "synergy_green_gold", DisplayName = "Self-Replicating Logistics", Description = "Supply chains that grow organically", ColorA = PrecursorColor.Green, ColorB = PrecursorColor.Gold, RequiredTierA = 2, RequiredTierB = 2, ResearchCost = 150 },
            new SynergyTechData { Id = "synergy_blue_gold", DisplayName = "Perfect Efficiency", Description = "Optimized manufacturing and trade routing", ColorA = PrecursorColor.Blue, ColorB = PrecursorColor.Gold, RequiredTierA = 3, RequiredTierB = 3, ResearchCost = 200 },
            new SynergyTechData { Id = "synergy_gold_blue_scan", DisplayName = "Deep Scanning", Description = "Extended detection and survey range", ColorA = PrecursorColor.Gold, ColorB = PrecursorColor.Blue, RequiredTierA = 2, RequiredTierB = 2, ResearchCost = 150 },
            new SynergyTechData { Id = "synergy_gold_purple", DisplayName = "Dimensional Surveying", Description = "Detection of hidden lanes and anomalies", ColorA = PrecursorColor.Gold, ColorB = PrecursorColor.Purple, RequiredTierA = 3, RequiredTierB = 3, ResearchCost = 200 },
            new SynergyTechData { Id = "synergy_green_red", DisplayName = "Extreme Adaptation", Description = "Colonization of hostile environments", ColorA = PrecursorColor.Green, ColorB = PrecursorColor.Red, RequiredTierA = 3, RequiredTierB = 3, ResearchCost = 200 },
            new SynergyTechData { Id = "synergy_green_purple", DisplayName = "Conscious Worlds", Description = "Terraforming with sentient biospheres", ColorA = PrecursorColor.Green, ColorB = PrecursorColor.Purple, RequiredTierA = 4, RequiredTierB = 4, ResearchCost = 300 },
            new SynergyTechData { Id = "synergy_blue_purple", DisplayName = "Psychic Intelligence", Description = "Espionage through consciousness manipulation", ColorA = PrecursorColor.Blue, ColorB = PrecursorColor.Purple, RequiredTierA = 4, RequiredTierB = 4, ResearchCost = 300 },
            new SynergyTechData { Id = "synergy_blue_green", DisplayName = "Biological Infiltration", Description = "Living spy systems and counter-intelligence", ColorA = PrecursorColor.Blue, ColorB = PrecursorColor.Green, RequiredTierA = 3, RequiredTierB = 3, ResearchCost = 200 },
        });
    }

    private static int GetTierCost(int tier) => tier switch
    {
        1 => 50, 2 => 100, 3 => 200, 4 => 400, 5 => 700, 6 => 1000, _ => 100
    };

    private static int GetSubsystemCost(int tier) => tier switch
    {
        1 => 20, 2 => 40, 3 => 80, 4 => 150, 5 => 250, 6 => 400, _ => 50
    };

    private static string GenerateSubsystemName(PrecursorColor color, TechCategory category, int tier, int sub)
    {
        string prefix = category switch
        {
            TechCategory.WeaponsEnergyPropulsion => sub switch { 0 => "Weapon", 1 => "Engine", _ => "Reactor" },
            TechCategory.ComputingSensors => sub switch { 0 => "Scanner", 1 => "Computer", _ => "ECM" },
            TechCategory.IndustryMining => sub switch { 0 => "Extractor", 1 => "Foundry", _ => "Refinery" },
            TechCategory.AdminLogistics => sub switch { 0 => "Supply", 1 => "Admin", _ => "Trade" },
            TechCategory.Special => sub switch { 0 => "Exotic", 1 => "Unique", _ => "Prototype" },
            _ => "System"
        };

        string colorName = color switch
        {
            PrecursorColor.Red => "Forge", PrecursorColor.Blue => "Lattice",
            PrecursorColor.Green => "Bio", PrecursorColor.Gold => "Nav",
            PrecursorColor.Purple => "Void", _ => ""
        };

        return $"{colorName} {prefix} Mk.{tier}{(char)('A' + sub)}";
    }
}
