using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// HUD panel with speed control buttons (pause, 1x, 2x, 4x, 8x).
/// </summary>
public partial class SpeedControlPanel : HBoxContainer
{
    private Button _pauseBtn = null!;
    private Button _normalBtn = null!;
    private Button _fastBtn = null!;
    private Button _fasterBtn = null!;
    private Button _fastestBtn = null!;

    public override void _Ready()
    {
        _pauseBtn = CreateSpeedButton("||", GameSpeed.Paused);
        _normalBtn = CreateSpeedButton(">", GameSpeed.Normal);
        _fastBtn = CreateSpeedButton(">>", GameSpeed.Fast);
        _fasterBtn = CreateSpeedButton(">>>", GameSpeed.Faster);
        _fastestBtn = CreateSpeedButton(">>>>", GameSpeed.Fastest);

        UpdateVisuals(GameManager.Instance?.CurrentSpeed ?? GameSpeed.Normal);
        if (EventBus.Instance != null)
            EventBus.Instance.SpeedChanged += UpdateVisuals;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("pause"))
        {
            TogglePause();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("speed_up"))
        {
            CycleSpeedUp();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("speed_down"))
        {
            CycleSpeedDown();
            GetViewport().SetInputAsHandled();
        }
    }

    private Button CreateSpeedButton(string text, GameSpeed speed)
    {
        var btn = new Button();
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(40, 30);
        btn.Pressed += () => SetSpeed(speed);
        AddChild(btn);
        return btn;
    }

    private void SetSpeed(GameSpeed speed)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.CurrentSpeed = speed;
    }

    private void TogglePause()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.CurrentSpeed =
            GameManager.Instance.CurrentSpeed == GameSpeed.Paused
                ? GameSpeed.Normal
                : GameSpeed.Paused;
    }

    private static readonly GameSpeed[] SpeedLevels =
    {
        GameSpeed.Paused, GameSpeed.Normal, GameSpeed.Fast, GameSpeed.Faster, GameSpeed.Fastest
    };

    private void CycleSpeedUp()
    {
        if (GameManager.Instance == null) return;
        var current = GameManager.Instance.CurrentSpeed;
        for (int i = 0; i < SpeedLevels.Length - 1; i++)
        {
            if (SpeedLevels[i] == current)
            {
                GameManager.Instance.CurrentSpeed = SpeedLevels[i + 1];
                return;
            }
        }
    }

    private void CycleSpeedDown()
    {
        if (GameManager.Instance == null) return;
        var current = GameManager.Instance.CurrentSpeed;
        for (int i = 1; i < SpeedLevels.Length; i++)
        {
            if (SpeedLevels[i] == current)
            {
                GameManager.Instance.CurrentSpeed = SpeedLevels[i - 1];
                return;
            }
        }
    }

    private void UpdateVisuals(GameSpeed speed)
    {
        _pauseBtn.Disabled = speed == GameSpeed.Paused;
        _normalBtn.Disabled = speed == GameSpeed.Normal;
        _fastBtn.Disabled = speed == GameSpeed.Fast;
        _fasterBtn.Disabled = speed == GameSpeed.Faster;
        _fastestBtn.Disabled = speed == GameSpeed.Fastest;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.SpeedChanged -= UpdateVisuals;
    }
}
