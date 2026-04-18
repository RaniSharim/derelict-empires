using Godot;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// 80×80 cell used in the Tech Tree overlay's tier matrix. Shows state symbol + tier label.
/// Per research_ui_spec.md §5.3.
/// </summary>
public partial class TierMatrixCell : Button
{
    public enum CellState
    {
        Locked,      // predecessor not unlocked
        Available,   // predecessor unlocked, not started
        Active,      // partial progress or current research
        Completed,   // 2-of-3 subsystems researched
        Queued,      // in tier queue
    }

    private TechNodeData? _node;
    private CellState _state;
    private PrecursorColor _color;
    private bool _isSelected;

    public void Configure(TechNodeData node, CellState state, PrecursorColor color, bool isSelected)
    {
        _node = node;
        _state = state;
        _color = color;
        _isSelected = isSelected;
        FocusMode = FocusModeEnum.None;
        ClipText = true;
        CustomMinimumSize = new Vector2(80, 80);
        ApplyStyles();
    }

    private void ApplyStyles()
    {
        var glow = UIColors.GetFactionGlow(_color);
        var style = new StyleBoxFlat();
        style.SetCornerRadiusAll(0);

        switch (_state)
        {
            case CellState.Locked:
                style.BgColor = new Color(glow.R * 0.25f, glow.G * 0.25f, glow.B * 0.25f, 0.15f);
                style.SetBorderWidthAll(1);
                style.BorderColor = new Color(glow.R, glow.G, glow.B, 0.15f);
                break;
            case CellState.Available:
                style.BgColor = new Color(0, 0, 0, 0.4f);
                style.SetBorderWidthAll(1);
                style.BorderColor = new Color(glow.R, glow.G, glow.B, 0.55f);
                break;
            case CellState.Active:
                style.BgColor = new Color(glow.R, glow.G, glow.B, 0.18f);
                style.SetBorderWidthAll(2);
                style.BorderColor = glow;
                break;
            case CellState.Completed:
                style.BgColor = new Color(glow.R, glow.G, glow.B, 0.35f);
                style.SetBorderWidthAll(1);
                style.BorderColor = glow;
                break;
            case CellState.Queued:
                style.BgColor = new Color(0, 0, 0, 0.4f);
                style.SetBorderWidthAll(1);
                style.BorderColor = new Color(glow.R, glow.G, glow.B, 0.4f);
                break;
        }

        if (_isSelected)
        {
            // Cyan-ring selected indicator, always over the state styling so it's unmissable.
            style.BgColor = new Color(style.BgColor.R, style.BgColor.G, style.BgColor.B,
                System.MathF.Min(1f, style.BgColor.A + 0.15f));
            style.BorderColor = UIColors.Accent;
            style.BorderWidthLeft = 3;
            style.BorderWidthTop = 3;
            style.BorderWidthRight = 3;
            style.BorderWidthBottom = 3;
        }

        AddThemeStyleboxOverride("normal", style);

        var hover = style.Duplicate() as StyleBoxFlat ?? new StyleBoxFlat();
        hover.BgColor = new Color(style.BgColor.R, style.BgColor.G, style.BgColor.B,
            System.MathF.Min(1f, style.BgColor.A + 0.12f));
        AddThemeStyleboxOverride("hover", hover);
        AddThemeStyleboxOverride("pressed", style);
        AddThemeStyleboxOverride("focus", style);

        Text = ComposeCellText();
        AddThemeColorOverride("font_color",
            _state == CellState.Locked
                ? new Color(glow.R, glow.G, glow.B, 0.45f)
                : glow);
        // Share Tech Mono at 14 — comfortably above the 10px floor. Symbol at this size is unmissable.
        AddThemeFontOverride("font", UIFonts.ShareTechMono);
        AddThemeFontSizeOverride("font_size", 14);
    }

    private string ComposeCellText()
    {
        if (_node == null) return "";
        string symbol = _state switch
        {
            CellState.Locked => "·",
            CellState.Available => "○",
            CellState.Active => "▶",
            CellState.Completed => "✓",
            CellState.Queued => "⧗",
            _ => ""
        };
        return $"T{_node.Tier}\n{symbol}";
    }
}
