using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;
using DerlictEmpires.Core.Ships;

namespace DerlictEmpires.Core.Combat;

/// <summary>
/// Tracks all active battles and drives them on fast ticks. Wraps
/// <see cref="CombatSimulator"/> for the running game: each fast tick advances
/// the currently-running battles by one sim round (simulated time ≈ 0.5s per round).
/// UI consumes role-grouped aggregates from <see cref="GetOwnAggregate"/> /
/// <see cref="GetEnemyAggregate"/> — no direct reads from the raw CombatUnit list.
/// Pure C# — no Godot dependency.
/// </summary>
public class BattleManager
{
    public event Action<int>? BattleStarted;            // battleId
    public event Action<int>? BattleTicked;             // battleId (fires 4×/sec during combat)
    public event Action<int, CombatResult>? BattleEnded; // battleId, result

    private readonly Dictionary<int, Battle> _battles = new();
    private int _nextBattleId = 1;
    private readonly CombatSimulator _simulator = new();
    private readonly GameRandom _rng;

    /// <summary>Seconds of simulated combat time per one game-side round step.</summary>
    public const float SecondsPerRound = 0.5f;

    /// <summary>Emit a BattleTicked event every N ticks; at 10Hz fast-tick that's 4Hz UI updates.</summary>
    private const int UiTickEveryN = 3;

    /// <summary>Run one combat round every N fast ticks. Keeps battles human-paced.</summary>
    private const int RoundEveryN = 8;
    private int _tickCounter;

    public BattleManager(GameRandom rng)
    {
        _rng = rng;
    }

    public IReadOnlyCollection<Battle> ActiveBattles => _battles.Values;

    public Battle? GetBattle(int id) => _battles.GetValueOrDefault(id);

    /// <summary>
    /// Begin a new battle between the two fleets at their shared system.
    /// Builds CombatUnits from ship instance data; seeds dispositions from the
    /// owning empires' fleet templates if any; returns the new battle id.
    /// </summary>
    public int StartBattle(
        FleetData attacker, EmpireData attackerEmpire,
        FleetData defender, EmpireData defenderEmpire,
        IReadOnlyDictionary<int, ShipInstanceData> shipsById,
        int systemId)
    {
        var battle = new Battle
        {
            Id = _nextBattleId++,
            SystemId = systemId,
            AttackerEmpireId = attackerEmpire.Id,
            DefenderEmpireId = defenderEmpire.Id,
            AttackerFleetId = attacker.Id,
            DefenderFleetId = defender.Id,
            State = BattleState.Running,
        };

        foreach (var shipId in attacker.ShipIds)
            if (shipsById.TryGetValue(shipId, out var ship))
                battle.Attackers.Add(ToCombatUnit(ship, attackerEmpire, attackerRole: GuessRole(ship)));

        foreach (var shipId in defender.ShipIds)
            if (shipsById.TryGetValue(shipId, out var ship))
                battle.Defenders.Add(ToCombatUnit(ship, defenderEmpire, attackerRole: GuessRole(ship)));

        // Seed design performance entries keyed by design id or ship name.
        SeedDesignPerformance(battle, attacker, shipsById);

        _battles[battle.Id] = battle;
        BattleStarted?.Invoke(battle.Id);
        return battle.Id;
    }

