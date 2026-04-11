using Godot;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Left side panel with tabs: FLEETS / COLONIES / RESEARCH / BUILD.
/// 270px wide, anchored top=68 left=0 bottom=0.
/// </summary>
public partial class LeftPanel : Control
{
    private VBoxContainer _listContainer = null!;
    private readonly List<Button> _tabButtons = new();
    private int _activeTab;

    // Data references
    private List<FleetData> _fleets = new();
    private List<ShipInstanceData> _ships = new();

    private static readonly string[] TabNames = { "FLEETS", "COLONIES", "RESEARCH", "BUILD" };

    public override void _Ready()
    {
        // Anchors: left side, below topbar, full height
        AnchorLeft = 0;
        AnchorRight = 0;
        AnchorTop = 0;
        AnchorBottom = 1;
        OffsetLeft = 0;
        OffsetRight = 270;
        OffsetTop = 68;
        OffsetBottom = 0;
        ClipContents = true;
        ZIndex = 50;

        // Background
        var bg = new Panel { Name = "Bg" };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = UIColors.GlassDarkFlat;
        bgStyle.SetBorderWidthAll(0);
        bgStyle.BorderWidthRight = 1;
        bgStyle.BorderColor = UIColors.BorderBright;
        bgStyle.SetCornerRadiusAll(0);
        bg.AddThemeStyleboxOverride("panel", bgStyle);
        AddChild(bg);

        // Main layout
        var layout = new VBoxContainer { Name = "Layout" };
        layout.SetAnchorsPreset(LayoutPreset.FullRect);
        layout.AddThemeConstantOverride("separation", 0);
        AddChild(layout);

        // Tab bar
        BuildTabBar(layout);

        // Sub-header
        var subHeader = new MarginContainer();
        subHeader.AddThemeConstantOverride("margin_left", 14);
        subHeader.AddThemeConstantOverride("margin_right", 14);
        subHeader.AddThemeConstantOverride("margin_top", 4);
        subHeader.AddThemeConstantOverride("margin_bottom", 4);
        layout.AddChild(subHeader);

        var subLabel = new Label { Text = "FLEETS" };
        UIFonts.Style(subLabel, UIFonts.BarlowRegular, 8, UIColors.TextFaint);
        subHeader.AddChild(subLabel);

        // Divider
        var div = new ColorRect();
        div.CustomMinimumSize = new Vector2(0, 1);
        div.Color = UIColors.BorderDim;
        layout.AddChild(div);

        // Scrollable list area
        var scroll = new ScrollContainer { Name = "Scroll" };
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        layout.AddChild(scroll);

        _listContainer = new VBoxContainer { Name = "ListContainer" };
        _listContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _listContainer.AddThemeConstantOverride("separation", 0);
        scroll.AddChild(_listContainer);

        // Subscribe to fleet selection
        if (EventBus.Instance != null)
        {
            EventBus.Instance.FleetSelected += OnFleetSelected;
            EventBus.Instance.FleetDeselected += OnFleetDeselected;
        }
    }

    private void BuildTabBar(VBoxContainer parent)
    {
        var tabRow = new HBoxContainer();
        tabRow.CustomMinimumSize = new Vector2(0, 44);
        tabRow.AddThemeConstantOverride("separation", 0);
        parent.AddChild(tabRow);

        for (int i = 0; i < TabNames.Length; i++)
        {
            var tab = new Button { Text = TabNames[i] };
            tab.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            tab.CustomMinimumSize = new Vector2(0, 44);
            UIFonts.StyleButton(tab, UIFonts.BarlowSemiBold, 9, UIColors.TextDim);
            tab.ClipText = true;

            int tabIndex = i;
            tab.Pressed += () => SetActiveTab(tabIndex);

            StyleTab(tab, i == 0);
            tabRow.AddChild(tab);
            _tabButtons.Add(tab);
        }
    }

    private void SetActiveTab(int index)
    {
        _activeTab = index;
        for (int i = 0; i < _tabButtons.Count; i++)
            StyleTab(_tabButtons[i], i == index);
        RebuildList();
    }

    private static void StyleTab(Button tab, bool active)
    {
        var style = new StyleBoxFlat();
        style.SetCornerRadiusAll(0);
        style.SetBorderWidthAll(0);

        if (active)
        {
            style.BgColor = new Color(34 / 255f, 136 / 255f, 238 / 255f, 0.08f);
            style.BorderWidthBottom = 2;
            style.BorderColor = UIColors.Accent;
            tab.AddThemeColorOverride("font_color", new Color("#55bbff"));
        }
        else
        {
            style.BgColor = Colors.Transparent;
            tab.AddThemeColorOverride("font_color", UIColors.TextDim);
        }

        tab.AddThemeStyleboxOverride("normal", style);

        var hoverStyle = new StyleBoxFlat();
        hoverStyle.BgColor = new Color(34 / 255f, 136 / 255f, 238 / 255f, 0.06f);
        hoverStyle.SetBorderWidthAll(0);
        hoverStyle.SetCornerRadiusAll(0);
        tab.AddThemeStyleboxOverride("hover", hoverStyle);
        tab.AddThemeStyleboxOverride("pressed", style);
        tab.AddThemeStyleboxOverride("focus", style);
    }

