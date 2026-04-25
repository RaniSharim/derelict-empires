using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Services;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Full-screen modal Tech Tree overlay. Color tabs, 5×6 tier matrix, focus panel.
/// Open via EventBus.TechTreeOpenRequested or hotkey T. Honors research_ui_spec.md §5.
/// Single-track (subsystem) research for now — the spec's two-track (TIER + MOD) model
/// requires Core changes deferred to a follow-up pass.
/// </summary>
public partial class TechTreeOverlay : GlassOverlay
{
    private IGameQuery? _query;
    private PrecursorColor _activeColor = PrecursorColor.Red;
    private TechNodeData? _selectedNode;
    private SubsystemData? _selectedSubsystem;

    // Cached UI nodes (rebuilt on color change)
    private HBoxContainer _colorTabBar = null!;
    private GridContainer _tierMatrix = null!;
    private VBoxContainer _focusPanel = null!;
    private readonly Dictionary<PrecursorColor, Button> _colorTabs = new();
    private readonly List<TierMatrixCell> _matrixCells = new();

    public TechTreeOverlay()
    {
        OverlayTitle = "TECH TREE";
    }

    public void Configure(IGameQuery query, PrecursorColor initialColor)
    {
        _query = query;
        _activeColor = initialColor;
    }

    public override void _Ready()
    {
        base._Ready();
        BuildLayout();
        AutoSelectActiveTier();
        RebuildMatrix();
    }

    /// <summary>
    /// When the overlay opens, pre-select the tier the player is currently researching
    /// (or whose subsystem is being researched) so the focus panel is already populated.
    /// </summary>
    private void AutoSelectActiveTier()
    {
        var state = _query?.PlayerResearchState;
        var registry = _query?.TechRegistry;
        if (state == null || registry == null) return;

        TechNodeData? target = null;

        if (!string.IsNullOrEmpty(state.CurrentTierProject))
            target = registry.GetNode(state.CurrentTierProject);

        if (target == null && !string.IsNullOrEmpty(state.CurrentProject))
        {
            var sub = registry.GetSubsystem(state.CurrentProject);
            if (sub != null)
                target = registry.GetNode(sub.Color, sub.Category, sub.Tier);
        }

        // Fallback: first unlocked tier in the active color.
        if (target == null)
        {
            foreach (var category in System.Enum.GetValues<TechCategory>())
            {
                int t = state.GetUnlockedTier(_activeColor, category);
                if (t >= 1)
                {
                    target = registry.GetNode(_activeColor, category, t);
                    if (target != null) break;
                }
            }
        }

        if (target != null)
        {
            _activeColor = target.Color;
            _selectedNode = target;
        }
    }

    private void BuildLayout()
    {
        Body.AddChild(BuildColorTabBar());
        Body.AddChild(BuildSeparator(1));
        var body = BuildBody();
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        Body.AddChild(body);
    }

