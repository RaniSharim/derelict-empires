using System.Collections.Generic;

namespace DerlictEmpires.Core.Settlements;

/// <summary>
/// Static definition of a building type that can be constructed at a colony.
/// </summary>
public class BuildingData
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public int ProductionCost { get; set; }

    // Effects
    public float ProductionBonus { get; set; }
    public float ResearchBonus { get; set; }
    public float MiningBonus { get; set; }
    public float FoodBonus { get; set; }
    public int BonusPopCap { get; set; }
    public int ExpertSlots { get; set; }
    public float HappinessBonus { get; set; }
    public float DefenseStrength { get; set; }
    public float LogisticsCapacity { get; set; }

    /// <summary>All building definitions.</summary>
    public static readonly BuildingData[] All = new[]
    {
        new BuildingData
        {
            Id = "mining_facility", DisplayName = "Mining Facility",
            Description = "Increases resource extraction rate at this colony.",
            ProductionCost = 60, MiningBonus = 0.25f
        },
        new BuildingData
        {
            Id = "research_lab", DisplayName = "Research Laboratory",
            Description = "Increases research output from this colony.",
            ProductionCost = 80, ResearchBonus = 0.30f
        },
        new BuildingData
        {
            Id = "food_farm", DisplayName = "Hydroponic Farm",
            Description = "Produces food for colony growth and logistics.",
            ProductionCost = 40, FoodBonus = 0.30f
        },
        new BuildingData
        {
            Id = "defense_emplacement", DisplayName = "Defense Emplacement",
            Description = "Planetary defense against orbital bombardment.",
            ProductionCost = 70, DefenseStrength = 20f
        },
        new BuildingData
        {
            Id = "logistics_hub", DisplayName = "Logistics Hub",
            Description = "Extends supply network range and capacity.",
            ProductionCost = 90, LogisticsCapacity = 50f
        },
        new BuildingData
        {
            Id = "hab_module", DisplayName = "Habitat Module",
            Description = "Increases maximum population capacity.",
            ProductionCost = 100, BonusPopCap = 3
        },
        new BuildingData
        {
            Id = "industrial_complex", DisplayName = "Industrial Complex",
            Description = "Increases production output.",
            ProductionCost = 120, ProductionBonus = 0.25f, ExpertSlots = 1
        },
        new BuildingData
        {
            Id = "entertainment_center", DisplayName = "Entertainment Center",
            Description = "Improves colony happiness.",
            ProductionCost = 50, HappinessBonus = 10f
        },
    };

    public static BuildingData? FindById(string id)
    {
        foreach (var b in All)
            if (b.Id == id) return b;
        return null;
    }
}
