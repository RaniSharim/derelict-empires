using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Nodes.UI.ShipDesigner;

/// <summary>
/// Inline dropdown (child of a SlotMatrix row) listing candidate subsystems for one slot.
/// Rows grouped by faction color; each row shows [name · tier · expertise multiplier · status].
/// Locked rows dim by default; [SHOW LOCKED] toggle reveals them.
/// </summary>
public partial class SlotPickerDropdown : PanelContainer
{
    private readonly ShipDesignerOverlay _overlay;
    public int SlotIndex { get; }

    private bool _showLocked;
    private VBoxContainer _list = null!;

    public SlotPickerDropdown(ShipDesignerOverlay overlay, int slotIndex)
    {
        _overlay = overlay;
        SlotIndex = slotIndex;
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, 240);

        var bg = new StyleBoxFlat
        {
            BgColor = new Color(4 / 255f, 6 / 255f, 14 / 255f, 0.98f),
            BorderColor = UIColors.BorderMid,
        };
        bg.SetBorderWidthAll(1);
        bg.BorderWidthTop = 0;
        bg.SetCornerRadiusAll(0);
        bg.ContentMarginLeft = 8;
        bg.ContentMarginRight = 8;
        bg.ContentMarginTop = 8;
        bg.ContentMarginBottom = 8;
        AddThemeStyleboxOverride("panel", bg);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 6);
        AddChild(col);

        // Header row: [BEST AVAILABLE] · [SHOW LOCKED]
        var toolRow = new HBoxContainer();
        toolRow.AddThemeConstantOverride("separation", 6);
        col.AddChild(toolRow);

        var best = new Button { Text = "BEST AVAILABLE" };
        best.CustomMinimumSize = new Vector2(0, 26);
        UIFonts.StyleButtonRole(best, UIFonts.Role.UILabel);
        GlassPanel.StyleButton(best);
        best.Pressed += PickBestAvailable;
        toolRow.AddChild(best);

        var showLocked = new Button { Text = _showLocked ? "HIDE LOCKED" : "SHOW LOCKED", ToggleMode = true };
        showLocked.ButtonPressed = _showLocked;
        showLocked.CustomMinimumSize = new Vector2(0, 26);
        UIFonts.StyleButtonRole(showLocked, UIFonts.Role.UILabel);
        GlassPanel.StyleButton(showLocked);
        showLocked.Toggled += on =>
        {
            _showLocked = on;
            showLocked.Text = on ? "HIDE LOCKED" : "SHOW LOCKED";
            RebuildList();
        };
        toolRow.AddChild(showLocked);

        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        col.AddChild(scroll);

        _list = new VBoxContainer();
        _list.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _list.AddThemeConstantOverride("separation", 2);
        scroll.AddChild(_list);

        RebuildList();
    }

    private void RebuildList()
    {
        foreach (var child in _list.GetChildren()) child.QueueFree();

        var registry = _overlay.Registry;
        var research = _overlay.ResearchState;
        if (registry == null || research == null)
        {
            _list.AddChild(MakeInfoLabel("Research unavailable."));
            return;
        }

        var candidates = GatherCandidates(registry, research).ToList();
        if (candidates.Count == 0)
        {
            _list.AddChild(MakeInfoLabel("No options yet — research more tiers."));
            return;
        }

        // Group by faction color
        foreach (var group in candidates
            .GroupBy(c => c.sub.Color)
            .OrderBy(g => (int)g.Key))
        {
            _list.AddChild(MakeColorHeader(group.Key));
            foreach (var entry in group.OrderBy(c => c.sub.Tier).ThenBy(c => c.sub.DisplayName))
                _list.AddChild(BuildRow(entry));
        }
    }

    private IEnumerable<(SubsystemData sub, State state)> GatherCandidates(
        TechTreeRegistry registry, EmpireResearchState research)
    {
        foreach (var id in research.ResearchedSubsystems)
        {
            var sub = registry.GetSubsystem(id);
            if (sub != null) yield return (sub, State.Researched);
        }
        foreach (var id in research.AvailableSubsystems)
        {
            bool isInProgress = research.CurrentProject == id;
            var sub = registry.GetSubsystem(id);
            if (sub != null) yield return (sub, isInProgress ? State.InProgress : State.Available);
        }
        if (_showLocked)
        {
            foreach (var id in research.LockedSubsystems)
            {
                var sub = registry.GetSubsystem(id);
                if (sub != null) yield return (sub, State.Locked);
            }
        }
    }

    private Control BuildRow((SubsystemData sub, State state) entry)
    {
        var btn = new Button { Flat = false };
        btn.CustomMinimumSize = new Vector2(0, 28);
        btn.FocusMode = Control.FocusModeEnum.None;

        bool selectable = entry.state == State.Researched;
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

        btn.Disabled = !selectable;
        btn.Pressed += () => _overlay.SetSlot(SlotIndex, entry.sub.Id);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        row.MouseFilter = Control.MouseFilterEnum.Ignore;
        row.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        btn.AddChild(row);

        var accentDot = new ColorRect { Color = accent };
        accentDot.CustomMinimumSize = new Vector2(4, 12);
        accentDot.MouseFilter = Control.MouseFilterEnum.Ignore;
        row.AddChild(accentDot);

        var name = new Label { Text = entry.sub.DisplayName.ToUpperInvariant() };
        UIFonts.StyleRole(name, UIFonts.Role.BodyPrimary,
            selectable ? UIColors.TextBright : UIColors.TextFaint);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        name.ClipText = true;
        name.MouseFilter = Control.MouseFilterEnum.Ignore;
        row.AddChild(name);

        var tier = new Label { Text = $"T{entry.sub.Tier}" };
        UIFonts.StyleRole(tier, UIFonts.Role.DataSmall, accent);
        tier.MouseFilter = Control.MouseFilterEnum.Ignore;
        row.AddChild(tier);

        float bonus = _overlay.Expertise?.GetSubsystemBonus(entry.sub.Id) ?? 1.0f;
        var mult = new Label { Text = $"{bonus:0.0}×" };
        UIFonts.StyleRole(mult, UIFonts.Role.DataSmall,
            bonus > 1.05f ? UIColors.DeltaPos : UIColors.TextDim);
        mult.MouseFilter = Control.MouseFilterEnum.Ignore;
        row.AddChild(mult);

        var statusLabel = new Label { Text = StateToBadge(entry.state) };
        UIFonts.Style(statusLabel, UIFonts.ShareTechMonoTracked, 9, StateToColor(entry.state));
        statusLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        row.AddChild(statusLabel);

        return btn;
    }

    private void PickBestAvailable()
    {
        var registry = _overlay.Registry;
        var research = _overlay.ResearchState;
        if (registry == null || research == null) return;

        // Prefer empire affinity, else highest-tier researched subsystem.
        var affinity = _overlay.EmpireAffinity;
        SubsystemData? best = null;
        int bestScore = -1;

        foreach (var id in research.ResearchedSubsystems)
        {
            var sub = registry.GetSubsystem(id);
            if (sub == null) continue;
            int score = sub.Tier * 2 + (affinity.HasValue && sub.Color == affinity.Value ? 1 : 0);
            if (score > bestScore) { bestScore = score; best = sub; }
        }
        if (best != null) _overlay.SetSlot(SlotIndex, best.Id);
    }

    private static Control MakeColorHeader(PrecursorColor color)
    {
        var header = new Label { Text = color.ToString().ToUpperInvariant() };
        UIFonts.StyleRole(header, UIFonts.Role.UILabel, UIColors.GetFactionGlow(color));
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 4);
        margin.AddThemeConstantOverride("margin_bottom", 2);
        margin.AddChild(header);
        return margin;
    }

    private static Control MakeInfoLabel(string text)
    {
        var lbl = new Label { Text = text };
        UIFonts.StyleRole(lbl, UIFonts.Role.BodySecondary);
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        margin.AddChild(lbl);
        return margin;
    }

    private enum State { Researched, InProgress, Available, Locked }

    private static string StateToBadge(State s) => s switch
    {
        State.Researched => "READY",
        State.InProgress => "\u29d7 IN PROGRESS",
        State.Available => "RESEARCH",
        State.Locked => "LOCKED",
        _ => ""
    };

    private static Color StateToColor(State s) => s switch
    {
        State.Researched => UIColors.DeltaPos,
        State.InProgress => UIColors.Moving,
        State.Available => UIColors.TextLabel,
        State.Locked => UIColors.TextFaint,
        _ => UIColors.TextDim,
    };
}
