using Godot;
using System.Linq;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Right side panel showing selected system info, POIs, and action buttons.
/// 306px wide, anchored top=80 right=0, leaves 172px at bottom for event log.
/// Hidden when no system is selected.
/// </summary>
public partial class RightPanel : Control
{
    public const int PanelWidth = 306;

    private Label _systemName = null!;
    private Label _systemInfo = null!;
    private VBoxContainer _poiList = null!;
    private HBoxContainer _actionGrid = null!;
    private StarSystemData? _selectedSystem;

    public override void _Ready()
    {
        // Anchors: right side, below topbar, leave room for event log at bottom
        AnchorLeft = 1;
        AnchorRight = 1;
        AnchorTop = 0;
        AnchorBottom = 1;
        OffsetLeft = -PanelWidth;
        OffsetRight = 0;
        OffsetTop = TopBar.BarHeight;
        OffsetBottom = -172; // 160px event log + 12px gap
        ClipContents = true;
        ZIndex = 50;
        Visible = false; // hidden until a system is selected

        // Background with tarnished glass
        var bg = new PanelContainer { Name = "Bg" };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        GlassPanel.Apply(bg, enableBlur: true);
        AddChild(bg);

        // Main layout
        var layout = new VBoxContainer { Name = "Layout" };
        layout.SetAnchorsPreset(LayoutPreset.FullRect);
        layout.AddThemeConstantOverride("separation", 0);
        AddChild(layout);

        // System header
        BuildHeader(layout);

        // Divider
        AddDivider(layout);

        // POI list (scrollable)
        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        layout.AddChild(scroll);

        _poiList = new VBoxContainer { Name = "POIList" };
        _poiList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _poiList.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_poiList);

        // Divider
        AddDivider(layout);

        // Action buttons section
        BuildActionSection(layout);

