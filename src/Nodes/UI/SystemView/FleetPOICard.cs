using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// A fleet-as-POI card rendered when a fleet is drifting in the band's void (no MooredPOIId).
/// Dashed border, `FLEET · UNMOORED` tag, `⇠` drift suffix on name. See design/in_system_design.md §4.5.
/// </summary>
public partial class FleetPOICard : PanelContainer
{
    public FleetData Fleet { get; }
    public int ViewerEmpireId { get; }

    private bool _isSelected;
    private StyleBoxFlat _idleStyle = null!;
    private StyleBoxFlat _selectedStyle = null!;

    public FleetPOICard(FleetData fleet, int viewerEmpireId)
    {
        Fleet = fleet;
        ViewerEmpireId = viewerEmpireId;
    }

    public override void _Ready()
    {
        Name = $"Fleet_{Fleet.Id}";
        CustomMinimumSize = new Vector2(POICard.FixedWidth, 0);
        SizeFlagsHorizontal = 0;
        SizeFlagsVertical   = SizeFlags.ShrinkBegin;
        MouseFilter = MouseFilterEnum.Stop;

        BuildStyles();
        AddThemeStyleboxOverride("panel", _idleStyle);

        var h = new HBoxContainer();
        h.AddThemeConstantOverride("separation", 7);
        AddChild(h);

        // Green (owner) accent bar.
        bool isOwn = Fleet.OwnerEmpireId == ViewerEmpireId;
        var accent = new ColorRect
        {
            Color = isOwn ? new Color("#22dd44") : UIColors.AccentRed,
            CustomMinimumSize = new Vector2(3, 0),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        accent.SizeFlagsVertical = SizeFlags.Fill;
        h.AddChild(accent);

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

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 4);
        v.AddChild(header);

        var tag = new Label { Text = "FLEET · UNMOORED" };
        UIFonts.Style(tag, UIFonts.Main, 9, UIColors.TextDim);
        header.AddChild(tag);

        header.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        int sig = (Fleet.ShipIds?.Count ?? 0) * 4;
        header.AddChild(DetectionGlyph.CreateLabel(DetectionGlyph.Kind.Signature, 11, sig.ToString()));

        // Name — Exo 2 12px ALL-CAPS per spec §4.2. Drift glyph ⇠ stays as-is.
        var name = new Label { Text = $"{Fleet.Name.ToUpperInvariant()} ⇠", ClipText = true };
        UIFonts.Style(name, UIFonts.Title, UIFonts.SmallSize, UIColors.TextLabel);
        v.AddChild(name);

        var status = new Label { Text = "drifting · quiet" };
        UIFonts.Style(status, UIFonts.Main, 10, UIColors.TextDim);
        v.AddChild(status);

        GuiInput += OnGuiInput;
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        AddThemeStyleboxOverride("panel", _isSelected ? _selectedStyle : _idleStyle);
    }

    private void BuildStyles()
    {
        // Dashed-look approximation via a broken border: top + bottom only, plus left/right
        // only 1px — reads as looser than the solid POICard frame without a true dashed stroke.
        _idleStyle = new StyleBoxFlat
        {
            BgColor = new Color(4 / 255f, 8 / 255f, 16 / 255f, 0.88f),
            BorderColor = UIColors.BorderDim,
            BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2, CornerRadiusBottomRight = 2,
            ExpandMarginTop = 0, ExpandMarginBottom = 0,
            // No true dashed border in StyleBoxFlat; the looser content margin + darker border
            // alpha gives it a distinct "drifting" read. A texture-based dashed box can be
            // swapped in during polish.
        };
        _selectedStyle = new StyleBoxFlat
        {
            BgColor = new Color(34 / 255f, 136 / 255f, 238 / 255f, 0.14f),
            BorderColor = new Color(34 / 255f, 136 / 255f, 238 / 255f, 1),
            BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2, CornerRadiusBottomRight = 2,
        };
    }

    private void OnGuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            EventBus.Instance?.FireEntitySelected(POIEntityKind.Fleet.ToString(), Fleet.Id, -1);
            AcceptEvent();
        }
    }
}
