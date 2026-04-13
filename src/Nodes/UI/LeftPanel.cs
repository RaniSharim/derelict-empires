using Godot;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Left side panel with tabs: FLEETS / COLONIES / RESEARCH / BUILD.
/// 310px wide, anchored top=80 left=0 bottom=-120 (room for minimap).
/// </summary>
public partial class LeftPanel : Control
{
    public const int PanelWidth = 310;

    private VBoxContainer _listContainer = null!;
    private readonly List<Button> _tabButtons = new();
    private int _activeTab;
    private int _selectedFleetId = -1;

    // Data references
    private List<FleetData> _fleets = new();
    private List<ShipInstanceData> _ships = new();

    private static readonly string[] TabNames = { "FLEETS", "COLONIES", "RESEARCH", "BUILD" };

    public override void _Ready()
    {
        // Anchors: left side, below topbar, leave 120px at bottom for minimap
        AnchorLeft = 0;
        AnchorRight = 0;
        AnchorTop = 0;
        AnchorBottom = 1;
        OffsetLeft = 0;
        OffsetRight = PanelWidth;
        OffsetTop = TopBar.BarHeight;
        OffsetBottom = -120;
        ClipContents = true;
        ZIndex = 50;

        // Background styling using GlassPanel
        var bg = new PanelContainer { Name = "Bg" };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        GlassPanel.Apply(bg, enableBlur: true);
        AddChild(bg);

        // Main layout
        var layout = new VBoxContainer { Name = "Layout" };
        layout.SetAnchorsPreset(LayoutPreset.FullRect);
        layout.AddThemeConstantOverride("separation", 0);
        AddChild(layout);

        // Tab bar
        BuildTabBar(layout);

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
        tabRow.CustomMinimumSize = new Vector2(0, 48);
        tabRow.AddThemeConstantOverride("separation", 0);
        parent.AddChild(tabRow);

        for (int i = 0; i < TabNames.Length; i++)
        {
            var tab = new Button { Text = TabNames[i] };
            tab.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            tab.CustomMinimumSize = new Vector2(0, 48);
            UIFonts.StyleButton(tab, UIFonts.Exo2Bold, 15, UIColors.TextDim);
            tab.ClipText = true;

            int tabIndex = i;
            tab.Pressed += () => SetActiveTab(tabIndex);

            StyleTab(tab, i == 0);
            tabRow.AddChild(tab);
            _tabButtons.Add(tab);
        }

        // Underline separator spanning full width
        var underline = new ColorRect();
        underline.CustomMinimumSize = new Vector2(0, 1);
        underline.Color = new Color(80 / 255f, 120 / 255f, 180 / 255f, 0.15f);
        parent.AddChild(underline);
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
            tab.AddThemeColorOverride("font_color", new Color("#44aaff"));
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
        var playerFleets = _fleets.Where(f => f.OwnerEmpireId == playerEmpireId).ToList();

        for (int i = 0; i < playerFleets.Count; i++)
        {
            var fleet = playerFleets[i];
            var item = BuildFleetCard(fleet);
            _listContainer.AddChild(item);
        }

        if (playerFleets.Count == 0)
        {
            var empty = new Label { Text = "No fleets available" };
            UIFonts.Style(empty, UIFonts.BarlowRegular, 11, UIColors.TextFaint);
            empty.HorizontalAlignment = HorizontalAlignment.Center;
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_top", 20);
            margin.AddChild(empty);
            _listContainer.AddChild(margin);
        }
    }

    private Control BuildFleetCard(FleetData fleet)
    {
        bool isSelected = fleet.Id == _selectedFleetId;
        var factionColor = UIColors.Accent; // Player fleet accent

        // Outer margin for card spacing
        var outerMargin = new MarginContainer();
        outerMargin.AddThemeConstantOverride("margin_left", 8);
        outerMargin.AddThemeConstantOverride("margin_right", 8);
        outerMargin.AddThemeConstantOverride("margin_top", 3);
        outerMargin.AddThemeConstantOverride("margin_bottom", 3);

        // Card button (clickable)
        var btn = new Button();
        btn.CustomMinimumSize = new Vector2(0, 90);
        btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        // Card background style — elevated from panel per spec §5.2
        var normalStyle = new StyleBoxFlat();
        normalStyle.BgColor = new Color(16 / 255f, 22 / 255f, 44 / 255f, 0.92f);
        normalStyle.SetBorderWidthAll(1);
        normalStyle.BorderColor = new Color(80 / 255f, 120 / 255f, 180 / 255f, 0.20f);
        normalStyle.SetCornerRadiusAll(4);
        // Left padding to make room for the accent strip
        normalStyle.ContentMarginLeft = 4;
        btn.AddThemeStyleboxOverride("normal", normalStyle);

        var hoverStyle = new StyleBoxFlat();
        hoverStyle.BgColor = new Color(22 / 255f, 30 / 255f, 56 / 255f, 0.95f);
        hoverStyle.SetBorderWidthAll(1);
        hoverStyle.BorderColor = new Color(80 / 255f, 120 / 255f, 180 / 255f, 0.50f);
        hoverStyle.SetCornerRadiusAll(4);
        hoverStyle.ContentMarginLeft = 4;
        btn.AddThemeStyleboxOverride("hover", hoverStyle);
        btn.AddThemeStyleboxOverride("pressed", hoverStyle);
        btn.AddThemeStyleboxOverride("focus", normalStyle);

        btn.Pressed += () => EventBus.Instance?.FireFleetSelected(fleet.Id);

        // Left accent border strip (ColorRect overlay)
        var accentStrip = new ColorRect();
        accentStrip.Color = new Color(factionColor, isSelected ? 1.0f : 0.6f);
        accentStrip.AnchorLeft = 0;
        accentStrip.AnchorRight = 0;
        accentStrip.AnchorTop = 0;
        accentStrip.AnchorBottom = 1;
        accentStrip.OffsetLeft = 0;
        accentStrip.OffsetRight = 4;
        accentStrip.OffsetTop = 1;
        accentStrip.OffsetBottom = -1;
        accentStrip.MouseFilter = MouseFilterEnum.Ignore;
        btn.AddChild(accentStrip);

        // Content overlay
        var content = new MarginContainer();
        content.SetAnchorsPreset(LayoutPreset.FullRect);
        content.AddThemeConstantOverride("margin_left", 12);
        content.AddThemeConstantOverride("margin_right", 10);
        content.AddThemeConstantOverride("margin_top", 8);
        content.AddThemeConstantOverride("margin_bottom", 8);
        content.MouseFilter = MouseFilterEnum.Ignore;
        btn.AddChild(content);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 3);
        vbox.MouseFilter = MouseFilterEnum.Ignore;
        content.AddChild(vbox);

