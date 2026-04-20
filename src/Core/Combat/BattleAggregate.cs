using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Combat;

/// <summary>
/// Role-grouped view of one side of a battle for UI consumption at 4Hz.
/// Pure value object — copy-on-read, safe to pass to the HUD without locking.
/// </summary>
public class BattleAggregate
{
    public List<RoleSlice> Roles { get; set; } = new();
    public Dictionary<string, float> SupplyDrainPerColor { get; set; } = new();
    public int TotalShips => Roles.Sum(r => r.ShipCount);
    public int AliveShips => Roles.Sum(r => r.AliveCount);
    public float AverageHpPercent { get; set; }
    public float AverageMoralePercent { get; set; }

    public static BattleAggregate Build(IEnumerable<CombatUnit> units)
    {
        var agg = new BattleAggregate();
        var byRole = units.GroupBy(u => u.Role).OrderBy(g => (int)g.Key);
        float hpSum = 0f, moraleSum = 0f;
        int total = 0;

        foreach (var group in byRole)
        {
            var slice = new RoleSlice { Role = group.Key };
            foreach (var unit in group)
            {
                slice.ShipCount++;
                if (!unit.IsDestroyed && !(unit.IsRetreating && unit.RetreatProgress >= 1f))
                    slice.AliveCount++;

                float maxTotal = unit.StructureMax + unit.ArmorMax + unit.ShieldMax;
                float curTotal = unit.StructureHp + unit.ArmorHp + unit.ShieldHp;
                float hpPct = maxTotal > 0 ? curTotal / maxTotal : 0f;
                slice.HpPips.Add(hpPct);
                slice.MoraleSum += unit.Morale;
                hpSum += hpPct;
                moraleSum += unit.Morale;
                total++;
            }
            agg.Roles.Add(slice);
        }

        agg.AverageHpPercent = total > 0 ? hpSum / total * 100f : 0f;
        agg.AverageMoralePercent = total > 0 ? moraleSum / total : 0f;
        return agg;
    }
}

public class RoleSlice
{
    public FleetRole Role { get; set; }
    public int ShipCount { get; set; }
    public int AliveCount { get; set; }
    /// <summary>Per-ship HP fraction 0..1. One entry per ship in the role (for segmented bar).</summary>
    public List<float> HpPips { get; set; } = new();
    public float MoraleSum { get; set; }
    public float AverageHpPercent =>
        HpPips.Count == 0 ? 0f : HpPips.Average() * 100f;
    public float AverageMoralePercent =>
        ShipCount == 0 ? 0f : MoraleSum / ShipCount;
}