    private Control BuildColorTabBar()
    {
        var bar = new PanelContainer { Name = "ColorTabBar" };
        bar.CustomMinimumSize = new Vector2(0, 44);
        var style = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0) };
        style.SetBorderWidthAll(0);
        bar.AddThemeStyleboxOverride("panel", style);

        _colorTabBar = new HBoxContainer();
        _colorTabBar.AddThemeConstantOverride("separation", 0);
        bar.AddChild(_colorTabBar);

        foreach (var color in Enum.GetValues<PrecursorColor>())
        {
            var tab = new Button { Text = color.ToString().ToUpperInvariant() };
            tab.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            tab.CustomMinimumSize = new Vector2(0, 44);
            tab.FocusMode = Control.FocusModeEnum.None;
            // Title Medium — Exo 2 SemiBold at 13px, faction color.
            UIFonts.StyleButton(tab, UIFonts.Exo2SemiBold, 13, UIColors.GetFactionGlow(color));

            var c = color;
            tab.Pressed += () => SetActiveColor(c);
            StyleColorTab(tab, color, isActive: color == _activeColor);

            _colorTabBar.AddChild(tab);
            _colorTabs[color] = tab;
        }

        return bar;
    }

    private static void StyleColorTab(Button tab, PrecursorColor color, bool isActive)
    {
        var glow = UIColors.GetFactionGlow(color);
        var normal = new StyleBoxFlat
        {
            BgColor = isActive ? new Color(glow.R, glow.G, glow.B, 0.14f) : new Color(0, 0, 0, 0),
            BorderColor = glow,
        };
        normal.SetBorderWidthAll(0);
        if (isActive)
            normal.BorderWidthBottom = 2;
        normal.SetCornerRadiusAll(0);

        var hover = new StyleBoxFlat
        {
            BgColor = new Color(glow.R, glow.G, glow.B, 0.08f),
            BorderColor = glow,
        };
        hover.SetBorderWidthAll(0);
        hover.BorderWidthBottom = 2;
        hover.SetCornerRadiusAll(0);

        tab.AddThemeStyleboxOverride("normal", normal);
        tab.AddThemeStyleboxOverride("hover", hover);
        tab.AddThemeStyleboxOverride("pressed", normal);
        tab.AddThemeStyleboxOverride("focus", normal);
        tab.AddThemeColorOverride("font_color", isActive ? glow : new Color(glow.R, glow.G, glow.B, 0.6f));
    }

    private Control BuildSeparator(int height)
    {
        var sep = new ColorRect { Color = UIColors.BorderDim };
        sep.CustomMinimumSize = new Vector2(0, height);
        return sep;
    }

    private Control BuildLegend()
    {
        var legend = new HBoxContainer();
        legend.AddThemeConstantOverride("separation", 16);

        AddLegendItem(legend, "·", UIColors.TextFaint, "LOCKED");
        AddLegendItem(legend, "○", UIColors.TextLabel, "AVAILABLE");
        AddLegendItem(legend, "▶", UIColors.Accent, "IN PROGRESS");
        AddLegendItem(legend, "✓", UIColors.DeltaPos, "UNLOCKED");
        AddLegendItem(legend, "⧗", UIColors.Moving, "QUEUED");

        return legend;
    }

    private static void AddLegendItem(HBoxContainer host, string symbol, Color color, string label)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);

        var sym = new Label { Text = symbol };
        UIFonts.Style(sym, UIFonts.Main, UIFonts.NormalSize, color);
        sym.CustomMinimumSize = new Vector2(18, 0);
        sym.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(sym);

        var text = new Label { Text = label };
        UIFonts.StyleRole(text, UIFonts.Role.UILabel);
        text.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(text);

        host.AddChild(row);
    }

    private Control BuildBody()
    {
        var body = new HBoxContainer();
        body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.AddThemeConstantOverride("separation", 24);

        // Padding
        var padL = new Control { CustomMinimumSize = new Vector2(24, 0) };
        body.AddChild(padL);

        // Matrix section (60%)
        var matrixSection = new VBoxContainer();
        matrixSection.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        matrixSection.SizeFlagsStretchRatio = 1.5f;
        matrixSection.AddThemeConstantOverride("separation", 12);

        var matrixHeader = new Label { Text = "TIER MATRIX — 1 ROW PER CATEGORY, 1 COLUMN PER TIER" };
        UIFonts.StyleRole(matrixHeader, UIFonts.Role.UILabel);
        matrixSection.AddChild(matrixHeader);

        _tierMatrix = new GridContainer();
        _tierMatrix.Columns = 7; // 1 label column + 6 tiers
        _tierMatrix.AddThemeConstantOverride("h_separation", 6);
        _tierMatrix.AddThemeConstantOverride("v_separation", 6);
        matrixSection.AddChild(_tierMatrix);

        matrixSection.AddChild(BuildLegend());

        body.AddChild(matrixSection);

        // Focus panel (40%)
        var focusContainer = new PanelContainer();
        focusContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        focusContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
        focusContainer.CustomMinimumSize = new Vector2(320, 0);
        var focusStyle = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0.25f),
            BorderColor = UIColors.BorderDim,
        };
        focusStyle.SetBorderWidthAll(1);
        focusStyle.SetCornerRadiusAll(0);
        focusContainer.AddThemeStyleboxOverride("panel", focusStyle);

        var focusMargin = new MarginContainer();
        focusMargin.AddThemeConstantOverride("margin_left", 16);
        focusMargin.AddThemeConstantOverride("margin_right", 16);
        focusMargin.AddThemeConstantOverride("margin_top", 16);
        focusMargin.AddThemeConstantOverride("margin_bottom", 16);
        focusContainer.AddChild(focusMargin);

        _focusPanel = new VBoxContainer();
        _focusPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _focusPanel.AddThemeConstantOverride("separation", 8);
        focusMargin.AddChild(_focusPanel);

        body.AddChild(focusContainer);

        var padR = new Control { CustomMinimumSize = new Vector2(24, 0) };
        body.AddChild(padR);

        return body;
    }

    private void SetActiveColor(PrecursorColor color)
    {
        _activeColor = color;
        foreach (var (c, tab) in _colorTabs)
            StyleColorTab(tab, c, c == color);
        _selectedNode = null;
        _selectedSubsystem = null;
        RebuildMatrix();
        BuildFocusPanel();
    }

    private void RebuildMatrix()
    {
        foreach (var child in _tierMatrix.GetChildren())
            child.QueueFree();
        _matrixCells.Clear();

        if (_query?.TechRegistry == null) return;

        var registry = _query.TechRegistry;
        var state = _query.PlayerResearchState;

        // Header row: blank corner + tier numbers
        var corner = new Control { CustomMinimumSize = new Vector2(110, 24) };
        _tierMatrix.AddChild(corner);
        for (int tier = 1; tier <= 6; tier++)
        {
            var lbl = new Label { Text = $"T{tier}" };
            // Mono at its 10px floor, tracked for ALL-CAPS label feel.
            UIFonts.StyleRole(lbl, UIFonts.Role.DataSmall);
            lbl.HorizontalAlignment = HorizontalAlignment.Center;
            lbl.CustomMinimumSize = new Vector2(80, 24);
            _tierMatrix.AddChild(lbl);
        }

        // Category rows
        foreach (var category in Enum.GetValues<TechCategory>())
        {
            var rowLabel = new Label { Text = FormatCategory(category) };
            // Title Medium — Exo 2 SemiBold at 13px for category names.
            UIFonts.StyleRole(rowLabel, UIFonts.Role.TitleMedium, UIColors.TextLabel);
            rowLabel.HorizontalAlignment = HorizontalAlignment.Right;
            rowLabel.VerticalAlignment = VerticalAlignment.Center;
            rowLabel.CustomMinimumSize = new Vector2(110, 80);
            _tierMatrix.AddChild(rowLabel);

            for (int tier = 1; tier <= 6; tier++)
            {
                var node = registry.GetNode(_activeColor, category, tier);
                var cell = new TierMatrixCell();
                cell.CustomMinimumSize = new Vector2(80, 80);
                if (node != null)
                {
                    bool isSelected = _selectedNode != null && _selectedNode.Id == node.Id;
                    cell.Configure(node, ComputeCellState(node, state), _activeColor, isSelected);
                    cell.Pressed += () => OnCellClicked(node);
                }
                _tierMatrix.AddChild(cell);
                _matrixCells.Add(cell);
            }
        }

        BuildFocusPanel();
    }

    private static string FormatCategory(TechCategory cat) => cat switch
    {
        TechCategory.WeaponsEnergyPropulsion => "WEAPONS",
        TechCategory.ComputingSensors => "SENSORS",
        TechCategory.IndustryMining => "INDUSTRY",
        TechCategory.AdminLogistics => "LOGISTICS",
        TechCategory.Special => "SPECIAL",
        _ => cat.ToString().ToUpperInvariant()
    };

    private static TierMatrixCell.CellState ComputeCellState(TechNodeData node, EmpireResearchState? state)
    {
        if (state == null) return TierMatrixCell.CellState.Locked;

        int unlocked = state.GetUnlockedTier(node.Color, node.Category);

        // Already unlocked (either via tier research completion or side-effect from subsystems).
        if (node.Tier <= unlocked) return TierMatrixCell.CellState.Completed;

        // Predecessor not unlocked yet.
        if (node.Tier > unlocked + 1) return TierMatrixCell.CellState.Locked;

        // node.Tier == unlocked + 1 — candidate for active/queued/available.
        if (state.CurrentTierProject == node.Id) return TierMatrixCell.CellState.Active;
        if (state.TierQueue.Contains(node.Id)) return TierMatrixCell.CellState.Queued;
        return TierMatrixCell.CellState.Available;
    }

    private void OnCellClicked(TechNodeData node)
    {
        _selectedNode = node;
        _selectedSubsystem = null;
        // Rebuild cells so the selected one gets the highlight ring, and refresh focus.
        RebuildMatrix();
    }

    private void OnSubsystemClicked(SubsystemData subsystem)
    {
        _selectedSubsystem = subsystem;
        BuildFocusPanel();
    }

    private void BuildFocusPanel()
    {
        foreach (var child in _focusPanel.GetChildren())
            child.QueueFree();

        if (_selectedSubsystem != null)
        {
            BuildSubsystemFocus(_selectedSubsystem);
            return;
        }
        if (_selectedNode != null)
        {
            BuildTierFocus(_selectedNode);
            return;
        }

        var hint = new Label { Text = "Select a tier in the matrix to view its modules." };
        UIFonts.StyleRole(hint, UIFonts.Role.BodyPrimary, UIColors.TextDim);
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _focusPanel.AddChild(hint);
    }

    private void BuildTierFocus(TechNodeData node)
    {
        var state = _query?.PlayerResearchState;
        var registry = _query?.TechRegistry;
        if (state == null || registry == null) return;

        var glow = UIColors.GetFactionGlow(node.Color);
        var cellState = ComputeCellState(node, state);

        var title = new Label { Text = $"{node.Color.ToString().ToUpperInvariant()} {FormatCategory(node.Category)} T{node.Tier}" };
        UIFonts.StyleRole(title, UIFonts.Role.TitleLarge, glow);
        _focusPanel.AddChild(title);

        _focusPanel.AddChild(BuildSeparator(1));

        var desc = new Label { Text = $"Tier {node.Tier} {FormatCategory(node.Category).ToLowerInvariant()} technology from the {node.Color} precursors." };
        UIFonts.StyleRole(desc, UIFonts.Role.BodyPrimary);
        desc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _focusPanel.AddChild(desc);

        // Tier status + progress
        string stateLabel = cellState switch
        {
            TierMatrixCell.CellState.Completed => "UNLOCKED",
            TierMatrixCell.CellState.Active => $"IN PROGRESS · {state.CurrentTierProgress:F0}/{node.ResearchCost:F0} RP",
            TierMatrixCell.CellState.Queued => "QUEUED",
            TierMatrixCell.CellState.Available => "AVAILABLE",
            _ => "LOCKED — UNLOCK PREDECESSOR FIRST",
        };
        var statusRow = new Label { Text = stateLabel };
        UIFonts.StyleRole(statusRow, UIFonts.Role.StatusBadge,
            cellState == TierMatrixCell.CellState.Locked ? UIColors.TextDim : glow);
        _focusPanel.AddChild(statusRow);

        // Modules block — only meaningful if tier is unlocked (modules rolled).
        var modulesHeader = new Label { Text = "MODULES (2 OF 3 ROLLED AT UNLOCK)" };
        UIFonts.StyleRole(modulesHeader, UIFonts.Role.UILabel);
        _focusPanel.AddChild(modulesHeader);

        bool tierIsUnlocked = cellState == TierMatrixCell.CellState.Completed;

        foreach (var subId in node.SubsystemIds)
        {
            var sub = registry.GetSubsystem(subId);
            if (sub == null) continue;

            string tag;
            Color tagColor;
            bool clickable = false;
            if (!tierIsUnlocked)
            {
                tag = "SILHOUETTED";
                tagColor = UIColors.TextFaint;
            }
            else if (state.ResearchedSubsystems.Contains(subId)) { tag = "RESEARCHED"; tagColor = UIColors.DeltaPos; clickable = true; }
            else if (state.CurrentProject == subId) { tag = "ACTIVE"; tagColor = UIColors.Moving; clickable = true; }
            else if (state.AvailableSubsystems.Contains(subId)) { tag = "AVAILABLE"; tagColor = glow; clickable = true; }
            else if (state.LockedSubsystems.Contains(subId)) { tag = "LOCKED"; tagColor = UIColors.TextFaint; clickable = true; }
            else { tag = "SILHOUETTED"; tagColor = UIColors.TextFaint; }

            var row = new Button();
            row.CustomMinimumSize = new Vector2(0, 36);
            row.FocusMode = Control.FocusModeEnum.None;
            row.ClipText = true;
            row.Alignment = HorizontalAlignment.Left;
            row.Text = $"  {sub.DisplayName}     [{tag}]";
            row.Disabled = !clickable;
            // Title Medium (Exo 2 SemiBold 13px) keeps names above the 11px Exo 2 floor.
            UIFonts.StyleButtonRole(row, UIFonts.Role.TitleMedium, tagColor);
            GlassPanel.StyleButton(row);
            if (clickable)
                row.Pressed += () => OnSubsystemClicked(sub);
            _focusPanel.AddChild(row);
        }

        _focusPanel.AddChild(BuildTierActionButton(node, state, cellState));
    }

    private Button BuildTierActionButton(TechNodeData node, EmpireResearchState state, TierMatrixCell.CellState cellState)
    {
        var actionBtn = new Button();
        actionBtn.CustomMinimumSize = new Vector2(0, 44);

        bool enabled = false;
        string label;

        switch (cellState)
        {
            case TierMatrixCell.CellState.Available:
                label = "START TIER RESEARCH";
                enabled = true;
                break;
            case TierMatrixCell.CellState.Active:
                label = "RESEARCH IN PROGRESS";
                enabled = false;
                break;
            case TierMatrixCell.CellState.Queued:
                label = "QUEUED — REMOVE FROM QUEUE";
                enabled = true;
                break;
            case TierMatrixCell.CellState.Completed:
                label = "SELECT A MODULE ABOVE";
                enabled = false;
                break;
            case TierMatrixCell.CellState.Locked:
            default:
                label = "LOCKED";
                enabled = false;
                break;
        }

        actionBtn.Text = label;
        actionBtn.Disabled = !enabled;
        UIFonts.StyleButtonRole(actionBtn, UIFonts.Role.StatusBadge,
            enabled ? UIColors.Accent : UIColors.TextDim);
        GlassPanel.StyleButton(actionBtn, primary: enabled);

        actionBtn.Pressed += () =>
        {
            if (cellState == TierMatrixCell.CellState.Available)
            {
                state.CurrentTierProject = node.Id;
                state.CurrentTierProgress = 0f;
                EventBus.Instance?.FireResearchStarted(state.EmpireId, node.Id);
                RequestClose();
            }
            else if (cellState == TierMatrixCell.CellState.Queued)
            {
                state.TierQueue.Remove(node.Id);
                RebuildMatrix();
            }
        };

        return actionBtn;
    }

    private void BuildSubsystemFocus(SubsystemData sub)
    {
        var state = _query?.PlayerResearchState;
        if (state == null) return;

        var glow = UIColors.GetFactionGlow(sub.Color);

        var title = new Label { Text = sub.DisplayName.ToUpperInvariant() };
        UIFonts.StyleRole(title, UIFonts.Role.TitleLarge, glow);
        _focusPanel.AddChild(title);

        _focusPanel.AddChild(BuildSeparator(1));

        var desc = new Label { Text = sub.Description };
        UIFonts.StyleRole(desc, UIFonts.Role.BodyPrimary);
        desc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _focusPanel.AddChild(desc);

        var statsHeader = new Label { Text = "STATS" };
        UIFonts.StyleRole(statsHeader, UIFonts.Role.UILabel);
        _focusPanel.AddChild(statsHeader);

        AddStatRow("Color", sub.Color.ToString());
        AddStatRow("Category", FormatCategory(sub.Category));
        AddStatRow("Tier", sub.Tier.ToString());
        AddStatRow("Research cost", sub.ResearchCost.ToString("F0"));

        // Status
        string status;
        Color statusColor;
        bool canStart = false;
        if (state.ResearchedSubsystems.Contains(sub.Id)) { status = "RESEARCHED"; statusColor = UIColors.DeltaPos; }
        else if (state.CurrentProject == sub.Id) { status = $"ACTIVE · {state.CurrentProgress:F0}/{sub.ResearchCost:F0} RP"; statusColor = UIColors.Moving; }
        else if (state.AvailableSubsystems.Contains(sub.Id)) { status = "AVAILABLE"; statusColor = glow; canStart = true; }
        else if (state.LockedSubsystems.Contains(sub.Id)) { status = "LOCKED — SALVAGE / TRADE"; statusColor = UIColors.TextDim; }
        else { status = "SILHOUETTED — UNLOCK TIER FIRST"; statusColor = UIColors.TextDim; }

        AddStatRow("Status", status, valueColor: statusColor);

        var actionBtn = new Button
        {
            Text = canStart ? "START RESEARCH" : "NOT AVAILABLE",
            Disabled = !canStart,
        };
        actionBtn.CustomMinimumSize = new Vector2(0, 44);
        UIFonts.StyleButtonRole(actionBtn, UIFonts.Role.StatusBadge,
            canStart ? UIColors.Accent : UIColors.TextDim);
        GlassPanel.StyleButton(actionBtn, primary: canStart);
        actionBtn.Pressed += () => StartResearch(sub);
        _focusPanel.AddChild(actionBtn);

        // Phase E extension — if the module is researched, show design + combat usage.
        if (state.ResearchedSubsystems.Contains(sub.Id))
        {
            BuildUsedInDesignsBlock(sub);
            BuildCombatPerformanceBlock(sub);
        }
    }

    private void BuildUsedInDesignsBlock(SubsystemData sub)
    {
        var player = GameManager.Instance?.LocalPlayerEmpire;
        if (player == null) return;

        var designs = player.DesignState.Designs
            .Where(d => d.SlotFills.Contains(sub.Id))
            .ToList();

        var header = new Label { Text = designs.Count > 0 ? $"USED IN {designs.Count} DESIGN(S)" : "USED IN 0 DESIGNS" };
        UIFonts.StyleRole(header, UIFonts.Role.UILabel);
        _focusPanel.AddChild(header);

        if (designs.Count == 0)
        {
            var hint = new Label { Text = "Not yet used \u2014 author a design with this module." };
            UIFonts.StyleRole(hint, UIFonts.Role.Small);
            _focusPanel.AddChild(hint);
            return;
        }

        foreach (var design in designs)
        {
            string designId = design.Id;
            var chip = DeepLinkChip.Create(
                design.Name.ToUpperInvariant(),
                UIColors.Accent,
                () =>
                {
                    EventBus.Instance?.FireDesignerOpenRequested(new DesignerOpenRequest
                    {
                        DesignId = designId,
                    });
                    RequestClose();
                });
            _focusPanel.AddChild(chip);
        }
    }

    private void BuildCombatPerformanceBlock(SubsystemData sub)
    {
        var header = new Label { Text = "COMBAT PERFORMANCE" };
        UIFonts.StyleRole(header, UIFonts.Role.UILabel);
        _focusPanel.AddChild(header);

        // No battle-history store yet — stub until Phase G wires persistent combat logs.
        var note = new Label { Text = "No battles recorded for this module yet." };
        UIFonts.StyleRole(note, UIFonts.Role.Small);
        _focusPanel.AddChild(note);
    }

    private void AddStatRow(string label, string value, Color? valueColor = null)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var lbl = new Label { Text = label };
        UIFonts.StyleRole(lbl, UIFonts.Role.BodyPrimary, UIColors.TextDim);
        lbl.CustomMinimumSize = new Vector2(120, 0);
        row.AddChild(lbl);

        var val = new Label { Text = value };
        UIFonts.StyleRole(val, UIFonts.Role.DataLarge, valueColor ?? UIColors.TextLabel);
        val.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(val);

        _focusPanel.AddChild(row);
    }

    private void StartResearch(SubsystemData sub)
    {
        var state = _query?.PlayerResearchState;
        if (state == null) return;

        state.CurrentProject = sub.Id;
        state.CurrentProgress = 0f;

        var empire = GameManager.Instance?.LocalPlayerEmpire;
        if (empire != null)
            EventBus.Instance?.FireResearchStarted(empire.Id, sub.Id);

        // Snap focus to matching active block and close overlay
        RequestClose();
    }
}
