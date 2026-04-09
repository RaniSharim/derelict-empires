using System;
using Godot;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Autoloads;

/// <summary>
/// Real-time tick dispatcher. Accumulates scaled delta time and fires
/// fast ticks (0.1 game-seconds) and slow ticks (1.0 game-seconds) via EventBus.
/// Registered as an autoload.
/// </summary>
public partial class TurnManager : Node
{
    public static TurnManager Instance { get; private set; } = null!;

    public const float FastTickInterval = 0.1f;
    public const float SlowTickInterval = 1.0f;

    private double _fastTickAccum;
    private double _slowTickAccum;

    /// <summary>Total fast ticks fired since game start.</summary>
    public long FastTickCount { get; private set; }

    /// <summary>Total slow ticks fired since game start.</summary>
    public long SlowTickCount { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        GD.Print("[TurnManager] Ready");
    }

    public override void _Process(double delta)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (gm.CurrentSpeed == GameSpeed.Paused) return;

        double scaledDelta = delta * (int)gm.CurrentSpeed;

        // Advance game time
        gm.GameTime += scaledDelta;

        // Fast tick
        _fastTickAccum += scaledDelta;
        while (_fastTickAccum >= FastTickInterval)
        {
            _fastTickAccum -= FastTickInterval;
            FastTickCount++;
            EventBus.Instance?.FireFastTick(FastTickInterval);
        }

        // Slow tick
        _slowTickAccum += scaledDelta;
        while (_slowTickAccum >= SlowTickInterval)
        {
            _slowTickAccum -= SlowTickInterval;
            SlowTickCount++;
            EventBus.Instance?.FireSlowTick(SlowTickInterval);
        }
    }
}
