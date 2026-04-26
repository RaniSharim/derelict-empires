using System.Collections.Generic;
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
    public IReadOnlyList<POIEntity> Entities { get; private set; }
    public int ViewerEmpireId { get; private set; }

    private bool _isSelected;
    private bool _isHovered;

    private StyleBoxFlat _idleStyle = null!;
    private StyleBoxFlat _hoverStyle = null!;
    private StyleBoxFlat _selectedStyle = null!;
    private ColorRect _accentBar = null!;
    private ColorRect? _accentBarSecondary;

    public POICard(POIData poi, POIEntity? primary)
        : this(poi, primary, System.Array.Empty<POIEntity>(), viewerEmpireId: -1) { }

    public POICard(POIData poi, POIEntity? primary, IReadOnlyList<POIEntity> entities, int viewerEmpireId)
    {
        Poi = poi;
        Primary = primary;
        Entities = entities;
        ViewerEmpireId = viewerEmpireId;
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

        // Single- or split-gradient accent bar. Shared POIs with a foreign entity get a two-tone
        // stripe: top half owner color, bottom half foreign color (spec §4.3).
        var accentStack = new VBoxContainer();
        accentStack.CustomMinimumSize = new Vector2(3, 0);
        accentStack.AddThemeConstantOverride("separation", 0);
        accentStack.SizeFlagsVertical = SizeFlags.Fill;
        accentStack.MouseFilter = MouseFilterEnum.Ignore;
        h.AddChild(accentStack);

        bool shared = Entities.Count > 1;
        Color topColor    = AccentFor(Poi, Primary);
        Color bottomColor = shared ? ForeignAccent() : topColor;

        _accentBar = new ColorRect { Color = topColor, MouseFilter = MouseFilterEnum.Ignore };
        _accentBar.SizeFlagsVertical = SizeFlags.ExpandFill;
        accentStack.AddChild(_accentBar);

        if (shared && !bottomColor.Equals(topColor))
        {
            _accentBarSecondary = new ColorRect { Color = bottomColor, MouseFilter = MouseFilterEnum.Ignore };
            _accentBarSecondary.SizeFlagsVertical = SizeFlags.ExpandFill;
            accentStack.AddChild(_accentBarSecondary);
        }

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
        // Coarse resolution: approximate prefix per spec §6.5.
        bool coarseSig = Primary != null
                        && Primary.OwnerEmpireId >= 0
                        && Primary.OwnerEmpireId != ViewerEmpireId
                        && Primary.Resolution != ResolutionTier.Id;
        header.AddChild(DetectionGlyph.CreateLabel(
            DetectionGlyph.Kind.Signature, 11,
            coarseSig ? $"~{sig}" : sig.ToString()));

        // Name — Exo 2 12px ALL-CAPS per spec §4.2.
        var name = new Label { Text = DisplayName(Poi, Primary).ToUpperInvariant(), ClipText = true };
        UIFonts.Style(name, UIFonts.Title, UIFonts.SmallSize, UIColors.TextLabel);
        v.AddChild(name);

        // Status line.
        var status = new Label { Text = StatusLine(Poi, Primary) };
        UIFonts.Style(status, UIFonts.Main, 10, UIColors.TextDim);
        v.AddChild(status);

        // Sub-ticket list for shared POIs. Spec §5: self entities first, then foreign.
        if (shared)
        {
            var divider = new ColorRect
            {
                Color = UIColors.BorderDim,
                CustomMinimumSize = new Vector2(0, 1),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            v.AddChild(divider);

            foreach (var entity in OrderSubTickets(Entities, ViewerEmpireId))
            {
                var row = SubTicketRow.Scene.Instantiate<SubTicketRow>();
                v.AddChild(row);
                row.Configure(entity, ViewerEmpireId, Poi.Id);
            }
        }

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
            // Shared POIs: card click selects the POI but leaves Entity alone — per-entity
            // selection happens via SubTicketRow clicks. Single-entity POIs set both at once.
            bool shared = Entities.Count > 1;
            if (!shared)
            {
                if (Primary != null)
                    EventBus.Instance?.FireEntitySelected(Primary.Kind.ToString(), Primary.Id, Poi.Id);
                else
                    EventBus.Instance?.FireEntityDeselected();
            }
            AcceptEvent();
        }
    }

    private static IEnumerable<POIEntity> OrderSubTickets(IReadOnlyList<POIEntity> entities, int viewerEmpireId)
    {
        // Self-owned first, then foreign grouped by empire id.
        foreach (var e in entities)
            if (e.OwnerEmpireId == viewerEmpireId) yield return e;
        int? lastOwner = null;
        foreach (var e in entities)
        {
            if (e.OwnerEmpireId == viewerEmpireId) continue;
            if (lastOwner != null && lastOwner != e.OwnerEmpireId) { /* grouping marker could go here */ }
            lastOwner = e.OwnerEmpireId;
            yield return e;
        }
    }

    private Color ForeignAccent()
    {
        foreach (var e in Entities)
            if (e.OwnerEmpireId >= 0 && e.OwnerEmpireId != ViewerEmpireId)
                return UIColors.AccentRed;
        return AccentFor(Poi, Primary);
    }

    private string TypeTag(POIData poi)
    {
        // Silhouette override when primary is an unresolved foreign — card masks its type.
        if (Primary != null
            && Primary.OwnerEmpireId >= 0
            && Primary.OwnerEmpireId != ViewerEmpireId
            && Primary.Resolution == ResolutionTier.Silhouette)
        {
            return "UNKNOWN · SILH";
        }
        return poi.Type switch
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
    }

    private string DisplayName(POIData poi, POIEntity? primary)
    {
        // Silhouette masks the entity name.
        if (primary != null
            && primary.OwnerEmpireId >= 0
            && primary.OwnerEmpireId != ViewerEmpireId
            && primary.Resolution == ResolutionTier.Silhouette)
        {
            return "? contact ?";
        }
        if (!string.IsNullOrEmpty(poi.Name)) return poi.Name;
        if (primary != null && !string.IsNullOrEmpty(primary.Name)) return primary.Name;
        return $"POI {poi.Id}";
    }

    private string StatusLine(POIData poi, POIEntity? primary)
    {
        if (primary != null
            && primary.OwnerEmpireId >= 0
            && primary.OwnerEmpireId != ViewerEmpireId
            && primary.Resolution == ResolutionTier.Silhouette)
        {
            string magnitude = primary.Signature > 60 ? "large sig" :
                               primary.Signature > 20 ? "mod sig"   : "quiet";
            return $"{magnitude} · unknown";
        }
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
