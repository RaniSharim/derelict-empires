using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Ships;

namespace DerlictEmpires.Core.Combat;

/// <summary>
/// One active battle at a star system. Tracks attacker/defender units,
/// per-role disposition + target priority, elapsed simulated time, and per-design
/// performance accumulators (damage dealt/taken by origin design id).
/// The BattleManager advances this via <see cref="Step"/> on fast ticks.
/// </summary>
public class Battle
{
    public int Id { get; set; }
    public int SystemId { get; set; }

    public int AttackerEmpireId { get; set; }
    public int DefenderEmpireId { get; set; }
    public int AttackerFleetId { get; set; }
    public int DefenderFleetId { get; set; }

    public List<CombatUnit> Attackers { get; set; } = new();
    public List<CombatUnit> Defenders { get; set; } = new();

    /// <summary>Simulated time elapsed in seconds.</summary>
    public float ElapsedSeconds { get; set; }

    /// <summary>How many fast ticks of sim have been run. Rounds scale with tick count.</summary>
    public int RoundsProcessed { get; set; }

    /// <summary>Per-role dispositions applied to the attacker (local player) side.</summary>
    public Dictionary<FleetRole, Disposition> AttackerDispositions { get; set; } = new();
    public Dictionary<FleetRole, Disposition> DefenderDispositions { get; set; } = new();

    /// <summary>Per-role target priority. Null/"" = default (lowest-HP).</summary>
    public Dictionary<FleetRole, string> AttackerTargetPriority { get; set; } = new();

    /// <summary>
    /// Per-ship design performance. Populated as the battle runs; consumed by the debrief.
    /// Key = design id (or ship name when unknown).
    /// </summary>
    public Dictionary<string, DesignPerformance> PerDesignPerformance { get; set; } = new();

    /// <summary>Recent event strings for the LiveEventToasts stack (Phase G).</summary>
    public List<string> RecentEvents { get; set; } = new();

    public BattleState State { get; set; } = BattleState.Running;

    public CombatResult? FinalResult { get; set; }

    /// <summary>Has the player explicitly retreated?</summary>
    public bool PlayerRequestedRetreat { get; set; }

    /// <summary>True when either side has no valid combatants left.</summary>
    public bool IsOver => State != BattleState.Running;

    public int AttackersAlive =>
        Attackers.Count(u => !u.IsDestroyed && !(u.IsRetreating && u.RetreatProgress >= 1f));

    public int DefendersAlive =>
        Defenders.Count(u => !u.IsDestroyed && !(u.IsRetreating && u.RetreatProgress >= 1f));
}

public enum BattleState
{
    Running,
    Ended,
}

/// <summary>Per-design performance accumulator for post-battle debrief.</summary>
public class DesignPerformance
{
    public string DesignId { get; set; } = "";
    public string DesignName { get; set; } = "";
    public int ShipsEngaged { get; set; }
    public int ShipsSurvived { get; set; }
    public float DamageDealt { get; set; }
    public float DamageTaken { get; set; }

    /// <summary>Subsystem id → damage attributed to that module.</summary>
    public Dictionary<string, float> TopContributors { get; set; } = new();

    /// <summary>Subsystem id → prevented/absorbed damage; low numbers flag underperformers.</summary>
    public Dictionary<string, float> DefensiveContributions { get; set; } = new();
}
