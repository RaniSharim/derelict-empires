using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using System.Collections.Generic;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// One faction's resource display in the topbar.
/// Shows 6 resources: 3 common (Row A) + 3 rare (Row B).
/// Uses Control base (not PanelContainer) to avoid auto-sizing.
/// </summary>
public partial class FactionResourceBox : Control
{
    private readonly PrecursorColor _faction;
    private readonly Color _glowColor;
    private readonly Color _bgColor;

    private readonly Label[,] _stockLabels = new Label[2, 3];
    private readonly Label[,] _deltaLabels = new Label[2, 3];

    private static readonly ResourceType[,] ResourceLayout =
    {
        { ResourceType.SimpleParts, ResourceType.SimpleMaterials, ResourceType.SimpleEnergy },
        { ResourceType.AdvancedParts, ResourceType.AdvancedMaterials, ResourceType.AdvancedEnergy }
    };

    private readonly Dictionary<string, float> _incomeCache = new();

    public FactionResourceBox(PrecursorColor faction)
    {
        _faction = faction;
        _glowColor = UIColors.GetFactionGlow(faction);
        _bgColor = UIColors.GetFactionBg(faction);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        ClipContents = true;
    }

    public override void _Ready()
    {
        // Background panel with tarnished glass
        var bg = new PanelContainer { Name = "Bg" };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        GlassPanel.Apply(bg, enableBlur: false);
        AddChild(bg);

        // Faction tint overlay
        var tint = new ColorRect { Name = "Tint" };
        tint.Color = _bgColor;
        tint.SetAnchorsPreset(LayoutPreset.FullRect);
        tint.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(tint);

        // Solid accent bar on the left
        var accentBar = new ColorRect();
        accentBar.Color = _glowColor;
        accentBar.CustomMinimumSize = new Vector2(3, 0);
        accentBar.SetAnchorsPreset(LayoutPreset.LeftWide);
        AddChild(accentBar);

        // Content VBox fills the control
        var content = new VBoxContainer { Name = "Content" };
        content.SetAnchorsPreset(LayoutPreset.FullRect);
        // Leave 3px left for the accent border
        content.OffsetLeft = 3;
        // Center the rows vertically
        content.Alignment = BoxContainer.AlignmentMode.Center;
        content.AddThemeConstantOverride("separation", 2);
        AddChild(content);

        // Sub-margin for centering visually
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 6);
        margin.AddThemeConstantOverride("margin_right", 6);
        margin.AddThemeConstantOverride("margin_top", 4);
        margin.AddThemeConstantOverride("margin_bottom", 4);
        content.AddChild(margin);

        var rowsContainer = new VBoxContainer();
        rowsContainer.AddThemeConstantOverride("separation", 3);
        margin.AddChild(rowsContainer);

        // Row A — Common resources
        AddResourceRow(rowsContainer, 0, false);

        // Subtle divider
        AddDivider(rowsContainer, new Color(1f, 1f, 1f, 0.06f));

