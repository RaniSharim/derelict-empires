using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Floating widget showing turn/cycle number and speed controls.
/// Positioned at bottom, right of minimap area.
/// </summary>
public partial class SpeedTimeWidget : Control
{
    private Label _turnLabel = null!;
    private readonly Button[] _speedButtons = new Button[5];
    private int _activeSpeedIndex = 1; // default Normal

    private static readonly (string label, GameSpeed speed)[] Speeds =
    {
        ("\u23F8", GameSpeed.Paused),   // ⏸
        ("\u00D71", GameSpeed.Normal),  // ×1
        ("\u00D72", GameSpeed.Fast),    // ×2
        ("\u00D74", GameSpeed.Faster),  // ×4
        ("\u00D78", GameSpeed.Fastest), // ×8
    };

    public override void _Ready()
    {
        // Position: bottom-right, below event log
        AnchorLeft = 1;
        AnchorRight = 1;
        AnchorTop = 1;
        AnchorBottom = 1;
        OffsetLeft = -RightPanel.PanelWidth;
        OffsetRight = 0;
        OffsetTop = -58;
        OffsetBottom = -14;
        ZIndex = 60;

        // Background — rounded rectangle
        var bg = new Panel { Name = "Bg" };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = UIColors.GlassDarkFlat;
        bgStyle.SetBorderWidthAll(1);
        bgStyle.BorderColor = UIColors.BorderBright;
        bgStyle.SetCornerRadiusAll(6);
        bg.AddThemeStyleboxOverride("panel", bgStyle);
        AddChild(bg);

        // Content
        var content = new HBoxContainer { Name = "Content" };
        content.SetAnchorsPreset(LayoutPreset.FullRect);
        content.OffsetLeft = 10;
        content.OffsetRight = -10;
        content.AddThemeConstantOverride("separation", 6);
        content.Alignment = BoxContainer.AlignmentMode.Center;
        AddChild(content);

        // Turn/cycle section
        var turnSection = new VBoxContainer();
        turnSection.AddThemeConstantOverride("separation", -2);
        turnSection.Alignment = BoxContainer.AlignmentMode.Center;
        content.AddChild(turnSection);

        _turnLabel = new Label { Text = "T-0" };
        UIFonts.Style(_turnLabel, UIFonts.Exo2SemiBold, 14, UIColors.TextBright);
        turnSection.AddChild(_turnLabel);

        var cycleLabel = new Label { Text = "CYCLE" };
        UIFonts.Style(cycleLabel, UIFonts.ShareTechMono, 7, UIColors.TextFaint);
        turnSection.AddChild(cycleLabel);

        // Divider dot
        var dot = new Label { Text = "\u00B7" };
        UIFonts.Style(dot, UIFonts.ShareTechMono, 12, UIColors.TextFaint);
        dot.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        content.AddChild(dot);

        // Speed buttons
        for (int i = 0; i < Speeds.Length; i++)
        {
            var (label, speed) = Speeds[i];
            var btn = new Button { Text = label };
            btn.CustomMinimumSize = new Vector2(32, 0);
            btn.SizeFlagsVertical = SizeFlags.ShrinkCenter;

            int index = i;
            btn.Pressed += () => OnSpeedPressed(index);

            StyleSpeedButton(btn, i == _activeSpeedIndex);
            content.AddChild(btn);
            _speedButtons[i] = btn;
        }

        // Subscribe
        if (EventBus.Instance != null)
            EventBus.Instance.SpeedChanged += OnSpeedChanged;
    }

    private void OnSpeedPressed(int index)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.CurrentSpeed = Speeds[index].speed;
    }

    private void OnSpeedChanged(GameSpeed speed)
    {
        for (int i = 0; i < Speeds.Length; i++)
        {
            bool active = Speeds[i].speed == speed;
            _activeSpeedIndex = active ? i : _activeSpeedIndex;
            StyleSpeedButton(_speedButtons[i], active);
        }
    }

    private static void StyleSpeedButton(Button btn, bool active)
    {
        var style = new StyleBoxFlat();
        style.SetCornerRadiusAll(4);
        style.SetBorderWidthAll(1);

        if (active)
        {
            // Active = gold fill per spec §5.3
            style.BgColor = UIColors.Moving; // --accent-gold
            style.BorderColor = UIColors.Moving;
            btn.AddThemeColorOverride("font_color", new Color("#0a0e14"));
        }
        else
        {
            style.BgColor = Colors.Transparent;
            style.BorderColor = new Color(80 / 255f, 120 / 255f, 180 / 255f, 0.15f);
            btn.AddThemeColorOverride("font_color", UIColors.TextDim);
        }

        btn.AddThemeStyleboxOverride("normal", style);

        var hoverStyle = new StyleBoxFlat();
        hoverStyle.BgColor = active ? style.BgColor : new Color(16 / 255f, 24 / 255f, 40 / 255f, 0.60f);
        hoverStyle.SetBorderWidthAll(1);
        hoverStyle.BorderColor = UIColors.BorderBright;
        hoverStyle.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("hover", hoverStyle);
        btn.AddThemeStyleboxOverride("pressed", style);
        btn.AddThemeStyleboxOverride("focus", style);

        UIFonts.StyleButton(btn, UIFonts.ShareTechMono, 10,
            active ? new Color("#0a0e14") : UIColors.TextDim);
    }

    public override void _Process(double delta)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        int turn = (int)(gm.GameTime / 60.0);
        _turnLabel.Text = $"T-{turn}";
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.SpeedChanged -= OnSpeedChanged;
    }
}
