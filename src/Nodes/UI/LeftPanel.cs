using Godot;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Ships;
using DerlictEmpires.Nodes.Map;

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
    private readonly HashSet<int> _selectedFleetIds = new();

    // Data references
    private List<FleetData> _fleets = new();
    private List<ShipInstanceData> _ships = new();
    private MainScene? _mainScene;

    // Research tab content (cached to preserve state across tab switches)
    private ResearchTabContent? _researchContent;

    public void SetMainScene(MainScene mainScene) => _mainScene = mainScene;

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

        // Top edge highlight — simulates light catching the glass bevel (spec §1 layer 5)
        var topEdge = new ColorRect { Name = "TopEdge" };
        topEdge.Color = new Color(80 / 255f, 140 / 255f, 220 / 255f, 0.25f);
        topEdge.AnchorLeft = 0;
        topEdge.AnchorRight = 1;
        topEdge.AnchorTop = 0;
        topEdge.AnchorBottom = 0;
        topEdge.OffsetTop = 0;
        topEdge.OffsetBottom = 1;
        topEdge.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(topEdge);

        // Right edge highlight (panel is on left side, so right edge faces the map)
        var rightEdge = new ColorRect { Name = "RightEdge" };
        rightEdge.Color = new Color(80 / 255f, 140 / 255f, 220 / 255f, 0.18f);
        rightEdge.AnchorLeft = 1;
        rightEdge.AnchorRight = 1;
        rightEdge.AnchorTop = 0;
        rightEdge.AnchorBottom = 1;
        rightEdge.OffsetLeft = -1;
        rightEdge.OffsetRight = 0;
        rightEdge.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(rightEdge);

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
            EventBus.Instance.FleetSelectionToggled += OnFleetSelectionToggled;
            EventBus.Instance.FleetDeselected += OnFleetDeselected;
            EventBus.Instance.FleetOrderChanged += OnFleetOrderChanged;
            EventBus.Instance.FleetArrivedAtSystem += OnFleetArrived;
            EventBus.Instance.SiteActivityChanged += OnSiteActivityChanged;
            EventBus.Instance.SiteActivityRateChanged += OnSiteActivityRateChanged;
            EventBus.Instance.DesignSaved += OnDesignSaved;
        }
    }

    private void OnDesignSaved(string _)
    {
        if (_activeTab == 3) RebuildList();
    }

    private void OnFleetOrderChanged(int fleetId) => RebuildList();
    private void OnFleetArrived(int fleetId, int systemId) => RebuildList();
    private void OnSiteActivityChanged(int empireId, int poiId, SiteActivity activity) => RebuildList();
    private void OnSiteActivityRateChanged(int empireId, int poiId) => RebuildList();

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
            tab.CustomMinimumSize = new Vector2(0, 40);
            UIFonts.StyleButton(tab, UIFonts.RajdhaniSemiBold, 10, UIColors.TextBody);
            tab.ClipText = true;

            int tabIndex = i;
            tab.Pressed += () => SetActiveTab(tabIndex);

            // FLEETS (0), RESEARCH (2), BUILD (3) are active. COLONIES (1) still a placeholder.
            if (i == 1)
                tab.Disabled = true;

            StyleTab(tab, i == 0);
            tabRow.AddChild(tab);
            _tabButtons.Add(tab);
        }

        // Underline separator spanning full width
        var underline = new ColorRect();
        underline.CustomMinimumSize = new Vector2(0, 1);
        underline.Color = UIColors.BorderMid;
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
            style.BgColor = new Color(34 / 255f, 136 / 255f, 238 / 255f, 0.10f);
            style.BorderWidthBottom = 2;
            style.BorderColor = UIColors.Accent;
            tab.AddThemeColorOverride("font_color", UIColors.Accent);
        }
        else
        {
            style.BgColor = Colors.Transparent;
            tab.AddThemeColorOverride("font_color", UIColors.TextBody);
        }

        tab.AddThemeStyleboxOverride("normal", style);

        var hoverStyle = new StyleBoxFlat();
        hoverStyle.BgColor = new Color(34 / 255f, 136 / 255f, 238 / 255f, 0.08f);
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
        else if (_activeTab == 2)
            BuildResearchTab();
        else if (_activeTab == 3)
            BuildDesignsList();
        else
            BuildPlaceholder(TabNames[_activeTab]);
    }

    private void BuildDesignsList()
    {
        var player = GameManager.Instance?.LocalPlayerEmpire;
        if (player == null)
        {
            BuildPlaceholder("BUILD");
            return;
        }

        // [+ NEW DESIGN] row — always first
        _listContainer.AddChild(BuildNewDesignRow());

        var designs = player.DesignState.Designs;
        if (designs.Count == 0)
        {
            var empty = new Label { Text = "No saved designs yet." };
            UIFonts.Style(empty, UIFonts.RajdhaniRegular, 11, UIColors.TextFaint);
            empty.HorizontalAlignment = HorizontalAlignment.Center;
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_top", 12);
            margin.AddChild(empty);
            _listContainer.AddChild(margin);
            return;
        }

        foreach (var design in designs)
            _listContainer.AddChild(BuildDesignCard(design));
    }

    private Control BuildNewDesignRow()
    {
        var outer = new MarginContainer();
        outer.AddThemeConstantOverride("margin_left", 8);
        outer.AddThemeConstantOverride("margin_right", 8);
        outer.AddThemeConstantOverride("margin_top", 8);
        outer.AddThemeConstantOverride("margin_bottom", 4);

        var btn = new Button { Text = "+  NEW DESIGN" };
        btn.CustomMinimumSize = new Vector2(0, 36);
        UIFonts.StyleButtonRole(btn, UIFonts.Role.UILabel, UIColors.Accent);
        GlassPanel.StyleButton(btn);
        btn.Pressed += () =>
            EventBus.Instance?.FireDesignerOpenRequested(new DesignerOpenRequest());
        outer.AddChild(btn);
        return outer;
    }

    private Control BuildDesignCard(ShipDesign design)
    {
        var outer = new MarginContainer();
        outer.AddThemeConstantOverride("margin_left", 8);
        outer.AddThemeConstantOverride("margin_right", 8);
        outer.AddThemeConstantOverride("margin_top", 3);
        outer.AddThemeConstantOverride("margin_bottom", 3);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(0, 72);

        var bg = new StyleBoxFlat
        {
            BgColor = new Color(14 / 255f, 20 / 255f, 40 / 255f, 0.92f),
            BorderColor = UIColors.BorderMid,
        };
        bg.SetBorderWidthAll(1);
        bg.SetCornerRadiusAll(4);
        bg.ContentMarginLeft = 10;
        bg.ContentMarginRight = 10;
        bg.ContentMarginTop = 8;
        bg.ContentMarginBottom = 8;
        panel.AddThemeStyleboxOverride("panel", bg);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 4);
        panel.AddChild(col);

        var chassis = design.GetChassis();
        var titleRow = new HBoxContainer();
        titleRow.AddThemeConstantOverride("separation", 8);
        col.AddChild(titleRow);

        var title = new Label { Text = design.Name.ToUpperInvariant() };
        UIFonts.StyleRole(title, UIFonts.Role.TitleMedium);
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        title.ClipText = true;
        titleRow.AddChild(title);

        var info = new Label
        {
            Text = chassis != null
                ? $"{chassis.DisplayName.ToUpperInvariant()} · {design.SlotFills.Count(s => !string.IsNullOrEmpty(s))}/{chassis.BigSystemSlots}"
                : "Unknown chassis"
        };
        UIFonts.StyleRole(info, UIFonts.Role.DataSmall);
        col.AddChild(info);

        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 6);
        col.AddChild(actionRow);

        var editBtn = new Button { Text = "EDIT" };
        editBtn.CustomMinimumSize = new Vector2(72, 26);
        UIFonts.StyleButtonRole(editBtn, UIFonts.Role.UILabel);
        GlassPanel.StyleButton(editBtn);
        var capturedId = design.Id;
        editBtn.Pressed += () =>
            EventBus.Instance?.FireDesignerOpenRequested(new DesignerOpenRequest { DesignId = capturedId });
        actionRow.AddChild(editBtn);

        var buildBtn = new Button { Text = "BUILD" };
        buildBtn.CustomMinimumSize = new Vector2(72, 26);
        UIFonts.StyleButtonRole(buildBtn, UIFonts.Role.UILabel);
        GlassPanel.StyleButton(buildBtn);
        buildBtn.Disabled = true;
        buildBtn.TooltipText = "BUILD queue — Phase F";
        actionRow.AddChild(buildBtn);

        var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        actionRow.AddChild(spacer);

        outer.AddChild(panel);
        return outer;
    }

    private void BuildResearchTab()
    {
        _researchContent = new ResearchTabContent { Name = "ResearchContent" };
        if (_mainScene != null)
            _researchContent.Configure(_mainScene);
        _listContainer.AddChild(_researchContent);
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
            UIFonts.Style(empty, UIFonts.RajdhaniRegular, 11, UIColors.TextFaint);
            empty.HorizontalAlignment = HorizontalAlignment.Center;
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_top", 20);
            margin.AddChild(empty);
            _listContainer.AddChild(margin);
        }
    }

    private Control BuildFleetCard(FleetData fleet)
    {
        bool isSelected = _selectedFleetIds.Contains(fleet.Id);
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
        normalStyle.BgColor = new Color(14 / 255f, 20 / 255f, 40 / 255f, 0.95f);
        normalStyle.SetBorderWidthAll(1);
        normalStyle.BorderColor = UIColors.BorderMid;
        normalStyle.SetCornerRadiusAll(4);
        // Left padding to make room for the accent strip
        normalStyle.ContentMarginLeft = 4;
        btn.AddThemeStyleboxOverride("normal", normalStyle);

        var hoverStyle = new StyleBoxFlat();
        hoverStyle.BgColor = new Color(22 / 255f, 30 / 255f, 56 / 255f, 0.97f);
        hoverStyle.SetBorderWidthAll(1);
        hoverStyle.BorderColor = UIColors.BorderBright;
        hoverStyle.SetCornerRadiusAll(4);
        hoverStyle.ContentMarginLeft = 4;
        btn.AddThemeStyleboxOverride("hover", hoverStyle);
        btn.AddThemeStyleboxOverride("pressed", hoverStyle);
        btn.AddThemeStyleboxOverride("focus", normalStyle);

        // Capture modifier state from the mouse event that drives the Pressed signal.
        // Input.IsKeyPressed works for OS keyboard but not for MCP-injected InputEvents,
        // so we read CtrlPressed off the event itself.
        bool ctrlModifier = false;
        btn.GuiInput += (ev) =>
        {
            if (ev is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
                ctrlModifier = mb.CtrlPressed;
        };
        btn.Pressed += () =>
        {
            bool ctrl = ctrlModifier || Input.IsKeyPressed(Key.Ctrl);
            ctrlModifier = false;
            if (ctrl) EventBus.Instance?.FireFleetSelectionToggled(fleet.Id);
            else EventBus.Instance?.FireFleetSelected(fleet.Id);
        };

        string tip = BuildFleetTooltip(fleet);
        if (!string.IsNullOrEmpty(tip)) btn.TooltipText = tip;

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
        UIFonts.Style(nameLabel, UIFonts.Exo2SemiBold, 13, UIColors.TextBright);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        nameLabel.ClipText = true;
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        row1.AddChild(nameLabel);

        // Status badge — derived from active orders.
        var (statusText, statusColor) = GetFleetStatus(fleet);
        var statusLabel = new Label { Text = statusText };
        UIFonts.Style(statusLabel, UIFonts.MonoMedium, 9, statusColor);
        statusLabel.MouseFilter = MouseFilterEnum.Ignore;
        row1.AddChild(statusLabel);

        // Row 2: Fleet ID tag
        var idLabel = new Label { Text = $"#fcc{fleet.Id:X2}" };
        UIFonts.Style(idLabel, UIFonts.MonoMedium, 9, UIColors.TextDim);
        idLabel.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(idLabel);

        // Row 3: Location + ship count (Rajdhani body text)
        int shipCount = _ships.Count(s => s.FleetId == fleet.Id);
        var galaxy = GameManager.Instance?.Galaxy;
        string systemName = galaxy?.GetSystem(fleet.CurrentSystemId)?.Name ?? $"System {fleet.CurrentSystemId}";
        var locLabel = new Label { Text = $"Location: {systemName} \u00B7 {shipCount} SHIPS" };
        UIFonts.Style(locLabel, UIFonts.RajdhaniRegular, 11, UIColors.TextBody);
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
        UIFonts.Style(label, UIFonts.RajdhaniRegular, 11, UIColors.TextFaint);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddChild(label);
        _listContainer.AddChild(margin);
    }

    private void OnFleetSelected(int fleetId)
    {
        _selectedFleetIds.Clear();
        _selectedFleetIds.Add(fleetId);
        RebuildList();
    }

    private void OnFleetSelectionToggled(int fleetId)
    {
        if (!_selectedFleetIds.Add(fleetId)) _selectedFleetIds.Remove(fleetId);
        RebuildList();
    }

    private void OnFleetDeselected()
    {
        _selectedFleetIds.Clear();
        RebuildList();
    }

    private (string text, Color color) GetFleetStatus(FleetData fleet)
    {
        var moveOrder = _mainScene?.MovementSystem?.GetOrder(fleet.Id);
        if (moveOrder != null && !moveOrder.IsComplete)
            return ("EN ROUTE", UIColors.Moving);

        var salvage = _mainScene?.SalvageSystem;
        if (salvage != null && _mainScene != null)
        {
            var (scans, extracts) = salvage.GetFleetContributions(fleet, _mainScene.ShipsById);
            if (extracts.Count > 0) return ("ENGAGED", UIColors.GreenGlow);
            if (scans.Count > 0) return ("ENGAGED", UIColors.Accent);
        }
        return ("IDLE", UIColors.TextDim);
    }

    private string BuildFleetTooltip(FleetData fleet)
    {
        var salvage = _mainScene?.SalvageSystem;
        if (salvage == null || _mainScene == null) return "";
        var (scans, extracts) = salvage.GetFleetContributions(fleet, _mainScene.ShipsById);
        if (scans.Count == 0 && extracts.Count == 0) return "";

        var gm = GameManager.Instance;
        string NameFor(int poiId)
        {
            if (gm?.Galaxy == null) return $"POI {poiId}";
            foreach (var s in gm.Galaxy.Systems)
                foreach (var p in s.POIs)
                    if (p.Id == poiId) return p.Name;
            return $"POI {poiId}";
        }

        var lines = new List<string>();
        if (scans.Count > 0)
            lines.Add("Scanning: " + string.Join(", ", scans.Select(NameFor)));
        if (extracts.Count > 0)
            lines.Add("Extracting: " + string.Join(", ", extracts.Select(NameFor)));
        return string.Join("\n", lines);
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.FleetSelected -= OnFleetSelected;
            EventBus.Instance.FleetSelectionToggled -= OnFleetSelectionToggled;
            EventBus.Instance.FleetDeselected -= OnFleetDeselected;
            EventBus.Instance.FleetOrderChanged -= OnFleetOrderChanged;
            EventBus.Instance.FleetArrivedAtSystem -= OnFleetArrived;
            EventBus.Instance.SiteActivityChanged -= OnSiteActivityChanged;
            EventBus.Instance.SiteActivityRateChanged -= OnSiteActivityRateChanged;
            EventBus.Instance.DesignSaved -= OnDesignSaved;
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
