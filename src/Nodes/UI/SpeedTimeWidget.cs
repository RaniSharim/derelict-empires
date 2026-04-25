using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Bottom-right speed control: T-N cycle counter + ⏸/×1/×2/×4/×8 buttons.
/// Layout in <c>scenes/ui/speed_time_widget.tscn</c>; this script wires button
/// presses, listens for <c>SpeedChanged</c>, and styles active/inactive states.
/// </summary>
public partial class SpeedTimeWidget : Control
{
    [Export] private Label _turnLabel = null!;
    [Export] private Button _btnPause = null!;
    [Export] private Button _btnNormal = null!;
    [Export] private Button _btnFast = null!;
    [Export] private Button _btnFaster = null!;
    [Export] private Button _btnFastest = null!;

    private readonly Button[] _buttons = new Button[5];
    private static readonly GameSpeed[] Speeds =
        { GameSpeed.Paused, GameSpeed.Normal, GameSpeed.Fast, GameSpeed.Faster, GameSpeed.Fastest };

    public override void _Ready()
    {
        _buttons[0] = _btnPause;
        _buttons[1] = _btnNormal;
        _buttons[2] = _btnFast;
        _buttons[3] = _btnFaster;
        _buttons[4] = _btnFastest;

        UIFonts.Style(_turnLabel, UIFonts.Title, UIFonts.TitleSize, UIColors.TextBright);
        UIFonts.Style(GetNode<Label>("Content/TurnSection/CycleLabel"), UIFonts.Main, UIFonts.SmallSize, UIColors.TextFaint);
        UIFonts.Style(GetNode<Label>("Content/Dot"), UIFonts.Main, UIFonts.SmallSize, UIColors.TextFaint);

        for (int i = 0; i < _buttons.Length; i++)
        {
            int idx = i;
            _buttons[i].Pressed += () => OnSpeedPressed(idx);
            StyleSpeedButton(_buttons[i], i == 1); // default Normal
        }

        if (EventBus.Instance != null)
            EventBus.Instance.SpeedChanged += OnSpeedChanged;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.SpeedChanged -= OnSpeedChanged;
    }

    public override void _Process(double delta)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        _turnLabel.Text = $"T-{(int)(gm.GameTime / 60.0)}";
    }

    private void OnSpeedPressed(int index)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.CurrentSpeed = Speeds[index];
    }

    private void OnSpeedChanged(GameSpeed speed)
    {
        for (int i = 0; i < _buttons.Length; i++)
            StyleSpeedButton(_buttons[i], Speeds[i] == speed);
    }

    private static void StyleSpeedButton(Button btn, bool active)
    {
        var style = new StyleBoxFlat();
        style.SetCornerRadiusAll(4);
        style.SetBorderWidthAll(1);

        if (active)
        {
            style.BgColor = UIColors.Moving;
            style.BorderColor = UIColors.Moving;
        }
        else
        {
            style.BgColor = Colors.Transparent;
            style.BorderColor = new Color(80 / 255f, 120 / 255f, 180 / 255f, 0.15f);
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

        UIFonts.StyleButton(btn, UIFonts.Main, UIFonts.SmallSize,
            active ? new Color("#0a0e14") : UIColors.TextDim);
    }
}