    /// <summary>
    /// Advance all running battles by one sim step. Called on each fast tick
    /// from the main game loop. Deterministic for a given RNG seed.
    /// </summary>
    public void ProcessTick(float tickDelta)
    {
        _tickCounter++;
        bool uiTickFire = _tickCounter % UiTickEveryN == 0;
        bool roundTick = _tickCounter % RoundEveryN == 0;

        var toEnd = new List<(int id, CombatResult result)>();

        foreach (var battle in _battles.Values)
        {
            if (battle.State != BattleState.Running) continue;

            battle.ElapsedSeconds += tickDelta;

            // Retreat-requested short-circuit.
            if (battle.PlayerRequestedRetreat)
            {
                foreach (var u in battle.Attackers)
                    if (!u.IsDestroyed) u.IsRetreating = true;
            }

            // Apply dispositions as a simple mapping onto CombatUnit.Position each step.
            ApplyDispositions(battle);

            // Only run an actual combat round every N ticks — gives the HUD time to render
            // pip states between exchanges instead of collapsing the battle in one frame.
            if (roundTick)
            {
                var log = _simulator.Simulate(battle.Attackers, battle.Defenders, _rng, maxRounds: 1);
                battle.RoundsProcessed++;
                RecordRoundToDesignPerformance(battle, log);

                foreach (var round in log.Rounds)
                    battle.RecentEvents.AddRange(round.Events);
                const int eventBufferMax = 40;
                if (battle.RecentEvents.Count > eventBufferMax)
                    battle.RecentEvents.RemoveRange(0, battle.RecentEvents.Count - eventBufferMax);
            }

            // End condition check (can fire between rounds once a side is wiped).
            if (battle.AttackersAlive == 0 || battle.DefendersAlive == 0 || battle.PlayerRequestedRetreat)
            {
                var result = DetermineResult(battle);
                battle.FinalResult = result;
                battle.State = BattleState.Ended;
                FinalizeDesignPerformance(battle);
                toEnd.Add((battle.Id, result));
            }
        }

        foreach (var (id, result) in toEnd)
            BattleEnded?.Invoke(id, result);

        if (uiTickFire)
            foreach (var battle in _battles.Values)
                if (battle.State == BattleState.Running)
                    BattleTicked?.Invoke(battle.Id);
    }

    public BattleAggregate GetAttackerAggregate(int battleId) =>
        _battles.TryGetValue(battleId, out var b) ? BattleAggregate.Build(b.Attackers) : new BattleAggregate();

    public BattleAggregate GetDefenderAggregate(int battleId) =>
        _battles.TryGetValue(battleId, out var b) ? BattleAggregate.Build(b.Defenders) : new BattleAggregate();

    /// <summary>Player requests retreat for the attacker (their) side.</summary>
    public void RequestPlayerRetreat(int battleId)
    {
        if (_battles.TryGetValue(battleId, out var b))
            b.PlayerRequestedRetreat = true;
    }

    public void SetDisposition(int battleId, FleetRole role, Disposition disposition)
    {
        if (_battles.TryGetValue(battleId, out var b))
            b.AttackerDispositions[role] = disposition;
    }

    public void SetAllDispositions(int battleId, Disposition disposition)
    {
        if (!_battles.TryGetValue(battleId, out var b)) return;
        foreach (var role in System.Enum.GetValues<FleetRole>())
            b.AttackerDispositions[role] = disposition;
    }

    /// <summary>Drop the record of an ended battle. UI should call after debrief dismissal.</summary>
    public bool ForgetBattle(int battleId) => _battles.Remove(battleId);

    // === Helpers ============================================================

    private static FleetRole GuessRole(ShipInstanceData ship) =>
        ship.Role switch
        {
            "Scout"    => FleetRole.Scout,
            "Fighter"  => FleetRole.Brawler,
            "Salvager" => FleetRole.NonCombatant,
            "Builder"  => FleetRole.NonCombatant,
            _          => ship.SizeClass switch
            {
                ShipSizeClass.Fighter   => FleetRole.Brawler,
                ShipSizeClass.Corvette  => FleetRole.Scout,
                ShipSizeClass.Frigate   => FleetRole.Guardian,
                ShipSizeClass.Destroyer => FleetRole.Brawler,
                ShipSizeClass.Cruiser   => FleetRole.Brawler,
                ShipSizeClass.Battleship=> FleetRole.Bombard,
                ShipSizeClass.Titan     => FleetRole.Bombard,
                _                       => FleetRole.Brawler,
            },
        };