    /// <summary>Set fleet/ship data for the FLEETS tab.</summary>
    public void SetData(List<FleetData> fleets, List<ShipInstanceData> ships)
    {
        _fleets = fleets;
        _ships = ships;
        RebuildList();
    }

    private void RebuildList()
    {
        // Clear existing items
        foreach (var child in _listContainer.GetChildren())
            child.QueueFree();

        if (_activeTab == 0)
            BuildFleetList();
        else
            BuildPlaceholder(TabNames[_activeTab]);
    }

    private void BuildFleetList()
    {
        int playerEmpireId = GameManager.Instance?.LocalPlayerEmpire?.Id ?? -1;

        foreach (var fleet in _fleets.Where(f => f.OwnerEmpireId == playerEmpireId))
        {
            var item = BuildFleetItem(fleet);
            _listContainer.AddChild(item);

            // Divider
            var div = new ColorRect();
            div.CustomMinimumSize = new Vector2(0, 1);
            div.Color = new Color(30 / 255f, 50 / 255f, 72 / 255f, 0.5f);
            var divMargin = new MarginContainer();
            divMargin.AddThemeConstantOverride("margin_left", 14);
            divMargin.AddThemeConstantOverride("margin_right", 14);
            divMargin.AddChild(div);
            _listContainer.AddChild(divMargin);
        }
    }

    private Button BuildFleetItem(FleetData fleet)
    {
        var btn = new Button();
        btn.CustomMinimumSize = new Vector2(0, 56);
        GlassPanel.StyleButton(btn);
        btn.Pressed += () => EventBus.Instance?.FireFleetSelected(fleet.Id);

        // Content overlay
        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        margin.MouseFilter = MouseFilterEnum.Ignore;
        btn.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 1);
        vbox.MouseFilter = MouseFilterEnum.Ignore;
        margin.AddChild(vbox);

        // Row 1: fleet name + status
        var row1 = new HBoxContainer();
        row1.AddThemeConstantOverride("separation", 8);
        row1.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(row1);

        var nameLabel = new Label { Text = fleet.Name };
        UIFonts.Style(nameLabel, UIFonts.Exo2SemiBold, 12, UIColors.TextBright);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        row1.AddChild(nameLabel);

        var statusLabel = new Label { Text = "PATROL" };
        UIFonts.Style(statusLabel, UIFonts.ShareTechMono, 8, UIColors.TextDim);
        statusLabel.MouseFilter = MouseFilterEnum.Ignore;
        row1.AddChild(statusLabel);

        // Row 2: location + ship count
        int shipCount = _ships.Count(s => s.FleetId == fleet.Id);
        var locLabel = new Label { Text = $"System {fleet.CurrentSystemId} · {shipCount} ships" };
        UIFonts.Style(locLabel, UIFonts.BarlowRegular, 10, UIColors.TextDim);
        locLabel.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(locLabel);

        // Row 3: ship pips
        var pipsRow = new HBoxContainer();
        pipsRow.AddThemeConstantOverride("separation", 3);
        pipsRow.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(pipsRow);

        for (int i = 0; i < Mathf.Min(shipCount, 12); i++)
        {
            var pip = new ColorRect();
            pip.CustomMinimumSize = new Vector2(5, 5);
            pip.Color = new Color(80 / 255f, 120 / 255f, 160 / 255f, 0.5f);
            pip.MouseFilter = MouseFilterEnum.Ignore;
            pipsRow.AddChild(pip);
        }

        return btn;
    }

    private void BuildPlaceholder(string tabName)
    {
        var label = new Label { Text = $"{tabName}\n(Coming soon)" };
        UIFonts.Style(label, UIFonts.BarlowRegular, 11, UIColors.TextFaint);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddChild(label);
        _listContainer.AddChild(margin);
    }

    private void OnFleetSelected(int fleetId)
    {
        // Could highlight the selected fleet in the list
    }

    private void OnFleetDeselected()
    {
        // Could remove highlight
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.FleetSelected -= OnFleetSelected;
            EventBus.Instance.FleetDeselected -= OnFleetDeselected;
        }
    }
}
