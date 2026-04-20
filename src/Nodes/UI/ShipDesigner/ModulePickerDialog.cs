using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Nodes.UI.ShipDesigner;

/// <summary>
/// Modal popup for choosing a Ship module to drop into a slot. Replaces the old
/// inline dropdown so it doesn't compete for space inside the slot row. Shows a
/// chip row of ship sub-type filters (Shield / Armor / ECM / Rail / Laser / Engine
/// / Reactor / Support) and a list of available modules (researched + diplomacy),
/// grouped by faction color.
/// </summary>
public partial class ModulePickerDialog : GlassOverlay
{
    private ShipDesignerOverlay? _overlay;
    private int _slotIndex;

    private readonly HashSet<TechShipSubType> _activeFilters = new();
    private readonly Dictionary<TechShipSubType, Button> _filterChips = new();
    private VBoxContainer _list = null!;

    public ModulePickerDialog()
    {
        OverlayTitle = "SELECT MODULE";
    }

    public void Configure(ShipDesignerOverlay overlay, int slotIndex)
    {
        _overlay = overlay;
        _slotIndex = slotIndex;
        OverlayTitle = $"SELECT MODULE — S{slotIndex + 1}";
    }

    public override void _Ready()
    {
        base._Ready();
        BuildBody();
    }

