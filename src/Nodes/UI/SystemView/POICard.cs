using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// A single POI card in a band — fixed 160px wide, grows vertically with content.
/// Accent bar keyed to type, header with type tag + signature, name, status line.
/// See design/in_system_design.md §4.
/// </summary>
public partial class POICard : PanelContainer
{
    public const int FixedWidth = 160;

    public POIData Poi { get; private set; }
    public POIEntity? Primary { get; private set; }

    private bool _isSelected;
    private bool _isHovered;

    private StyleBoxFlat _idleStyle = null!;
    private StyleBoxFlat _hoverStyle = null!;
    private StyleBoxFlat _selectedStyle = null!;
    private ColorRect _accentBar = null!;

    public POICard(POIData poi, POIEntity? primary)
    {
        Poi = poi;
        Primary = primary;
    }

    public override void _Ready()
    {
        Name = $"POI_{Poi.Id}";
        CustomMinimumSize = new Vector2(FixedWidth, 0);
        SizeFlagsHorizontal = 0; // don't stretch horizontally
        SizeFlagsVertical   = SizeFlags.ShrinkBegin; // hug content top-of-band
        MouseFilter = MouseFilterEnum.Stop;

        BuildStyles();
        AddThemeStyleboxOverride("panel", _idleStyle);

        // Single-child layout: HBox of [3px accent bar | content VBox]. Keeps the accent
        // structurally sized — PanelContainer only sizes one child, so overlaying doesn't work.
        var h = new HBoxContainer();
        h.AddThemeConstantOverride("separation", 7);
        AddChild(h);

        _accentBar = new ColorRect
        {
            Color = AccentFor(Poi, Primary),
            CustomMinimumSize = new Vector2(3, 0),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _accentBar.SizeFlagsVertical = SizeFlags.Fill;
        h.AddChild(_accentBar);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 0);
        margin.AddThemeConstantOverride("margin_right", 7);
        margin.AddThemeConstantOverride("margin_top", 9);
        margin.AddThemeConstantOverride("margin_bottom", 9);
        margin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        h.AddChild(margin);

        var v = new VBoxContainer();
        v.AddThemeConstantOverride("separation", 3);
        margin.AddChild(v);

        // Header: type tag + signature.
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 4);
        v.AddChild(header);

        var tag = new Label { Text = TypeTag(Poi) };
        UIFonts.Style(tag, UIFonts.Main, 9, UIColors.TextDim);
        header.AddChild(tag);

        header.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        int sig = Primary?.Signature ?? 0;
        header.AddChild(DetectionGlyph.CreateLabel(DetectionGlyph.Kind.Signature, 11, sig.ToString()));

        // Name.
        var name = new Label { Text = DisplayName(Poi, Primary), ClipText = true };
        UIFonts.Style(name, UIFonts.Main, UIFonts.SmallSize, UIColors.TextLabel);
        v.AddChild(name);

        // Status line.
        var status = new Label { Text = StatusLine(Poi, Primary) };
        UIFonts.Style(status, UIFonts.Main, 10, UIColors.TextDim);
        v.AddChild(status);

        GuiInput += OnGuiInput;
        MouseEntered += () => { _isHovered = true; RefreshStyle(); };
        MouseExited  += () => { _isHovered = false; RefreshStyle(); };
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        RefreshStyle();
    }

    private void BuildStyles()
    {
        _idleStyle = new StyleBoxFlat
        {
            BgColor = new Color(4 / 255f, 8 / 255f, 16 / 255f, 0.88f),
            BorderColor = UIColors.BorderDim,
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2, CornerRadiusBottomRight = 2,
        };
        _hoverStyle = new StyleBoxFlat
        {
            BgColor = new Color(34 / 255f, 136 / 255f, 238 / 255f, 0.07f),
            BorderColor = UIColors.BorderMid,
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2, CornerRadiusBottomRight = 2,
        };
        _selectedStyle = new StyleBoxFlat
        {
            BgColor = new Color(34 / 255f, 136 / 255f, 238 / 255f, 0.14f),
            BorderColor = new Color(34 / 255f, 136 / 255f, 238 / 255f, 1),
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2, CornerRadiusBottomRight = 2,
        };
    }

    private void RefreshStyle()
    {
        var style = _isSelected ? _selectedStyle : (_isHovered ? _hoverStyle : _idleStyle);
        AddThemeStyleboxOverride("panel", style);
    }

    private void OnGuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            EventBus.Instance?.FirePOISelected(Poi.Id);
            // v3: single-entity POI click also sets Selected Entity. When P4 lands shared-POI
            // sub-tickets, the card *header* will be POI-only and sub-ticket rows will fire the
            // per-entity selection instead.
            if (Primary != null)
                EventBus.Instance?.FireEntitySelected(Primary.Kind.ToString(), Primary.Id, Poi.Id);
            else
                EventBus.Instance?.FireEntityDeselected();
            AcceptEvent();
        }
    }

    private static string TypeTag(POIData poi) => poi.Type switch
    {
        POIType.HabitablePlanet   => "HABITABLE",
        POIType.BarrenPlanet      => "BARREN",
        POIType.AsteroidField     => "ASTEROIDS",
        POIType.DebrisField       => "DEBRIS",
        POIType.AbandonedStation  => "PRECURSOR STN",
        POIType.ShipGraveyard     => "GRAVEYARD",
        POIType.Megastructure     => "MEGASTRUCT",
        _                         => "POI",
    };

    private static string DisplayName(POIData poi, POIEntity? primary)
    {
        if (!string.IsNullOrEmpty(poi.Name)) return poi.Name;
        if (primary != null && !string.IsNullOrEmpty(primary.Name)) return primary.Name;
        return $"POI {poi.Id}";
    }

    private static string StatusLine(POIData poi, POIEntity? primary)
    {
        if (primary != null)
        {
            return primary.Kind switch
            {
                POIEntityKind.Colony      => "colony · active",
                POIEntityKind.Outpost     => "outpost · extracting",
                POIEntityKind.Station     => "station · online",
                POIEntityKind.SalvageSite => "unscanned",
                POIEntityKind.Fleet       => "fleet · moored",
                _                         => "—",
            };
        }
        return poi.Type switch
        {
            POIType.HabitablePlanet  => "unclaimed",
            POIType.BarrenPlanet     => "unclaimed",
            POIType.AsteroidField    => "unexploited",
            POIType.DebrisField      => "unexploited",
            POIType.ShipGraveyard    => "unscanned",
            POIType.AbandonedStation => "unscanned",
            POIType.Megastructure    => "unscanned",
            _                        => "—",
        };
    }

    private static Color AccentFor(POIData poi, POIEntity? primary)
    {
        if (primary != null)
        {
            return primary.Kind switch
            {
                POIEntityKind.Colony      => new Color("#22dd44"),
                POIEntityKind.Outpost     => new Color("#ddaa22"),
                POIEntityKind.Station     => UIColors.SensorIcon,
                POIEntityKind.SalvageSite => new Color("#ff5540"),
                POIEntityKind.Fleet       => new Color("#22dd44"),
                _                         => UIColors.TextDim,
            };
        }
        return poi.Type switch
        {
            POIType.AsteroidField     => new Color("#ddaa22"),
            POIType.DebrisField       => new Color("#ff5540"),
            POIType.ShipGraveyard     => new Color("#ff5540"),
            POIType.AbandonedStation  => new Color("#ff5540"),
            POIType.Megastructure     => new Color("#b366e8"),
            _                         => UIColors.TextDim,
        };
    }
}
