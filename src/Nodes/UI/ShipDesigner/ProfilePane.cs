using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Ships;

namespace DerlictEmpires.Nodes.UI.ShipDesigner;

/// <summary>
/// Right pane of the ShipDesigner. Shows draft name (editable), role, stats with delta-vs-saved,
/// per-color supply drain strip, and a Build Requirements block (full RESOLVE BLOCKERS is Phase F).
/// </summary>
public partial class ProfilePane : PanelContainer
{
    private readonly ShipDesignerOverlay _overlay;

    private LineEdit _nameEdit = null!;
    private OptionButton _roleDropdown = null!;
    private Label _hpValue = null!;
    private Label _speedValue = null!;
    private Label _visValue = null!;
    private Label _maintValue = null!;
    private Label _capacityValue = null!;
    private Label _expertiseValue = null!;
    private HBoxContainer _supplyStrip = null!;
    private VBoxContainer _requirementsBox = null!;

    private static readonly FleetRole[] Roles = new[]
    {
        FleetRole.Brawler, FleetRole.Guardian, FleetRole.Carrier,
        FleetRole.Bombard, FleetRole.Scout, FleetRole.NonCombatant
    };

    public ProfilePane(ShipDesignerOverlay overlay)
    {
        _overlay = overlay;
    }

    public override void _Ready()
    {
        var bg = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.25f), BorderColor = UIColors.BorderDim };
        bg.SetBorderWidthAll(1);
        bg.SetCornerRadiusAll(0);
        AddThemeStyleboxOverride("panel", bg);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        AddChild(margin);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 10);
        margin.AddChild(col);

        col.AddChild(BuildHeader());
        col.AddChild(BuildNameEdit());
        col.AddChild(BuildRoleRow());
        col.AddChild(BuildSeparator());

        var statsHeader = new Label { Text = "STATS" };
        UIFonts.StyleRole(statsHeader, UIFonts.Role.UILabel);
        col.AddChild(statsHeader);
        col.AddChild(BuildStatRow("HP",        out _hpValue));
        col.AddChild(BuildStatRow("SPEED",     out _speedValue));
        col.AddChild(BuildStatRow("VISIBILITY", out _visValue));
        col.AddChild(BuildStatRow("MAINT.",    out _maintValue));
        col.AddChild(BuildStatRow("CAPACITY",  out _capacityValue));
        col.AddChild(BuildStatRow("EXPERTISE", out _expertiseValue));

        col.AddChild(BuildSeparator());

        var supplyHeader = new Label { Text = "SUPPLY DRAIN" };
        UIFonts.StyleRole(supplyHeader, UIFonts.Role.UILabel);
        col.AddChild(supplyHeader);

        _supplyStrip = new HBoxContainer();
        _supplyStrip.AddThemeConstantOverride("separation", 4);
        _supplyStrip.CustomMinimumSize = new Vector2(0, 18);
        col.AddChild(_supplyStrip);

        col.AddChild(BuildSeparator());

        var reqHeader = new Label { Text = "BUILD REQUIREMENTS" };
        UIFonts.StyleRole(reqHeader, UIFonts.Role.UILabel);
        col.AddChild(reqHeader);

        _requirementsBox = new VBoxContainer();
        _requirementsBox.AddThemeConstantOverride("separation", 2);
        col.AddChild(_requirementsBox);

        Refresh();
    }

    public void Refresh()
    {
        var profile = _overlay.CurrentProfile();
        var stats = profile.Stats;
        var originalProfile = _overlay.OriginalSnapshot != null
            ? ShipDesignProfiler.Build(_overlay.OriginalSnapshot, _overlay.Registry, _overlay.Expertise,
                _overlay.ResearchState, _overlay.EmpireAffinity)
            : (ShipDesignProfiler.Profile?)null;

        var originalStats = originalProfile?.Stats;

        _hpValue.Text = FormatWithDelta(stats.Hp, originalStats?.Hp);
        _speedValue.Text = FormatWithDelta(stats.Speed, originalStats?.Speed);
        _visValue.Text = FormatWithDelta(stats.Visibility, originalStats?.Visibility);
        _maintValue.Text = FormatWithDelta(stats.MaintenanceCost, originalStats?.MaintenanceCost);
        _capacityValue.Text = $"{stats.UsedCapacity} / {stats.TotalCapacity}";
        _expertiseValue.Text = $"{profile.AverageExpertiseMultiplier:0.00}×";

        RebuildSupplyStrip(profile);
        RebuildRequirements(profile);

        if (_nameEdit.Text != _overlay.Draft.Name)
            _nameEdit.Text = _overlay.Draft.Name;
    }

    // === Builders ===========================================================

    private Control BuildHeader()
    {
        var title = new Label { Text = "DESIGN PROFILE" };
        UIFonts.StyleRole(title, UIFonts.Role.UILabel);
        return title;
    }

    private Control BuildNameEdit()
    {
        _nameEdit = new LineEdit
        {
            Text = _overlay.Draft.Name,
            PlaceholderText = "Design name",
        };
        UIFonts.Style(new Label(), UIFonts.Exo2SemiBold, 13, UIColors.TextBright);
        _nameEdit.AddThemeFontOverride("font", UIFonts.Exo2SemiBold);
        _nameEdit.AddThemeFontSizeOverride("font_size", 13);
        _nameEdit.AddThemeColorOverride("font_color", UIColors.TextBright);
        _nameEdit.CustomMinimumSize = new Vector2(0, 32);

        var style = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.4f), BorderColor = UIColors.BorderMid };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(0);
        style.ContentMarginLeft = 10;
        style.ContentMarginRight = 10;
        _nameEdit.AddThemeStyleboxOverride("normal", style);
        _nameEdit.AddThemeStyleboxOverride("focus", style);

        _nameEdit.TextChanged += t => _overlay.SetName(t);
        return _nameEdit;
    }

    private Control BuildRoleRow()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var label = new Label { Text = "ROLE" };
        UIFonts.StyleRole(label, UIFonts.Role.UILabel);
        label.CustomMinimumSize = new Vector2(80, 0);
        label.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(label);

        _roleDropdown = new OptionButton();
        _roleDropdown.CustomMinimumSize = new Vector2(0, 28);
        _roleDropdown.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        UIFonts.StyleButtonRole(_roleDropdown, UIFonts.Role.UILabel);
        GlassPanel.StyleButton(_roleDropdown);
        for (int i = 0; i < Roles.Length; i++)
            _roleDropdown.AddItem(Roles[i].ToString().ToUpperInvariant(), i);
        _roleDropdown.Select(0);
        row.AddChild(_roleDropdown);

        return row;
    }

    private Control BuildStatRow(string label, out Label valueLabel)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var keyLabel = new Label { Text = label };
        UIFonts.StyleRole(keyLabel, UIFonts.Role.UILabel);
        keyLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(keyLabel);

        valueLabel = new Label { Text = "—" };
        UIFonts.StyleRole(valueLabel, UIFonts.Role.DataLarge, UIColors.TextBright);
        row.AddChild(valueLabel);
        return row;
    }

    private Control BuildSeparator()
    {
        var sep = new ColorRect { Color = UIColors.BorderDim };
        sep.CustomMinimumSize = new Vector2(0, 1);
        return sep;
    }

    // === Dynamic sections ===================================================

    private void RebuildSupplyStrip(ShipDesignProfiler.Profile profile)
    {
        foreach (var child in _supplyStrip.GetChildren()) child.QueueFree();

        if (profile.TotalSupply <= 0f)
        {
            var empty = new Label { Text = "NO DRAIN" };
            UIFonts.StyleRole(empty, UIFonts.Role.DataSmall);
            _supplyStrip.AddChild(empty);
            return;
        }

        // Segmented bar: each color takes a width proportional to its share of total drain.
        foreach (var kv in profile.SupplyPerColor.OrderByDescending(k => k.Value))
        {
            float share = kv.Value / profile.TotalSupply;
            var seg = new PanelContainer();
            seg.CustomMinimumSize = new Vector2(Mathf.Max(24, 240 * share), 16);
            seg.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            seg.SizeFlagsStretchRatio = share;

            var segStyle = new StyleBoxFlat
            {
                BgColor = new Color(UIColors.GetFactionGlow(kv.Key).R,
                                    UIColors.GetFactionGlow(kv.Key).G,
                                    UIColors.GetFactionGlow(kv.Key).B,
                                    0.60f),
            };
            segStyle.SetBorderWidthAll(0);
            segStyle.SetCornerRadiusAll(0);
            seg.AddThemeStyleboxOverride("panel", segStyle);

            var label = new Label { Text = $"{kv.Value:0.#}" };
            UIFonts.Style(label, UIFonts.ShareTechMono, 9, UIColors.TextBright);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            seg.AddChild(label);

            var tip = $"{kv.Key.ToString().ToUpperInvariant()} · {kv.Value:0.##}/sec";
            seg.TooltipText = tip;

            _supplyStrip.AddChild(seg);
        }
    }

    private void RebuildRequirements(ShipDesignProfiler.Profile profile)
    {
        foreach (var child in _requirementsBox.GetChildren()) child.QueueFree();

        var blockers = new List<(string msg, Color color)>();

        if (profile.UnfilledRequiredSlots > 0)
            blockers.Add(($"\u26a0  {profile.UnfilledRequiredSlots} required slot(s) empty", UIColors.Moving));

        if (profile.LockedSlotIndexes.Count > 0)
            blockers.Add(($"\u26ab  {profile.LockedSlotIndexes.Count} locked module(s)", UIColors.TextFaint));

        if (profile.UnresearchedSlotIndexes.Count > 0)
            blockers.Add(($"\u29d7  {profile.UnresearchedSlotIndexes.Count} unresearched module(s)", UIColors.AccentOrange));

        if (profile.OffAffinity)
            blockers.Add((
                $"\u25b2  OFF-AFFINITY DESIGN ({profile.DominantColor.ToString().ToUpperInvariant()} dominant)",
                UIColors.TextDim));

        if (blockers.Count == 0)
        {
            var ok = new Label { Text = "\u2713 READY TO BUILD" };
            UIFonts.StyleRole(ok, UIFonts.Role.BodyPrimary, UIColors.DeltaPos);
            _requirementsBox.AddChild(ok);
            return;
        }

        foreach (var (msg, color) in blockers)
        {
            var lbl = new Label { Text = msg };
            UIFonts.StyleRole(lbl, UIFonts.Role.BodySecondary, color);
            _requirementsBox.AddChild(lbl);
        }
    }

    // === Formatters =========================================================

    private static string FormatWithDelta(int current, int? original)
    {
        if (!original.HasValue || current == original.Value) return current.ToString();
        int delta = current - original.Value;
        string sign = delta > 0 ? "+" : "";
        return $"{current}  ({sign}{delta})";
    }

    private static string FormatWithDelta(float current, float? original)
    {
        if (!original.HasValue || Math.Abs(current - original.Value) < 0.05f) return current.ToString("0.#");
        float delta = current - original.Value;
        string sign = delta > 0 ? "+" : "";
        return $"{current:0.#}  ({sign}{delta:0.#})";
    }
}
