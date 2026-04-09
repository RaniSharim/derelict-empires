using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Top HUD bar showing game time, speed indicator, and system selection info.
/// </summary>
public partial class TopBar : PanelContainer
{
    private Label _timeLabel = null!;
    private Label _speedLabel = null!;
    private Label _selectionLabel = null!;
    private SpeedControlPanel _speedControl = null!;

    public override void _Ready()
    {
        // Layout
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 20);
        AddChild(hbox);

        _timeLabel = new Label { Text = "Day 0" };
        _timeLabel.AddThemeFontSizeOverride("font_size", 14);
        hbox.AddChild(_timeLabel);

        _speedControl = new SpeedControlPanel();
        hbox.AddChild(_speedControl);

        _speedLabel = new Label { Text = "Speed: 1x" };
        _speedLabel.AddThemeFontSizeOverride("font_size", 14);
        hbox.AddChild(_speedLabel);

        // Spacer
        var spacer = new Control();
        spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        hbox.AddChild(spacer);

        _selectionLabel = new Label { Text = "" };
        _selectionLabel.AddThemeFontSizeOverride("font_size", 14);
        hbox.AddChild(_selectionLabel);

        // Anchors: top of screen, full width
        AnchorsPreset = (int)LayoutPreset.TopWide;

        // Events
        if (EventBus.Instance != null)
        {
            EventBus.Instance.SpeedChanged += OnSpeedChanged;
            EventBus.Instance.SystemSelected += sys => _selectionLabel.Text = sys.Name;
            EventBus.Instance.SystemDeselected += () => _selectionLabel.Text = "";
        }
    }

    public override void _Process(double delta)
    {
        if (GameManager.Instance != null)
        {
            // 1 game-second = ~1 minute of in-game time for pacing
            // Display as day count (1 day = 1440 game-seconds at this scale)
            double gameTime = GameManager.Instance.GameTime;
            int days = (int)(gameTime / 60.0); // ~1 minute real = 1 day at 1x
            int hours = (int)((gameTime % 60.0) / 2.5);
            _timeLabel.Text = $"Day {days}, {hours:00}:00";
        }
    }

    private void OnSpeedChanged(GameSpeed speed)
    {
        string text = speed switch
        {
            GameSpeed.Paused => "PAUSED",
            GameSpeed.Normal => "Speed: 1x",
            GameSpeed.Fast => "Speed: 2x",
            GameSpeed.Faster => "Speed: 4x",
            GameSpeed.Fastest => "Speed: 8x",
            _ => "Speed: ?"
        };
        _speedLabel.Text = text;
    }
}