        // Subscribe to events
        if (EventBus.Instance != null)
        {
            EventBus.Instance.SystemSelected += OnSystemSelected;
            EventBus.Instance.SystemDeselected += OnSystemDeselected;
        }
    }

    private void BuildHeader(VBoxContainer parent)
    {
        var headerMargin = new MarginContainer();
        headerMargin.AddThemeConstantOverride("margin_left", 16);
        headerMargin.AddThemeConstantOverride("margin_right", 16);
        headerMargin.AddThemeConstantOverride("margin_top", 12);
        headerMargin.AddThemeConstantOverride("margin_bottom", 8);
        parent.AddChild(headerMargin);

        var headerVBox = new VBoxContainer();
        headerVBox.AddThemeConstantOverride("separation", 4);
        headerMargin.AddChild(headerVBox);

        // Arm / tier label
        _systemInfo = new Label { Text = "" };
        UIFonts.Style(_systemInfo, UIFonts.ShareTechMono, 8, UIColors.TextFaint);
        headerVBox.AddChild(_systemInfo);

        // System name (large)
        _systemName = new Label { Text = "SELECT A SYSTEM" };
        UIFonts.Style(_systemName, UIFonts.Exo2SemiBold, 16, UIColors.TextBright);
        headerVBox.AddChild(_systemName);
    }

    private void BuildActionSection(VBoxContainer parent)
    {
        var actionsMargin = new MarginContainer();
        actionsMargin.AddThemeConstantOverride("margin_left", 12);
        actionsMargin.AddThemeConstantOverride("margin_right", 12);
        actionsMargin.AddThemeConstantOverride("margin_top", 8);
        actionsMargin.AddThemeConstantOverride("margin_bottom", 10);
        parent.AddChild(actionsMargin);

        var actionsVBox = new VBoxContainer();
        actionsVBox.AddThemeConstantOverride("separation", 6);
        actionsMargin.AddChild(actionsVBox);

        // Section label
        var label = new Label { Text = "ACTION BUTTONS" };
        UIFonts.Style(label, UIFonts.BarlowSemiBold, 9, UIColors.TextFaint);
        actionsVBox.AddChild(label);

        // Grid of 4 square buttons
        _actionGrid = new HBoxContainer();
        _actionGrid.AddThemeConstantOverride("separation", 6);
        actionsVBox.AddChild(_actionGrid);

        AddSquareActionButton("SEND\nFLEET", true);
        AddSquareActionButton("BUILD\nSTATION", false);
        AddSquareActionButton("EXPLORE", false);
        AddSquareActionButton("SCAN", false);
    }

    private void AddSquareActionButton(string text, bool primary)
    {
        var btn = new Button { Text = text };
        btn.CustomMinimumSize = new Vector2(56, 56);
        btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        GlassPanel.StyleButton(btn, primary);
        UIFonts.StyleButton(btn, UIFonts.BarlowSemiBold, 8,
            primary ? new Color("#55bbff") : UIColors.TextBody);
        _actionGrid.AddChild(btn);
    }

    private void OnSystemSelected(StarSystemData system)
    {
        _selectedSystem = system;
        Visible = true;

        _systemName.Text = system.Name.ToUpper();
        _systemInfo.Text = $"Arm {system.ArmIndex} Tier";

        RebuildPOIList(system);
    }

    private void OnSystemDeselected()
    {
        _selectedSystem = null;
        Visible = false;
    }

    private void RebuildPOIList(StarSystemData system)
    {
        foreach (var child in _poiList.GetChildren())
            child.QueueFree();

        foreach (var poi in system.POIs)
        {
            var item = BuildPOICard(poi);
            _poiList.AddChild(item);
        }

        if (system.POIs.Count == 0)
        {
            var empty = new Label { Text = "No points of interest" };
            UIFonts.Style(empty, UIFonts.BarlowRegular, 10, UIColors.TextFaint);
            empty.HorizontalAlignment = HorizontalAlignment.Center;
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_top", 12);
            margin.AddChild(empty);
            _poiList.AddChild(margin);
        }
    }

    private Control BuildPOICard(POIData poi)
    {
        var poiColor = GetPOIColor(poi.Type);
        var tintColor = GetPOITint(poi.Type);

        // Card container
        var card = new PanelContainer();
        var cardStyle = new StyleBoxFlat();
        cardStyle.BgColor = new Color(16 / 255f, 22 / 255f, 44 / 255f, 0.9f) + tintColor;
        cardStyle.SetBorderWidthAll(1);
        cardStyle.BorderColor = new Color(80 / 255f, 120 / 255f, 180 / 255f, 0.2f);
        cardStyle.BorderWidthLeft = 4;
        var leftBorderColor = poiColor;
        // StyleBoxFlat only supports one border color, so we use the accent color
        cardStyle.BorderColor = new Color(leftBorderColor, 0.6f);
        cardStyle.ContentMarginLeft = 12;
        cardStyle.ContentMarginRight = 12;
        cardStyle.ContentMarginTop = 8;
        cardStyle.ContentMarginBottom = 8;
        cardStyle.SetCornerRadiusAll(4);
        card.AddThemeStyleboxOverride("panel", cardStyle);

        var cardMargin = new MarginContainer();
        cardMargin.AddThemeConstantOverride("margin_left", 8);
        cardMargin.AddThemeConstantOverride("margin_right", 8);
        card.AddChild(cardMargin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        cardMargin.AddChild(vbox);

        // Row 1: Name + type label
        var row1 = new HBoxContainer();
        row1.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(row1);

        var nameLabel = new Label { Text = poi.Name };
        UIFonts.Style(nameLabel, UIFonts.Exo2SemiBold, 12, poiColor);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        nameLabel.ClipText = true;
        row1.AddChild(nameLabel);

        var typeLabel = new Label { Text = GetPOITypeLabel(poi.Type) };
        UIFonts.Style(typeLabel, UIFonts.BarlowRegular, 10, UIColors.TextDim);
        row1.AddChild(typeLabel);

        // Row 2: Stat readouts (POP, INCOME, DEFENSE)
        if (poi.Type == POIType.HabitablePlanet)
        {
            var statsRow = new HBoxContainer();
            statsRow.AddThemeConstantOverride("separation", 12);
            vbox.AddChild(statsRow);

            int sizeVal = (int)poi.PlanetSize + 1; // 1-6 scale
            AddStatReadout(statsRow, "POP:", $"{sizeVal * 0.7f:F1}B");
            AddStatReadout(statsRow, "INCOME:", $"{sizeVal * 1.5f:F1}K/M");
            AddStatReadout(statsRow, "DEFENSE:", $"{sizeVal * 500}");
        }
        else if (poi.Type == POIType.AsteroidField)
        {
            var statsRow = new HBoxContainer();
            statsRow.AddThemeConstantOverride("separation", 12);
            vbox.AddChild(statsRow);

            int depositCount = poi.Deposits?.Count ?? 0;
            AddStatReadout(statsRow, "DEPOSITS:", $"{depositCount}");
        }
        else
        {
            // Derelict, debris, etc. — show color
            var metaLabel = new Label { Text = GetPOIMeta(poi) };
            UIFonts.Style(metaLabel, UIFonts.ShareTechMono, 9, UIColors.TextDim);
            vbox.AddChild(metaLabel);
        }

        return card;
    }

    private static void AddStatReadout(HBoxContainer parent, string label, string value)
    {
        var stat = new VBoxContainer();
        stat.AddThemeConstantOverride("separation", 0);
        parent.AddChild(stat);

        var lbl = new Label { Text = label };
        UIFonts.Style(lbl, UIFonts.BarlowSemiBold, 8, UIColors.TextFaint);
        stat.AddChild(lbl);

        var val = new Label { Text = value };
        UIFonts.Style(val, UIFonts.ShareTechMono, 11, UIColors.TextBright);
        stat.AddChild(val);
    }

    private static Color GetPOIColor(POIType type) => type switch
    {
        POIType.HabitablePlanet => new Color("#22bb44"),
        POIType.BarrenPlanet => UIColors.TextDim,
        POIType.AsteroidField => new Color("#ddaa22"),
        POIType.DebrisField => new Color("#9944dd"),
        POIType.AbandonedStation => new Color("#9944dd"),
        POIType.ShipGraveyard => new Color("#f04030"),
        POIType.Megastructure => new Color("#2288ee"),
        _ => UIColors.TextDim
    };

    private static Color GetPOITint(POIType type) => type switch
    {
        POIType.HabitablePlanet => new Color(76 / 255f, 175 / 255f, 80 / 255f, 0.04f),
        POIType.DebrisField or POIType.AbandonedStation => new Color(179 / 255f, 102 / 255f, 232 / 255f, 0.04f),
        POIType.AsteroidField => new Color(138 / 255f, 138 / 255f, 60 / 255f, 0.06f),
        POIType.Megastructure => new Color(68 / 255f, 170 / 255f, 255 / 255f, 0.03f),
        POIType.ShipGraveyard => new Color(232 / 255f, 85 / 255f, 69 / 255f, 0.04f),
        _ => Colors.Transparent
    };

    private static string GetPOITypeLabel(POIType type) => type switch
    {
        POIType.HabitablePlanet => "Colony",
        POIType.BarrenPlanet => "Barren",
        POIType.AsteroidField => "Asteroid Field",
        POIType.DebrisField => "Derelict",
        POIType.AbandonedStation => "Abandoned",
        POIType.ShipGraveyard => "Graveyard",
        POIType.Megastructure => "Megastructure",
        _ => type.ToString()
    };

    private static string GetPOIMeta(POIData poi) => poi.Type switch
    {
        POIType.HabitablePlanet => $"SIZE {poi.PlanetSize}  DEPOSITS {poi.Deposits?.Count ?? 0}",
        POIType.AsteroidField => $"DEPOSITS {poi.Deposits?.Count ?? 0}",
        POIType.BarrenPlanet => $"SIZE {poi.PlanetSize}",
        _ => $"COLOR {poi.DominantColor}"
    };

    private static void AddDivider(VBoxContainer parent)
    {
        var div = new ColorRect();
        div.CustomMinimumSize = new Vector2(0, 1);
        div.Color = UIColors.BorderDim;
        parent.AddChild(div);
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.SystemSelected -= OnSystemSelected;
            EventBus.Instance.SystemDeselected -= OnSystemDeselected;
        }
    }
}