    private void BuildBody()
    {
        if (_overlay == null) return;

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 24);
        margin.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 14);
        col.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        margin.AddChild(col);

        col.AddChild(BuildFilterRow());
        col.AddChild(BuildToolRow());
        col.AddChild(BuildList());

        Body.AddChild(margin);
        RebuildList();
    }

    private Control BuildFilterRow()
    {
        var outer = new VBoxContainer();
        outer.AddThemeConstantOverride("separation", 6);

        var header = new Label { Text = "FILTER BY SUB-TYPE" };
        UIFonts.StyleRole(header, UIFonts.Role.UILabel);
        outer.AddChild(header);

        var chips = new HBoxContainer();
        chips.AddThemeConstantOverride("separation", 6);
        outer.AddChild(chips);

        var counts = CountAvailableBySubType();
        foreach (TechShipSubType t in Enum.GetValues<TechShipSubType>())
        {
            int count = counts.GetValueOrDefault(t);
            var chip = new Button
            {
                Text = $"{t.ToString().ToUpperInvariant()} ({count})",
                ToggleMode = true,
                FocusMode = Control.FocusModeEnum.None,
                Disabled = count == 0,
            };
            chip.CustomMinimumSize = new Vector2(96, 28);
            UIFonts.StyleButtonRole(chip, UIFonts.Role.UILabel,
                count == 0 ? UIColors.TextFaint : UIColors.TextBright);
            GlassPanel.StyleButton(chip);
            chip.Toggled += on =>
            {
                if (on) _activeFilters.Add(t);
                else _activeFilters.Remove(t);
                RebuildList();
            };
            _filterChips[t] = chip;
            chips.AddChild(chip);
        }

        return outer;
    }

    // How many available Ship modules exist for each sub-type — drives the chip counts
    // and disables chips the empire has no coverage for.
    private Dictionary<TechShipSubType, int> CountAvailableBySubType()
    {
        var counts = new Dictionary<TechShipSubType, int>();
        var registry = _overlay?.Registry;
        var research = _overlay?.ResearchState;
        if (registry == null || research == null) return counts;

        foreach (var sub in registry.Subsystems)
        {
            if (sub.Type != TechModuleType.Ship) continue;
            if (sub.ShipSubType is not TechShipSubType sst) continue;
            if (!research.IsAvailable(sub.Id)) continue;
            counts[sst] = counts.GetValueOrDefault(sst) + 1;
        }
        return counts;
    }

    private Control BuildToolRow()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var clear = new Button { Text = "CLEAR FILTERS", FocusMode = Control.FocusModeEnum.None };
        clear.CustomMinimumSize = new Vector2(130, 28);
        UIFonts.StyleButtonRole(clear, UIFonts.Role.UILabel);
        GlassPanel.StyleButton(clear);
        clear.Pressed += () =>
        {
            _activeFilters.Clear();
            foreach (var c in _filterChips.Values) c.SetPressedNoSignal(false);
            RebuildList();
        };
        row.AddChild(clear);

        var best = new Button { Text = "BEST AVAILABLE", FocusMode = Control.FocusModeEnum.None };
        best.CustomMinimumSize = new Vector2(140, 28);
        UIFonts.StyleButtonRole(best, UIFonts.Role.UILabel);
        GlassPanel.StyleButton(best);
        best.Pressed += PickBestAvailable;
        row.AddChild(best);

        var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(spacer);

        return row;
    }

    private Control BuildList()
    {
        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;

        _list = new VBoxContainer();
        _list.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _list.AddThemeConstantOverride("separation", 2);
        scroll.AddChild(_list);

        return scroll;
    }

    private void RebuildList()
    {
        foreach (var child in _list.GetChildren()) child.QueueFree();

        var registry = _overlay?.Registry;
        var research = _overlay?.ResearchState;
        if (registry == null || research == null)
        {
            _list.AddChild(MakeInfoLabel("Research unavailable."));
            return;
        }

        var candidates = GatherCandidates(registry, research).ToList();
        if (candidates.Count == 0)
        {
            _list.AddChild(MakeInfoLabel(_activeFilters.Count > 0
                ? "No modules match the current filters."
                : "No modules available yet — research a tier or negotiate a tech deal."));
            return;
        }

        foreach (var group in candidates
            .GroupBy(c => c.sub.Color)
            .OrderBy(g => (int)g.Key))
        {
            _list.AddChild(MakeColorHeader(group.Key));
            foreach (var entry in group
                .OrderBy(c => (int)(c.sub.ShipSubType ?? TechShipSubType.Laser))
                .ThenBy(c => c.sub.Tier)
                .ThenBy(c => c.sub.DisplayName))
                _list.AddChild(BuildRow(entry));
        }
    }

    private IEnumerable<(SubsystemData sub, TechAvailabilitySource source)> GatherCandidates(
        TechTreeRegistry registry, EmpireResearchState research)
    {
        foreach (var sub in registry.Subsystems)
        {
            if (sub.Type != TechModuleType.Ship) continue;
            if (_activeFilters.Count > 0 && (sub.ShipSubType is not TechShipSubType sst || !_activeFilters.Contains(sst)))
                continue;
            var source = research.GetAvailabilitySource(sub.Id);
            if (source.HasValue)
                yield return (sub, source.Value);
        }
    }

    private Control BuildRow((SubsystemData sub, TechAvailabilitySource source) entry)
    {
        var btn = new Button { Flat = false };
        btn.CustomMinimumSize = new Vector2(0, 32);
        btn.FocusMode = Control.FocusModeEnum.None;

        var accent = UIColors.GetFactionGlow(entry.sub.Color);

        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0.35f),
            BorderColor = UIColors.BorderDim,
        };
        normal.SetBorderWidthAll(1);
        normal.SetCornerRadiusAll(0);
        normal.ContentMarginLeft = 10;
        normal.ContentMarginRight = 10;
        normal.ContentMarginTop = 4;
        normal.ContentMarginBottom = 4;

        var hover = new StyleBoxFlat
        {
            BgColor = new Color(accent.R, accent.G, accent.B, 0.14f),
            BorderColor = UIColors.BorderBright,
        };
        hover.SetBorderWidthAll(1);
        hover.SetCornerRadiusAll(0);
        hover.ContentMarginLeft = 10;
        hover.ContentMarginRight = 10;
        hover.ContentMarginTop = 4;
        hover.ContentMarginBottom = 4;

        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeStyleboxOverride("focus", normal);

        btn.Pressed += () =>
        {
            _overlay?.SetSlot(_slotIndex, entry.sub.Id);
            RequestClose();
        };

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        row.MouseFilter = Control.MouseFilterEnum.Ignore;
        row.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        btn.AddChild(row);

        var accentDot = new ColorRect { Color = accent };
        accentDot.CustomMinimumSize = new Vector2(4, 14);
        accentDot.MouseFilter = Control.MouseFilterEnum.Ignore;
        row.AddChild(accentDot);

        var name = new Label { Text = entry.sub.DisplayName.ToUpperInvariant() };
        UIFonts.StyleRole(name, UIFonts.Role.BodyPrimary, UIColors.TextBright);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        name.ClipText = true;
        name.MouseFilter = Control.MouseFilterEnum.Ignore;
        row.AddChild(name);

        if (entry.sub.ShipSubType.HasValue)
        {
            var subType = new Label { Text = entry.sub.ShipSubType.Value.ToString().ToUpperInvariant() };
            UIFonts.StyleRole(subType, UIFonts.Role.DataSmall, UIColors.TextLabel);
            subType.MouseFilter = Control.MouseFilterEnum.Ignore;
            row.AddChild(subType);
        }

        var tier = new Label { Text = $"T{entry.sub.Tier}" };
        UIFonts.StyleRole(tier, UIFonts.Role.DataSmall, accent);
        tier.MouseFilter = Control.MouseFilterEnum.Ignore;
        row.AddChild(tier);

        float bonus = _overlay?.Expertise?.GetSubsystemBonus(entry.sub.Id) ?? 1.0f;
        var mult = new Label { Text = $"{bonus:0.0}×" };
        UIFonts.StyleRole(mult, UIFonts.Role.DataSmall,
            bonus > 1.05f ? UIColors.DeltaPos : UIColors.TextDim);
        mult.MouseFilter = Control.MouseFilterEnum.Ignore;
        row.AddChild(mult);

        var sourceLabel = new Label { Text = SourceToBadge(entry.source) };
        UIFonts.Style(sourceLabel, UIFonts.Main, UIFonts.SmallSize, SourceToColor(entry.source));
        sourceLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        row.AddChild(sourceLabel);

        return btn;
    }

    private void PickBestAvailable()
    {
        var registry = _overlay?.Registry;
        var research = _overlay?.ResearchState;
        if (registry == null || research == null || _overlay == null) return;

        var affinity = _overlay.EmpireAffinity;
        SubsystemData? best = null;
        int bestScore = -1;

        foreach (var sub in registry.Subsystems)
        {
            if (sub.Type != TechModuleType.Ship) continue;
            if (!research.IsAvailable(sub.Id)) continue;
            if (_activeFilters.Count > 0 && (sub.ShipSubType is not TechShipSubType sst || !_activeFilters.Contains(sst)))
                continue;
            int score = sub.Tier * 2 + (affinity.HasValue && sub.Color == affinity.Value ? 1 : 0);
            if (score > bestScore) { bestScore = score; best = sub; }
        }
        if (best != null)
        {
            _overlay.SetSlot(_slotIndex, best.Id);
            RequestClose();
        }
    }

    private static Control MakeColorHeader(PrecursorColor color)
    {
        var header = new Label { Text = color.ToString().ToUpperInvariant() };
        UIFonts.StyleRole(header, UIFonts.Role.UILabel, UIColors.GetFactionGlow(color));
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_bottom", 2);
        margin.AddChild(header);
        return margin;
    }

    private static Control MakeInfoLabel(string text)
    {
        var lbl = new Label { Text = text };
        UIFonts.StyleRole(lbl, UIFonts.Role.BodySecondary);
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        margin.AddChild(lbl);
        return margin;
    }

    private static string SourceToBadge(TechAvailabilitySource s) => s switch
    {
        TechAvailabilitySource.Research => "RESEARCH",
        TechAvailabilitySource.Diplomacy => "DIPLOMACY",
        _ => "",
    };

    private static Color SourceToColor(TechAvailabilitySource s) => s switch
    {
        TechAvailabilitySource.Research => UIColors.DeltaPos,
        TechAvailabilitySource.Diplomacy => UIColors.TextLabel,
        _ => UIColors.TextDim,
    };
}