    private static CombatUnit ToCombatUnit(ShipInstanceData ship, EmpireData owner, FleetRole attackerRole)
    {
        // Approximate defense / offense budgets from ship size until ship designs are authored per-run.
        float sizeScale = ship.SizeClass switch
        {
            ShipSizeClass.Fighter    => 0.3f,
            ShipSizeClass.Corvette   => 0.6f,
            ShipSizeClass.Frigate    => 1.0f,
            ShipSizeClass.Destroyer  => 1.6f,
            ShipSizeClass.Cruiser    => 2.4f,
            ShipSizeClass.Battleship => 3.6f,
            ShipSizeClass.Titan      => 5.0f,
            _                        => 1.0f,
        };

        float hp = ship.MaxHp;
        float cur = ship.CurrentHp > 0 ? ship.CurrentHp : hp;

        return new CombatUnit
        {
            Id = ship.Id,
            Name = string.IsNullOrEmpty(ship.Name) ? $"Ship-{ship.Id}" : ship.Name,
            OwnerId = ship.OwnerEmpireId,
            Role = attackerRole,

            WeaponDamage = 6f * sizeScale,
            WeaponType = WeaponType.Laser,
            Tracking = 1.0f,

            PointDefense = 4f * sizeScale,
            ShieldHp = 20f * sizeScale,
            ShieldMax = 20f * sizeScale,
            ShieldRegen = 0.5f * sizeScale,
            ArmorHp = 15f * sizeScale,
            ArmorMax = 15f * sizeScale,
            StructureHp = cur,
            StructureMax = hp,
            Morale = 100f,
        };
    }

    private static void SeedDesignPerformance(
        Battle battle, FleetData attackerFleet,
        IReadOnlyDictionary<int, ShipInstanceData> shipsById)
    {
        foreach (var shipId in attackerFleet.ShipIds)
        {
            if (!shipsById.TryGetValue(shipId, out var ship)) continue;
            var key = string.IsNullOrEmpty(ship.ShipDesignId) ? ship.Role : ship.ShipDesignId;
            if (!battle.PerDesignPerformance.TryGetValue(key, out var perf))
            {
                perf = new DesignPerformance
                {
                    DesignId = ship.ShipDesignId,
                    DesignName = string.IsNullOrEmpty(ship.Name) ? ship.Role : ship.Name,
                };
                battle.PerDesignPerformance[key] = perf;
            }
            perf.ShipsEngaged++;
        }
    }

    private void RecordRoundToDesignPerformance(Battle battle, CombatLog log)
    {
        // Rough attribution — split damage across attackers by their WeaponDamage share.
        float attackerBudget = battle.Attackers.Sum(u => u.WeaponDamage);
        if (attackerBudget <= 0f || log.Rounds.Count == 0) return;

        float roundDamageDealt = log.Rounds[^1].Events.Count(e => e.Contains("structure damage")) * 10f;

        foreach (var perf in battle.PerDesignPerformance.Values)
        {
            // Proxy: damage dealt ∝ ship count in design ∝ share of WeaponDamage.
            float share = perf.ShipsEngaged / (float)System.Math.Max(1, attackerBudget);
            perf.DamageDealt += roundDamageDealt * share * 0.2f;
        }
    }

    private static void FinalizeDesignPerformance(Battle battle)
    {
        foreach (var perf in battle.PerDesignPerformance.Values)
            perf.ShipsSurvived = battle.Attackers.Count(u => !u.IsDestroyed &&
                (string.IsNullOrEmpty(perf.DesignId) || true)); // stub: assume all survived units match
    }

    private static void ApplyDispositions(Battle battle)
    {
        foreach (var unit in battle.Attackers)
        {
            if (battle.AttackerDispositions.TryGetValue(unit.Role, out var d))
                unit.Position = d switch
                {
                    Disposition.Charge => CombatPosition.ChargeForward,
                    Disposition.Hold => CombatPosition.HoldPosition,
                    Disposition.StandBack => CombatPosition.StandBack,
                    Disposition.Retreat => CombatPosition.StandBack,
                    _ => CombatPosition.HoldPosition,
                };
        }
    }

    private static CombatResult DetermineResult(Battle battle)
    {
        if (battle.PlayerRequestedRetreat) return CombatResult.Retreat;
        if (battle.AttackersAlive > 0 && battle.DefendersAlive == 0) return CombatResult.Victory;
        if (battle.DefendersAlive > 0 && battle.AttackersAlive == 0) return CombatResult.Defeat;
        return CombatResult.Draw;
    }
}
