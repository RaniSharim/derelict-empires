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
            int day = (int)(GameManager.Instance.GameTime / 86400.0); // Rough day counter
            _timeLabel.Text = $"Day {day}";
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
