using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Combat;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;
using DerlictEmpires.Nodes.Camera;
using DerlictEmpires.Nodes.UI;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Owns BattleManager and combat visuals (system markers + popup).
/// Subscribes to <c>EventBus.CombatStartRequested</c> to engage; ticks the battle manager
/// from <c>EventBus.FastTick</c>; pauses the game on battle end.
/// </summary>
public partial class CombatRouter : Node
{
    private CanvasLayer _uiLayer = null!;
    private StrategyCameraRig _cameraRig = null!;

    private CombatPopup? _activeCombatPopup;
    private BattleManager? _battleManager;
    private int _activeBattleId = -1;
    private readonly Dictionary<int, BattleMarker> _battleMarkers = new();

    public void Configure(CanvasLayer uiLayer, StrategyCameraRig cameraRig)
    {
        _uiLayer = uiLayer;
        _cameraRig = cameraRig;
    }

    public override void _Ready()
    {
        if (EventBus.Instance == null) return;
        EventBus.Instance.CombatStartRequested += OnCombatStartRequested;
        EventBus.Instance.FastTick += OnFastTick;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance == null) return;
        EventBus.Instance.CombatStartRequested -= OnCombatStartRequested;
        EventBus.Instance.FastTick -= OnFastTick;
    }

    private void OnFastTick(float delta) => _battleManager?.ProcessTick(delta);

    private void OnCombatStartRequested(int attackerFleetId, int defenderFleetId)
    {
        var gm = GameManager.Instance;
        var attacker = gm.Fleets.FirstOrDefault(f => f.Id == attackerFleetId);
        var defender = gm.Fleets.FirstOrDefault(f => f.Id == defenderFleetId);
        if (attacker == null || defender == null)
        {
            McpLog.Warn($"[Combat] start rejected: fleet(s) not found (att={attackerFleetId}, def={defenderFleetId})");
            return;
        }

        var attackerEmp = gm.EmpiresById.GetValueOrDefault(attacker.OwnerEmpireId);
        var defenderEmp = gm.EmpiresById.GetValueOrDefault(defender.OwnerEmpireId);
        if (attackerEmp == null || defenderEmp == null) return;

        EngageCombat(attacker, attackerEmp, defender, defenderEmp);
    }

    private void EngageCombat(FleetData attacker, EmpireData attackerEmp,
                              FleetData defender, EmpireData defenderEmp)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Galaxy == null)
        {
            McpLog.Warn("[Combat] start rejected: GameManager/Galaxy not ready");
            return;
        }
        if (_battleManager == null)
        {
            if (gm.MasterSeed == 0)
                McpLog.Warn("[Combat] starting battle before MasterSeed was set — RNG seeded with 0");
            _battleManager = new BattleManager(new GameRandom(gm.MasterSeed).DeriveChild("battles"));
            _battleManager.BattleEnded += OnBattleEndedInternal;
            _battleManager.BattleTicked += id => EventBus.Instance?.FireBattleTick(id);
        }

        int battleId = _battleManager.StartBattle(attacker, attackerEmp, defender, defenderEmp,
            gm.ShipsById, attacker.CurrentSystemId);
        _activeBattleId = battleId;
        EventBus.Instance?.FireCombatStarted(battleId);

        // Pulsing red ring on the battle system.
        var sys = gm.Galaxy?.GetSystem(attacker.CurrentSystemId);
        if (sys != null)
        {
            var marker = new BattleMarker { Name = $"BattleMarker_{battleId}" };
            marker.Position = new Vector3(sys.PositionX, 1.2f, sys.PositionZ);
            AddChild(marker);
            _battleMarkers[battleId] = marker;
        }

        var popup = new CombatPopup { Name = $"CombatPopup_{battleId}" };
        popup.Configure(_battleManager, battleId, attacker.CurrentSystemId, _cameraRig.Camera);
        _uiLayer.AddChild(popup);
        _activeCombatPopup = popup;

        // Auto-select the battle's system so the popup is visible from the start.
        if (sys != null) EventBus.Instance?.FireSystemSelected(sys);
    }

    private void OnBattleEndedInternal(int battleId, CombatResult result)
    {
        McpLog.Info($"[Combat] Battle {battleId} ended: {result}");
        EventBus.Instance?.FireCombatEnded(battleId, result);

        // Popup swaps to debrief on its own and frees itself on CONTINUE.
        _activeCombatPopup = null;

        if (_battleMarkers.TryGetValue(battleId, out var marker))
        {
            marker.QueueFree();
            _battleMarkers.Remove(battleId);
        }

        GameManager.Instance.CurrentSpeed = GameSpeed.Paused;
        EventBus.Instance?.FireGamePaused();

        _activeBattleId = -1;
    }
}
