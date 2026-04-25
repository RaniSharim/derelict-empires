using Godot;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Ships;

namespace DerlictEmpires.Nodes.UI.ShipDesigner;

/// <summary>
/// Left pane of the ShipDesigner: shows the current chassis — name, size/variant, slot count,
/// free capacity, base HP/speed/visibility — and the [CHANGE CHASSIS] button that opens the picker.
/// </summary>
public partial class ChassisPane : PanelContainer
{
    private readonly ShipDesignerOverlay _overlay;

    private Label _titleLabel = null!;
    private Label _classLabel = null!;
    private Label _slotsLabel = null!;
    private Label _capacityLabel = null!;
    private Label _hpLabel = null!;
    private Label _speedLabel = null!;
    private Label _visLabel = null!;
    private Label _maintLabel = null!;
    private Control _thumbnail = null!;

    public ChassisPane(ShipDesignerOverlay overlay)
    {
        _overlay = overlay;
    }

    public override void _Ready()
    {
        var bg = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.25f), BorderColor = UIColors.BorderDim };
        bg.SetBorderWidthAll(1);
        bg.SetCornerRadiusAll(0);
        AddThemeStyleboxOverride("panel", bg);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        AddChild(margin);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 10);
        margin.AddChild(col);

        var header = new Label { Text = "CHASSIS" };
        UIFonts.StyleRole(header, UIFonts.Role.UILabel);
        col.AddChild(header);

        _titleLabel = new Label();
        UIFonts.StyleRole(_titleLabel, UIFonts.Role.TitleMedium);
        _titleLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        col.AddChild(_titleLabel);

        _classLabel = new Label();
        UIFonts.StyleRole(_classLabel, UIFonts.Role.BodySecondary);
        col.AddChild(_classLabel);

        _thumbnail = BuildThumbnail();
        col.AddChild(_thumbnail);

        col.AddChild(BuildStatRow("SLOTS",   out _slotsLabel));
        col.AddChild(BuildStatRow("CAPACITY", out _capacityLabel));
        col.AddChild(BuildStatRow("BASE HP",  out _hpLabel));
        col.AddChild(BuildStatRow("SPEED",    out _speedLabel));
        col.AddChild(BuildStatRow("VISIBILITY", out _visLabel));
        col.AddChild(BuildStatRow("MAINT.",   out _maintLabel));

        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });

        var changeBtn = new Button { Text = "CHANGE CHASSIS" };
        changeBtn.CustomMinimumSize = new Vector2(0, 34);
        UIFonts.StyleButtonRole(changeBtn, UIFonts.Role.UILabel);
        GlassPanel.StyleButton(changeBtn);
        changeBtn.Pressed += OpenPicker;
        col.AddChild(changeBtn);

        Refresh();
    }

    public void Refresh()
    {
        var chassis = _overlay.Draft.GetChassis();
        if (chassis == null) return;

        _titleLabel.Text = chassis.DisplayName.ToUpperInvariant();
        _classLabel.Text = $"{chassis.SizeClass.ToString().ToUpperInvariant()} · {chassis.Variant.ToUpperInvariant()}";
        _slotsLabel.Text = chassis.BigSystemSlots.ToString();
        _capacityLabel.Text = chassis.FreeCapacity.ToString();
        _hpLabel.Text = chassis.BaseHp.ToString();
        _speedLabel.Text = chassis.BaseSpeed.ToString("0.#");
        _visLabel.Text = chassis.BaseVisibility.ToString("0.#");
        _maintLabel.Text = chassis.MaintenanceCost.ToString();
        _thumbnail.QueueRedraw();
    }

    private Control BuildStatRow(string label, out Label valueLabel)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var keyLabel = new Label { Text = label };
        UIFonts.StyleRole(keyLabel, UIFonts.Role.UILabel);
        keyLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(keyLabel);

        valueLabel = new Label { Text = "—" };
        UIFonts.StyleRole(valueLabel, UIFonts.Role.DataLarge, UIColors.TextBright);
        row.AddChild(valueLabel);
        return row;
    }

    private Control BuildThumbnail()
    {
        var frame = new PanelContainer();
        frame.CustomMinimumSize = new Vector2(0, 120);

        var frameStyle = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0.35f),
            BorderColor = UIColors.BorderDim,
        };
        frameStyle.SetBorderWidthAll(1);
        frameStyle.SetCornerRadiusAll(0);
        frame.AddThemeStyleboxOverride("panel", frameStyle);

        var draw = new ChassisSilhouette(_overlay);
        draw.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        draw.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        frame.AddChild(draw);
        return frame;
    }

    private void OpenPicker()
    {
        var picker = new ChassisPickerSubOverlay { Name = "ChassisPicker" };
        picker.Configure(_overlay);
        // Mount on the same CanvasLayer as the designer so it stacks above it.
        _overlay.OverlayHost.AddChild(picker);
    }
}