        // Row B — Rare resources
        AddResourceRow(rowsContainer, 1, true);
    }

    private void AddResourceRow(VBoxContainer parent, int rowIndex, bool isRare)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);
        row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        if (isRare)
            row.Modulate = new Color(1, 1, 1, 0.85f);
        parent.AddChild(row);

        for (int col = 0; col < 3; col++)
        {
            var cellContainer = new PanelContainer();
            cellContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            cellContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
            var cellStyle = new StyleBoxFlat();
            cellStyle.BgColor = isRare ? new Color(0, 0, 0, 0.28f) : new Color(0, 0, 0, 0.18f);
            cellStyle.SetBorderWidthAll(1);
            cellStyle.BorderColor = new Color(1f, 1f, 1f, 0.05f);
            cellStyle.SetCornerRadiusAll(0);
            cellStyle.ContentMarginLeft = 4;
            cellStyle.ContentMarginRight = 4;
            cellStyle.ContentMarginTop = 2;
            cellStyle.ContentMarginBottom = 2;
            cellContainer.AddThemeStyleboxOverride("panel", cellStyle);
            row.AddChild(cellContainer);

            var cell = new HBoxContainer();
            cell.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            cell.Alignment = BoxContainer.AlignmentMode.Center;
            cell.AddThemeConstantOverride("separation", 3);
            cellContainer.AddChild(cell);

            // Icon element
            var icon = new FactionIcon(_faction, _glowColor);
            icon.CustomMinimumSize = new Vector2(10, 10);
            icon.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            cell.AddChild(icon);

            // Numbers layout
            var stockLabel = new Label { Text = "0" };
            int stockSize = isRare ? 9 : 10;
            var stockColor = isRare ? new Color(_glowColor, 0.80f) : _glowColor;
            UIFonts.Style(stockLabel, UIFonts.ShareTechMono, stockSize, stockColor);
            cell.AddChild(stockLabel);
            _stockLabels[rowIndex, col] = stockLabel;

            var deltaLabel = new Label { Text = "(+0)" };
            int deltaSize = isRare ? 7 : 8;
            UIFonts.Style(deltaLabel, UIFonts.ShareTechMono, deltaSize, UIColors.DeltaPos);
            cell.AddChild(deltaLabel);
            _deltaLabels[rowIndex, col] = deltaLabel;
        }
    }

    private static void AddDivider(VBoxContainer parent, Color color)
    {
        var div = new ColorRect();
        div.CustomMinimumSize = new Vector2(0, 1);
        div.Color = color;
        parent.AddChild(div);
    }

    public override void _Process(double delta)
    {
        var empire = GameManager.Instance?.LocalPlayerEmpire;
        if (empire == null) return;

        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                var type = ResourceLayout[row, col];
                float amount = empire.GetResource(_faction, type);
                _stockLabels[row, col].Text = FormatStock(amount);

                var key = EmpireData.ResourceKey(_faction, type);
                float income = _incomeCache.GetValueOrDefault(key);
                _deltaLabels[row, col].Text = FormatDelta(income);
                _deltaLabels[row, col].AddThemeColorOverride("font_color",
                    income >= 0 ? UIColors.DeltaPos : UIColors.DeltaNeg);
            }
        }
    }

    public void UpdateIncome(Dictionary<string, float> income)
    {
        _incomeCache.Clear();
        foreach (var kv in income)
        {
            if (kv.Key.StartsWith(_faction.ToString()))
                _incomeCache[kv.Key] = kv.Value;
        }
    }

    private static string FormatStock(float amount)
    {
        if (amount >= 1_000_000) return $"{amount / 1_000_000f:F1}M";
        if (amount >= 10_000) return $"{amount / 1_000f:F0}K";
        return $"{amount:F0}";
    }

    private static string FormatDelta(float income)
    {
        if (income > 0.01f) return $"(+{income:F0})";
        if (income < -0.01f) return $"({income:F0})";
        return "(+0)";
    }
}

public partial class FactionIcon : Control
{
    private readonly PrecursorColor _faction;
    private readonly Color _color;

    public FactionIcon(PrecursorColor faction, Color color)
    {
        _faction = faction;
        _color = color;
    }

    public override void _Draw()
    {
        float w = Size.X;
        float h = Size.Y;
        var center = new Vector2(w / 2, h / 2);

        switch (_faction)
        {
            case PrecursorColor.Red: // Hexagon
                var hex = new Vector2[6];
                for (int i = 0; i < 6; i++)
                {
                    float angle = Mathf.Pi / 3 * i + Mathf.Pi / 6;
                    hex[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (w / 2);
                }
                DrawPolygon(hex, new[] { _color });
                break;
            case PrecursorColor.Blue: // Diamond
                DrawPolygon(new[] {
                    new Vector2(w / 2, 0),
                    new Vector2(w, h / 2),
                    new Vector2(w / 2, h),
                    new Vector2(0, h / 2)
                }, new[] { _color });
                break;
            case PrecursorColor.Green: // Triangle
                DrawPolygon(new[] {
                    new Vector2(w / 2, 0),
                    new Vector2(w, h),
                    new Vector2(0, h)
                }, new[] { _color });
                break;
            case PrecursorColor.Gold: // Square
                DrawRect(new Rect2(0, 0, w, h), _color);
                break;
            case PrecursorColor.Purple: // Star
                var star = new Vector2[10];
                float outR = w / 2;
                float inR = w / 4;
                for (int i = 0; i < 10; i++)
                {
                    float angle = Mathf.Pi / 5 * i - Mathf.Pi / 2;
                    float r = (i % 2 == 0) ? outR : inR;
                    star[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                }
                DrawPolygon(star, new[] { _color });
                break;
            default:
                DrawRect(new Rect2(0, 0, w, h), _color);
                break;
        }
    }
}
