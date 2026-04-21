using System.Collections.Generic;
using Godot;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// One of the three horizontal bands (Inner / Mid / Outer) in the System View.
/// Header (name, quality, coverage) + tinted body where POI cards stack.
/// Spec: design/in_system_design.md §3.
/// </summary>
public partial class BandRow : VBoxContainer
{
    public Band Band { get; }

    private HBoxContainer? _body;
    private readonly Dictionary<int, POICard> _cardsByPoiId = new();
    private Label? _coverageLabel;
    private Label? _coverageSourceLabel;

    public BandRow(Band band)
    {
        Band = band;
        Name = $"{band}Band";
    }

    public override void _Ready()
    {
        SizeFlagsVertical   = SizeFlags.ExpandFill;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddThemeConstantOverride("separation", 0);

        BuildHeader();

        // Body — tinted backdrop + POI flex container.
        var bodyStack = new PanelContainer { Name = "Body" };
        bodyStack.SizeFlagsVertical   = SizeFlags.ExpandFill;
        bodyStack.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var bodyBg = new StyleBoxFlat
        {
            BgColor          = BodyTint(Band),
            BorderColor      = BorderTint(Band),
            BorderWidthBottom = 1,
        };
        bodyStack.AddThemeStyleboxOverride("panel", bodyBg);
        AddChild(bodyStack);

        // Scanline overlay on outer band — simple solid-tint for P1; a proper striped texture
        // can come later. Using a ColorRect with the scanline color at low alpha reads as
        // "darker, with a faint secondary cast" which matches the spec intent for the
        // foundation phase.
        if (Band == Band.Outer)
        {
            var scan = new ColorRect { Color = UIColors.BandOuterScanline };
            scan.SetAnchorsPreset(LayoutPreset.FullRect);
            scan.MouseFilter = MouseFilterEnum.Ignore;
            bodyStack.AddChild(scan);
        }

        var bodyMargin = new MarginContainer();
        bodyMargin.AddThemeConstantOverride("margin_left", 16);
        bodyMargin.AddThemeConstantOverride("margin_right", 16);
        bodyMargin.AddThemeConstantOverride("margin_top", 12);
        bodyMargin.AddThemeConstantOverride("margin_bottom", 12);
        bodyStack.AddChild(bodyMargin);

        _body = new HBoxContainer { Name = "PoiFlex" };
        _body.AddThemeConstantOverride("separation", 14);
        _body.Alignment = BoxContainer.AlignmentMode.Begin;
        _body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _body.SizeFlagsVertical   = SizeFlags.ShrinkBegin;
        bodyMargin.AddChild(_body);
    }

    private void BuildHeader()
    {
        var header = new PanelContainer { Name = "Header", CustomMinimumSize = new Vector2(0, 28) };
        var hbg = new StyleBoxFlat
        {
            BgColor          = new Color(0, 0, 0, 0),
            BorderColor      = BorderTint(Band),
            BorderWidthBottom = 1,
        };
        header.AddThemeStyleboxOverride("panel", hbg);
        AddChild(header);

        var h = new HBoxContainer();
        h.AddThemeConstantOverride("separation", 10);
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddChild(h);
        header.AddChild(margin);

        var nameLabel = new Label { Text = BandLabel(Band) };
        UIFonts.Style(nameLabel, UIFonts.Main, UIFonts.SmallSize, BandHeaderTint(Band));
        nameLabel.AddThemeConstantOverride("outline_size", 0);
        h.AddChild(nameLabel);

        var quality = new Label { Text = QualityLabel(Band) };
        UIFonts.Style(quality, UIFonts.Main, UIFonts.SmallSize, UIColors.TextDim);
        h.AddChild(quality);

        // Spacer.
        h.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        // Sensor coverage glyph + number + optional "via {source}" caveat.
        var coverageBox = new HBoxContainer();
        coverageBox.AddThemeConstantOverride("separation", 4);
        coverageBox.AddChild(new DetectionGlyph(DetectionGlyph.Kind.Sensor, 11));
        _coverageLabel = new Label { Text = "COVERAGE 0" };
        UIFonts.Style(_coverageLabel, UIFonts.Main, UIFonts.SmallSize, UIColors.SensorIcon);
        coverageBox.AddChild(_coverageLabel);
        _coverageSourceLabel = new Label { Text = "" };
        UIFonts.Style(_coverageSourceLabel, UIFonts.Main, 10, UIColors.TextFaint);
        coverageBox.AddChild(_coverageSourceLabel);
        h.AddChild(coverageBox);
    }

    /// <summary>Update the COVERAGE readout and "via {source}" caveat from a computed BandCoverage.</summary>
    public void SetCoverage(int coverage, string? soleSource)
    {
        if (_coverageLabel != null) _coverageLabel.Text = $"COVERAGE {coverage}";
        if (_coverageSourceLabel != null)
            _coverageSourceLabel.Text = !string.IsNullOrEmpty(soleSource) ? $"· via {soleSource}" : "";
    }

    private static string BandLabel(Band b) => b switch
    {
        Band.Inner => "INNER",
        Band.Mid   => "MID",
        Band.Outer => "OUTER",
        _          => "?",
    };

    private static string QualityLabel(Band b) => b switch
    {
        Band.Inner => "CLEAR",
        Band.Mid   => "PATCHY",
        Band.Outer => "DARK",
        _          => "?",
    };

    private static Color BodyTint(Band b) => b switch
    {
        Band.Inner => UIColors.BandInnerTint,
        Band.Mid   => UIColors.BandMidTint,
        Band.Outer => UIColors.BandOuterTint,
        _          => UIColors.BandMidTint,
    };

    private static Color BorderTint(Band b) => b switch
    {
        Band.Inner => UIColors.BandInnerBorder,
        Band.Mid   => UIColors.BandMidBorder,
        Band.Outer => UIColors.BandMidBorder,
        _          => UIColors.BandMidBorder,
    };

    private static Color BandHeaderTint(Band b) => b switch
    {
        Band.Inner => UIColors.SensorIcon,      // azure — feels clear
        Band.Mid   => UIColors.TextLabel,
        Band.Outer => UIColors.TextDim,
        _          => UIColors.TextBody,
    };

    /// <summary>Rebuild the card list from the given POIs (already filtered to this band) and their entities.</summary>
    public void Populate(
        IReadOnlyList<POIData> pois,
        System.Func<POIData, IReadOnlyList<POIEntity>> entitiesResolver,
        int viewerEmpireId)
    {
        if (_body == null) return;
        foreach (var child in _cardsByPoiId.Values)
            child.QueueFree();
        _cardsByPoiId.Clear();
        foreach (var child in _body.GetChildren())
            child.QueueFree();

        foreach (var poi in pois)
        {
            var entities = entitiesResolver(poi);
            var primary  = POIContentResolver.Primary((List<POIEntity>)entities, viewerEmpireId);
            var card = new POICard(poi, primary, entities, viewerEmpireId);
            _body.AddChild(card);
            _cardsByPoiId[poi.Id] = card;
        }
    }

    public void SetSelection(int selectedPoiId)
    {
        foreach (var (id, card) in _cardsByPoiId)
            card.SetSelected(id == selectedPoiId);
    }

    /// <summary>Append a non-POI card (eg. unmoored Fleet-POI) to the band body.</summary>
    public void AppendCard(Control card)
    {
        _body?.AddChild(card);
    }
}
