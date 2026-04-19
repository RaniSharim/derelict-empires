using System;
using Godot;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Reusable 20px-tall glass pill with a colored left accent, label, and `→` glyph.
/// The consistent cross-surface navigation affordance defined in ux_systems_design_2.md §1.3.
/// Invoke with a label, accent color, and an Action to run on click.
/// </summary>
public partial class DeepLinkChip : Button
{
    public const int ChipHeight = 20;
    private const int AccentWidth = 3;

    private Color _accentColor = UIColors.Accent;
    private Action? _onClick;

    /// <summary>
    /// Build a chip. Attach to any container; the chip sizes itself from its text.
    /// </summary>
    public static DeepLinkChip Create(string label, Color accent, Action onClick)
    {
        var chip = new DeepLinkChip
        {
            Text = $"{label}   →",
            _accentColor = accent,
            _onClick = onClick,
            Flat = false,
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = new Vector2(0, ChipHeight),
        };
        return chip;
    }

    public override void _Ready()
    {
        UIFonts.StyleButton(this, UIFonts.RajdhaniMedium, 10, UIColors.TextLabel);
        AddThemeConstantOverride("content_margin_left", AccentWidth + 10);
        AddThemeConstantOverride("content_margin_right", 10);
        AddThemeConstantOverride("content_margin_top", 2);
        AddThemeConstantOverride("content_margin_bottom", 2);

        AddThemeStyleboxOverride("normal", MakeStyle(UIColors.BorderDim, hovered: false));
        AddThemeStyleboxOverride("hover", MakeStyle(UIColors.BorderBright, hovered: true));
        AddThemeStyleboxOverride("pressed", MakeStyle(_accentColor, hovered: true));
        AddThemeStyleboxOverride("focus", MakeStyle(UIColors.BorderBright, hovered: true));

        Pressed += OnPressed;
    }

    private StyleBoxFlat MakeStyle(Color border, bool hovered)
    {
        var style = new StyleBoxFlat
        {
            BgColor = hovered
                ? new Color(_accentColor.R, _accentColor.G, _accentColor.B, 0.14f)
                : new Color(4 / 255f, 8 / 255f, 18 / 255f, 0.75f),
            BorderColor = border,
        };
        style.SetBorderWidthAll(1);
        style.BorderWidthLeft = AccentWidth;
        style.BorderColor = _accentColor; // left accent bar dominates; override only-left
        // For the non-left edges we want the generic border color — StyleBoxFlat can't do per-side colors,
        // so fall back to a single accent-colored outline. The accent left edge still reads because it's 3px wide.
        style.BorderColor = hovered ? UIColors.BorderBright : UIColors.BorderDim;
        style.SetCornerRadiusAll(0);
        return style;
    }

    private void OnPressed()
    {
        _onClick?.Invoke();
    }

    public override void _Draw()
    {
        // Draw the 3px left accent bar on top of the StyleBoxFlat border so it stays glow-colored.
        var rect = new Rect2(0, 0, AccentWidth, Size.Y);
        DrawRect(rect, _accentColor, filled: true);
    }
}
