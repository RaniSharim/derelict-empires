using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Production;

namespace DerlictEmpires.Core.Settlements;

/// <summary>
/// Full settlement on a habitable planet. Supports population growth,
/// buildings, production queue, happiness, and expert slots.
/// </summary>
public class Colony : Settlement
{
    public PlanetSize PlanetSize { get; set; }
    public ColonyPriority Priority { get; set; } = ColonyPriority.Balanced;
    public float Happiness { get; set; } = 70f;

    /// <summary>Installed building IDs.</summary>
    public List<string> Buildings { get; set; } = new();

    public ProductionQueue Queue { get; } = new();

    /// <summary>Accumulated food surplus for growth. Growth triggers at threshold.</summary>
    public float FoodSurplus { get; set; }

    public override int PopCap => GetBasePopCap() + GetBuildingBonusPopCap();

    public int GetBasePopCap() => PlanetSize switch
    {
        PlanetSize.Small => 6,
        PlanetSize.Medium => 8,
        PlanetSize.Large => 13,
        PlanetSize.Prime => 20,
        PlanetSize.Exceptional => 30,
        _ => 5
    };

    public int GetBuildingBonusPopCap() =>
        Buildings.Sum(id => BuildingData.FindById(id)?.BonusPopCap ?? 0);

    /// <summary>Base production output from workers (before building bonuses).</summary>
    public int BaseProductionOutput => GetWorkersIn(WorkPool.Production) * 3;

    /// <summary>Base research output from workers.</summary>
    public int BaseResearchOutput => GetWorkersIn(WorkPool.Research) * 2;

    /// <summary>Base food output from workers.</summary>
    public int BaseFoodOutput => GetWorkersIn(WorkPool.Food) * 3;

    /// <summary>Base mining output from workers.</summary>
    public int BaseMiningOutput => GetWorkersIn(WorkPool.Mining) * 2;

    /// <summary>Production output after building bonuses.</summary>
    public float EffectiveProductionOutput
    {
        get
        {
            float bonus = Buildings.Sum(id => BuildingData.FindById(id)?.ProductionBonus ?? 0f);
            float happinessMod = HappinessModifier;
            return BaseProductionOutput * (1f + bonus) * happinessMod;
        }
    }

    /// <summary>Research output after building bonuses.</summary>
    public float EffectiveResearchOutput
    {
        get
        {
            float bonus = Buildings.Sum(id => BuildingData.FindById(id)?.ResearchBonus ?? 0f);
            return BaseResearchOutput * (1f + bonus) * HappinessModifier;
        }
    }

    /// <summary>Food output after building bonuses. Consumed first by population, surplus drives growth.</summary>
    public float EffectiveFoodOutput
    {
        get
        {
            float bonus = Buildings.Sum(id => BuildingData.FindById(id)?.FoodBonus ?? 0f);
            return BaseFoodOutput * (1f + bonus);
        }
    }

    /// <summary>Mining output after building bonuses.</summary>
    public float EffectiveMiningOutput
    {
        get
        {
            float bonus = Buildings.Sum(id => BuildingData.FindById(id)?.MiningBonus ?? 0f);
            return BaseMiningOutput * (1f + bonus) * HappinessModifier;
        }
    }

    /// <summary>Happiness modifier on output: 1.0 at 70+, scales down below.</summary>
    public float HappinessModifier
    {
        get
        {
            if (Happiness >= 70f) return 1.0f;
            if (Happiness <= 0f) return 0.3f;
            return 0.3f + 0.7f * (Happiness / 70f);
        }
    }

    /// <summary>Total expert slots from buildings.</summary>
    public int ExpertSlots => Buildings.Sum(id => BuildingData.FindById(id)?.ExpertSlots ?? 0);

    /// <summary>Total defense strength from buildings.</summary>
    public float DefenseStrength => Buildings.Sum(id => BuildingData.FindById(id)?.DefenseStrength ?? 0f);
}
