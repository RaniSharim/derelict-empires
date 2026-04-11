using Godot;
using System.Linq;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Right side panel showing selected system info, POIs, and action buttons.
/// 275px wide, anchored top=68 right=0 bottom=0.
/// Hidden when no system is selected.
/// </summary>
public partial class RightPanel : Control
{
    private Label _systemName = null!;
    private Label _systemInfo = null!;
    private VBoxContainer _poiList = null!;
    private VBoxContainer _actionButtons = null!;
    private StarSystemData? _selectedSystem;

    public override void _Ready()
    {
        // Anchors: right side, below topbar, full height
        AnchorLeft = 1;
        AnchorRight = 1;
        AnchorTop = 0;
        AnchorBottom = 1;
        OffsetLeft = -275;
        OffsetRight = 0;
        OffsetTop = 68;
        OffsetBottom = 0;
        ClipContents = true;
        ZIndex = 50;
        Visible = false; // hidden until a system is selected

        // Background
        var bg = new Panel { Name = "Bg" };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = UIColors.GlassDarkFlat;
        bgStyle.SetBorderWidthAll(0);
        bgStyle.BorderWidthLeft = 1;
        bgStyle.BorderColor = UIColors.BorderBright;
        bgStyle.SetCornerRadiusAll(0);
        bg.AddThemeStyleboxOverride("panel", bgStyle);
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

        // POI section header
        var poiHeader = new MarginContainer();
        poiHeader.AddThemeConstantOverride("margin_left", 16);
        poiHeader.AddThemeConstantOverride("margin_top", 8);
        poiHeader.AddThemeConstantOverride("margin_bottom", 4);
        layout.AddChild(poiHeader);

        var poiLabel = new Label { Text = "POIs" };
        UIFonts.Style(poiLabel, UIFonts.BarlowSemiBold, 9, UIColors.TextFaint);
        poiHeader.AddChild(poiLabel);

        // POI list (scrollable)
        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        layout.AddChild(scroll);

        _poiList = new VBoxContainer { Name = "POIList" };
        _poiList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _poiList.AddThemeConstantOverride("separation", 0);
        scroll.AddChild(_poiList);

        // Divider
        AddDivider(layout);

        // Action buttons
        _actionButtons = new VBoxContainer { Name = "Actions" };
        _actionButtons.AddThemeConstantOverride("separation", 4);
        var actionsMargin = new MarginContainer();
        actionsMargin.AddThemeConstantOverride("margin_left", 12);
        actionsMargin.AddThemeConstantOverride("margin_right", 12);
        actionsMargin.AddThemeConstantOverride("margin_top", 8);
        actionsMargin.AddThemeConstantOverride("margin_bottom", 8);
        actionsMargin.AddChild(_actionButtons);
        layout.AddChild(actionsMargin);

        BuildActionButtons();

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
        headerVBox.AddThemeConstantOverride("separation", 2);
        headerMargin.AddChild(headerVBox);

        _systemName = new Label { Text = "SELECT A SYSTEM" };
        UIFonts.Style(_systemName, UIFonts.Exo2SemiBold, 18, UIColors.TextBright);
        headerVBox.AddChild(_systemName);

        _systemInfo = new Label { Text = "" };
        UIFonts.Style(_systemInfo, UIFonts.ShareTechMono, 9, UIColors.TextFaint);
        headerVBox.AddChild(_systemInfo);
    }

    private void BuildActionButtons()
    {
        AddActionButton("SEND FLEET", true);
        AddActionButton("BUILD MINING STATION", false);
        AddActionButton("EXPLORE", false);
        AddActionButton("SCAN", false);
    }

    private void AddActionButton(string text, bool primary)
    {
        var btn = new Button { Text = text };
        btn.CustomMinimumSize = new Vector2(0, 36);
        GlassPanel.StyleButton(btn, primary);
        UIFonts.StyleButton(btn, UIFonts.BarlowSemiBold, 10,
            primary ? new Color("#55bbff") : UIColors.TextBody);
        _actionButtons.AddChild(btn);
    }

    private void OnSystemSelected(StarSystemData system)
    {
        _selectedSystem = system;
        Visible = true;

        _systemName.Text = system.Name.ToUpper();
        _systemInfo.Text = $"ARM {system.ArmIndex}  ·  {system.POIs.Count} POIs";

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
            var item = BuildPOIItem(poi);
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

    private Control BuildPOIItem(POIData poi)
    {
        var item = new PanelContainer();
        var itemStyle = new StyleBoxFlat();
        itemStyle.BgColor = Colors.Transparent;
        itemStyle.SetBorderWidthAll(0);
        itemStyle.BorderWidthLeft = 2;
        itemStyle.BorderColor = GetPOIColor(poi.Type);
        itemStyle.ContentMarginLeft = 16;
        itemStyle.ContentMarginRight = 16;
        itemStyle.ContentMarginTop = 6;
        itemStyle.ContentMarginBottom = 6;
        itemStyle.SetCornerRadiusAll(0);
        item.AddThemeStyleboxOverride("panel", itemStyle);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);
        item.AddChild(vbox);

        // Row 1: dot + name + type tag
        var row1 = new HBoxContainer();
        row1.AddThemeConstantOverride("separation", 6);
        vbox.AddChild(row1);

        // Colored dot
        var dot = new ColorRect();
        dot.CustomMinimumSize = new Vector2(4, 4);
        dot.Color = GetPOIColor(poi.Type);
        dot.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        row1.AddChild(dot);

        // Name
        var nameLabel = new Label { Text = poi.Name.ToUpper() };
        UIFonts.Style(nameLabel, UIFonts.BarlowSemiBold, 11, UIColors.TextLabel);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row1.AddChild(nameLabel);

        // Type tag
        var typeTag = new Label { Text = poi.Type.ToString().ToUpper() };
        UIFonts.Style(typeTag, UIFonts.ShareTechMono, 8, UIColors.TextDim);
        row1.AddChild(typeTag);

        // Row 2: metadata
        string meta = GetPOIMeta(poi);
        if (!string.IsNullOrEmpty(meta))
        {
            var metaLabel = new Label { Text = meta };
            UIFonts.Style(metaLabel, UIFonts.ShareTechMono, 9, UIColors.TextDim);
            vbox.AddChild(metaLabel);
        }

        return item;
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