        // Row 1: Fleet name + status badge
        var row1 = new HBoxContainer();
        row1.AddThemeConstantOverride("separation", 6);
        row1.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(row1);

        var nameLabel = new Label { Text = fleet.Name.ToUpper() };
        UIFonts.Style(nameLabel, UIFonts.Exo2SemiBold, 12, UIColors.TextBright);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        nameLabel.ClipText = true;
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        row1.AddChild(nameLabel);

        // Status badge — monospace, colored
        var statusLabel = new Label { Text = "MOVING" };
        UIFonts.Style(statusLabel, UIFonts.ShareTechMono, 8, UIColors.Moving);
        statusLabel.MouseFilter = MouseFilterEnum.Ignore;
        row1.AddChild(statusLabel);

        // Row 2: Fleet ID tag
        var idLabel = new Label { Text = $"#fcc{fleet.Id:X2}" };
        UIFonts.Style(idLabel, UIFonts.ShareTechMono, 8, UIColors.TextFaint);
        idLabel.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(idLabel);

        // Row 3: Location + ship count (Barlow Condensed body text)
        int shipCount = _ships.Count(s => s.FleetId == fleet.Id);
        var galaxy = GameManager.Instance?.Galaxy;
        string systemName = galaxy?.GetSystem(fleet.CurrentSystemId)?.Name ?? $"System {fleet.CurrentSystemId}";
        var locLabel = new Label { Text = $"Location: Sol / {systemName} \u00B7 {shipCount} SHIPS" };
        UIFonts.Style(locLabel, UIFonts.BarlowRegular, 11, UIColors.TextDim);
        locLabel.ClipText = true;
        locLabel.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(locLabel);

        // Row 4: Ship pips
        var pipsRow = new HBoxContainer();
        pipsRow.AddThemeConstantOverride("separation", 3);
        pipsRow.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(pipsRow);

        for (int i = 0; i < Mathf.Min(shipCount, 12); i++)
        {
            var ship = _ships.Where(s => s.FleetId == fleet.Id).ElementAtOrDefault(i);
            var pip = new ShipPip(ship);
            pip.CustomMinimumSize = new Vector2(6, 6);
            pip.MouseFilter = MouseFilterEnum.Ignore;
            pipsRow.AddChild(pip);
        }

        outerMargin.AddChild(btn);
        return outerMargin;
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
        _selectedFleetId = fleetId;
        RebuildList();
    }

    private void OnFleetDeselected()
    {
        _selectedFleetId = -1;
        RebuildList();
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

public partial class ShipPip : Control
{
    private readonly ShipInstanceData? _ship;

    public ShipPip(ShipInstanceData? ship)
    {
        _ship = ship;
    }

    public override void _Draw()
    {
        float w = Size.X;
        float h = Size.Y;
        var center = new Vector2(w / 2, h / 2);

        Color c = new Color(80 / 255f, 120 / 255f, 160 / 255f, 0.5f);
        if (_ship != null)
        {
            if (_ship.CurrentHp < _ship.MaxHp)
                c = UIColors.Alert;
            else if (_ship.Role == "Salvager")
                c = UIColors.GreenGlow;
            else if (_ship.Role == "Builder")
                c = UIColors.GoldGlow;
            else if (_ship.SizeClass >= ShipSizeClass.Destroyer)
                c = new Color(120 / 255f, 170 / 255f, 210 / 255f, 0.75f);
        }

        if (_ship?.Role == "Builder")
            DrawRect(new Rect2(0, 0, w, h), c);
        else if (_ship?.Role == "Salvager")
            DrawCircle(center, w / 2, c);
        else if (_ship != null && _ship.SizeClass >= ShipSizeClass.Destroyer)
            DrawRect(new Rect2(0, 0, w, h), c);
        else
        {
            DrawPolygon(new[] {
                new Vector2(w / 2, 0),
                new Vector2(w, h),
                new Vector2(0, h)
            }, new[] { c });
        }
    }
}