/// <summary>
/// Simple shape-based chassis silhouette — class-accurate proportions without art asset dependency.
/// Triangle for light/fast, rectangle for mid, hexagon for heavy, diamond for capital.
/// </summary>
public partial class ChassisSilhouette : Control
{
    private readonly ShipDesignerOverlay _overlay;

    public ChassisSilhouette(ShipDesignerOverlay overlay)
    {
        _overlay = overlay;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        var chassis = _overlay.Draft.GetChassis();
        if (chassis == null) return;

        float w = Size.X;
        float h = Size.Y;
        var center = new Vector2(w / 2f, h / 2f);

        var color = UIColors.GetFactionGlow(_overlay.EmpireAffinity ?? PrecursorColor.Red);
        var fill = new Color(color.R, color.G, color.B, 0.25f);
        var stroke = new Color(color.R, color.G, color.B, 0.85f);

        float scale = chassis.SizeClass switch
        {
            ShipSizeClass.Fighter    => 0.30f,
            ShipSizeClass.Corvette   => 0.40f,
            ShipSizeClass.Frigate    => 0.52f,
            ShipSizeClass.Destroyer  => 0.62f,
            ShipSizeClass.Cruiser    => 0.72f,
            ShipSizeClass.Battleship => 0.82f,
            ShipSizeClass.Titan      => 0.92f,
            _ => 0.5f
        };
        float r = Mathf.Min(w, h) * 0.42f * scale;

        Vector2[] poly = chassis.SizeClass switch
        {
            ShipSizeClass.Fighter => new[]
            {
                center + new Vector2(0, -r),
                center + new Vector2(r * 0.85f, r * 0.7f),
                center + new Vector2(-r * 0.85f, r * 0.7f),
            },
            ShipSizeClass.Corvette => new[]
            {
                center + new Vector2(0, -r),
                center + new Vector2(r * 0.5f, 0),
                center + new Vector2(r * 0.35f, r * 0.8f),
                center + new Vector2(-r * 0.35f, r * 0.8f),
                center + new Vector2(-r * 0.5f, 0),
            },
            ShipSizeClass.Titan or ShipSizeClass.Battleship => BuildHex(center, r),
            _ => new[]
            {
                center + new Vector2(-r * 0.5f, -r),
                center + new Vector2(r * 0.5f, -r),
                center + new Vector2(r * 0.9f, r * 0.3f),
                center + new Vector2(r * 0.4f, r),
                center + new Vector2(-r * 0.4f, r),
                center + new Vector2(-r * 0.9f, r * 0.3f),
            }
        };

        DrawPolygon(poly, new[] { fill });
        DrawPolyline(ClosePolygon(poly), stroke, 1.5f, antialiased: true);
    }

    private static Vector2[] BuildHex(Vector2 c, float r)
    {
        var pts = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float ang = Mathf.Pi / 6f + i * Mathf.Pi / 3f;
            pts[i] = c + new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
        }
        return pts;
    }

    private static Vector2[] ClosePolygon(Vector2[] src)
    {
        var dst = new Vector2[src.Length + 1];
        System.Array.Copy(src, dst, src.Length);
        dst[src.Length] = src[0];
        return dst;
    }
}
