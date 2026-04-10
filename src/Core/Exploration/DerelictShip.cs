using System;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Exploration;

/// <summary>
/// A precursor ship found at a POI. Can be salvaged, used as-is, jury-rigged, repaired, or replicated.
/// </summary>
public class DerelictShip
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public PrecursorColor Color { get; set; }
    public int TechTier { get; set; }
    public ShipSizeClass SizeClass { get; set; }
    public float Condition { get; set; } // 0-100%
    public int POIId { get; set; }
}

/// <summary>Processes derelict ship actions.</summary>
public static class DerelictProcessor
{
    public struct ActionResult
    {
        public int BasicComponents;
        public int AdvancedComponents;
        public float ResearchPoints;
        public int ProductionCost; // Cost to perform the action
        public float EfficiencyPenalty; // For use-as-is/jury-rig
        public string Description;
    }

    public static ActionResult CalculateAction(DerelictShip derelict, DerelictAction action)
    {
        int tierMult = derelict.TechTier;
        float condMult = derelict.Condition / 100f;

        return action switch
        {
            DerelictAction.SalvageForParts => new ActionResult
            {
                BasicComponents = (int)(10 * tierMult * condMult),
                AdvancedComponents = (int)(3 * tierMult * condMult),
                ResearchPoints = 5 * tierMult,
                ProductionCost = 0,
                EfficiencyPenalty = 0f,
                Description = "Break down for components and research data"
            },
            DerelictAction.UseAsIs => new ActionResult
            {
                BasicComponents = 0,
                AdvancedComponents = 0,
                ResearchPoints = 0,
                ProductionCost = 10,
                EfficiencyPenalty = 0.40f, // 40% less effective
                Description = "Operate at reduced efficiency — devastating in early game"
            },
            DerelictAction.JuryRig => new ActionResult
            {
                BasicComponents = 0,
                AdvancedComponents = 0,
                ResearchPoints = 0,
                ProductionCost = 30 * tierMult,
                EfficiencyPenalty = 0.25f,
                Description = "Repurpose for a different role — functional but suboptimal"
            },
            DerelictAction.Repair => new ActionResult
            {
                BasicComponents = 0,
                AdvancedComponents = -(5 * tierMult), // Costs advanced components
                ResearchPoints = 0,
                ProductionCost = 80 * tierMult,
                EfficiencyPenalty = 0f, // Full restoration
                Description = "Full restoration — extremely expensive"
            },
            DerelictAction.Replicate => new ActionResult
            {
                BasicComponents = 0,
                AdvancedComponents = -(10 * tierMult),
                ResearchPoints = 0,
                ProductionCost = 200 * tierMult,
                EfficiencyPenalty = 0f,
                Description = "Build a copy — requires massive research investment"
            },
            _ => default
        };
    }
}
