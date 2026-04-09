using System.Collections.Generic;
using Godot;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Autoloads;

/// <summary>
/// Global game state container. Does not own logic — systems read from it.
/// Registered as an autoload — access via GameManager.Instance.
/// </summary>
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; } = null!;

    public override void _Ready()
    {
        Instance = this;
        GD.Print("[GameManager] Ready");
    }

    // === Game state ===
    public Core.Enums.GameState CurrentState { get; set; } = Core.Enums.GameState.MainMenu;

    private GameSpeed _currentSpeed = GameSpeed.Normal;
    public GameSpeed CurrentSpeed
    {
        get => _currentSpeed;
        set
        {
            if (_currentSpeed == value) return;
            var old = _currentSpeed;
            _currentSpeed = value;

            if (value == GameSpeed.Paused && old != GameSpeed.Paused)
                EventBus.Instance?.FireGamePaused();
            else if (value != GameSpeed.Paused && old == GameSpeed.Paused)
                EventBus.Instance?.FireGameResumed();

            EventBus.Instance?.FireSpeedChanged(value);
        }
    }

    // === Galaxy ===
    public GalaxyData? Galaxy { get; set; }

    // === Empires ===
    public List<EmpireData> Empires { get; set; } = new();
    public EmpireData? LocalPlayerEmpire => Empires.Find(e => e.IsHuman);

    // === Time ===
    /// <summary>Total elapsed game-seconds since game start.</summary>
    public double GameTime { get; set; }

    // === Seed ===
    /// <summary>Master seed for all game randomization. Set at game creation.</summary>
    public int MasterSeed { get; set; }
}
