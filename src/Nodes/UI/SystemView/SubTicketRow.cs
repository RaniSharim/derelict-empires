using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// One row inside a shared POI card, representing a single entity moored to the POI.
/// 24px tall, ownership accent, status dot, label, optional resolution chip, right chevron.
/// Layout: scenes/ui/sub_ticket_row.tscn. Configured via <see cref="Configure"/>.
/// Clicking fires EntitySelected without disturbing the Selected POI. See design/in_system_design.md §5.2.
/// </summary>
public partial class SubTicketRow : HBoxContainer
{
    public static readonly PackedScene Scene =
        GD.Load<PackedScene>("res://scenes/ui/sub_ticket_row.tscn");

    [Export] private ColorRect _accent = null!;
    [Export] private ColorRect _dot = null!;
    [Export] private Label _label = null!;
    [Export] private Label _resolutionChip = null!;
    [Export] private Label _chevron = null!;

    public POIEntity Entity { get; private set; } = null!;
    public int ViewerEmpireId { get; private set; }
    public int PoiId { get; private set; }

    public override void _Ready()
    {
        UIFonts.Style(_label, UIFonts.Main, 10, UIColors.TextLabel);
        UIFonts.Style(_resolutionChip, UIFonts.Main, 9, UIColors.TextDim);
        UIFonts.Style(_chevron, UIFonts.Main, 10, UIColors.TextDim);
        GuiInput += OnGuiInput;
    }

    /// <summary>Bind data + style to the row. Safe to call before or after the node enters the tree.</summary>
    public void Configure(POIEntity entity, int viewerEmpireId, int poiId)
    {
        Entity = entity;
        ViewerEmpireId = viewerEmpireId;
        PoiId = poiId;

        var ownerColor = OwnerTint(entity.OwnerEmpireId, viewerEmpireId);
        _accent.Color = ownerColor;
        _dot.Color = ownerColor;

        bool isForeign = entity.OwnerEmpireId >= 0 && entity.OwnerEmpireId != viewerEmpireId;
        _label.Text = (isForeign && entity.Resolution == ResolutionTier.Silhouette)
            ? "? contact ?"
            : (entity.Name.Length > 0 ? entity.Name : entity.Kind.ToString());

        _resolutionChip.Visible = isForeign;
        if (isForeign) _resolutionChip.Text = ResolutionChip(entity.Resolution);
    }

    private void OnGuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            EventBus.Instance?.FireEntitySelected(Entity.Kind.ToString(), Entity.Id, PoiId);
            AcceptEvent();
        }
    }

    private static Color OwnerTint(int ownerId, int viewerId)
    {
        if (ownerId < 0) return UIColors.TextDim;
        if (ownerId == viewerId) return new Color("#55ccee");   // OwnerYou per spec §13.2
        return UIColors.AccentRed;                              // foreign — warning red
    }

    private static string ResolutionChip(ResolutionTier tier) => tier switch
    {
        ResolutionTier.Silhouette => "[silh]",
        ResolutionTier.Type       => "[type]",
        ResolutionTier.Id         => "[id]",
        _                         => "[?]",
    };
}
