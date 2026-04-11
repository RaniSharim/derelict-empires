using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using System.Collections.Generic;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// One faction's resource display in the topbar.
/// Shows 4 resources: 2 common (Row A) + 2 rare (Row B).
/// Uses Control base (not PanelContainer) to avoid auto-sizing.
/// </summary>
public partial class FactionResourceBox : Control
{
    private readonly PrecursorColor _faction;
    private readonly Color _glowColor;
    private readonly Color _bgColor;

    private readonly Label[,] _stockLabels = new Label[2, 2];
    private readonly Label[,] _deltaLabels = new Label[2, 2];

    private static readonly ResourceType[,] ResourceLayout =
    {
        { ResourceType.SimpleEnergy, ResourceType.SimpleParts },
        { ResourceType.AdvancedEnergy, ResourceType.AdvancedParts }
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
        // Background panel with faction tint + 3px left accent border
        var bg = new Panel { Name = "Bg" };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = _bgColor;
        panelStyle.BorderWidthLeft = 3;
        panelStyle.BorderWidthTop = 1;
        panelStyle.BorderWidthRight = 1;
        panelStyle.BorderWidthBottom = 1;
        panelStyle.BorderColor = new Color(_glowColor, 0.35f);
        panelStyle.SetCornerRadiusAll(0);
        bg.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(bg);

        // Content VBox fills the control
        var content = new VBoxContainer { Name = "Content" };
        content.SetAnchorsPreset(LayoutPreset.FullRect);
        // Leave 3px left for the accent border
        content.OffsetLeft = 3;
        content.AddThemeConstantOverride("separation", 0);
        AddChild(content);

        // Header row
        var headerMargin = new MarginContainer();
        headerMargin.AddThemeConstantOverride("margin_left", 6);
        headerMargin.AddThemeConstantOverride("margin_top", 2);
        headerMargin.AddThemeConstantOverride("margin_bottom", 1);
        headerMargin.AddThemeConstantOverride("margin_right", 4);
        content.AddChild(headerMargin);

        var factionLabel = new Label();
        factionLabel.Text = GetFactionName(_faction);
        UIFonts.Style(factionLabel, UIFonts.BarlowSemiBold, 9, _glowColor);
        headerMargin.AddChild(factionLabel);

        // Divider
        AddDivider(content, new Color(60 / 255f, 110 / 255f, 160 / 255f, 0.30f));

        // Row A — Common resources
        AddResourceRow(content, 0, false);

        // Subtle divider
        AddDivider(content, new Color(1f, 1f, 1f, 0.06f));

        // Row B — Rare resources
        AddResourceRow(content, 1, true);
    }

    private void AddResourceRow(VBoxContainer parent, int rowIndex, bool isRare)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 2);
        row.SizeFlagsVertical = SizeFlags.ExpandFill;
        if (isRare)
            row.Modulate = new Color(1, 1, 1, 0.85f);
        parent.AddChild(row);

        for (int col = 0; col < 2; col++)
        {
            var cell = new PanelContainer();
            cell.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            cell.SizeFlagsVertical = SizeFlags.ExpandFill;
            var cellStyle = new StyleBoxFlat();
            cellStyle.BgColor = isRare ? new Color(0, 0, 0, 0.28f) : new Color(0, 0, 0, 0.18f);
            cellStyle.SetBorderWidthAll(1);
            cellStyle.BorderColor = new Color(1f, 1f, 1f, 0.05f);
            cellStyle.SetCornerRadiusAll(0);
            cellStyle.ContentMarginLeft = 3;
            cellStyle.ContentMarginRight = 2;
            cellStyle.ContentMarginTop = 1;
            cellStyle.ContentMarginBottom = 0;
            cell.AddThemeStyleboxOverride("panel", cellStyle);
            row.AddChild(cell);

            var cellLayout = new HBoxContainer();
            cellLayout.AddThemeConstantOverride("separation", 2);
            cell.AddChild(cellLayout);

            // Icon placeholder
            var icon = new ColorRect();
            icon.CustomMinimumSize = new Vector2(8, 8);
            icon.Color = new Color(_glowColor, 0.6f);
            icon.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            cellLayout.AddChild(icon);

            // Number column
            var numCol = new VBoxContainer();
            numCol.AddThemeConstantOverride("separation", -2);
            cellLayout.AddChild(numCol);

            var stockLabel = new Label { Text = "0" };
            int stockSize = isRare ? 9 : 10;
            var stockColor = isRare ? new Color(_glowColor, 0.80f) : _glowColor;
            UIFonts.Style(stockLabel, UIFonts.ShareTechMono, stockSize, stockColor);
            numCol.AddChild(stockLabel);
            _stockLabels[rowIndex, col] = stockLabel;

            var deltaLabel = new Label { Text = "+0" };
            int deltaSize = isRare ? 7 : 8;
            UIFonts.Style(deltaLabel, UIFonts.ShareTechMono, deltaSize, UIColors.DeltaPos);
            numCol.AddChild(deltaLabel);
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
            for (int col = 0; col < 2; col++)
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
        if (income > 0.01f) return $"+{income:F0}";
        if (income < -0.01f) return $"{income:F0}";
        return "+0";
    }

    private static string GetFactionName(PrecursorColor color) => color switch
    {
        PrecursorColor.Red    => "CRIMSON FORGE",
        PrecursorColor.Blue   => "AZURE LATTICE",
        PrecursorColor.Green  => "VERDANT SYNTHESIS",
        PrecursorColor.Gold   => "GOLDEN ASCENDANCY",
        PrecursorColor.Purple => "OBSIDIAN COVENANT",
        _ => "UNKNOWN"
    };
}
