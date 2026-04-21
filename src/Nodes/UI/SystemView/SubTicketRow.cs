using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// One row inside a shared POI card, representing a single entity moored to the POI.
/// 24px tall, ownership accent, status dot, label, right chevron. Clicking fires
/// EntitySelected without disturbing the Selected POI. See design/in_system_design.md §5.2.
/// </summary>
public partial class SubTicketRow : HBoxContainer
{
    public POIEntity Entity { get; }
    public int ViewerEmpireId { get; }
    public int PoiId { get; }

    public SubTicketRow(POIEntity entity, int viewerEmpireId, int poiId)
    {
        Entity = entity;
        ViewerEmpireId = viewerEmpireId;
        PoiId = poiId;
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, 24);
        AddThemeConstantOverride("separation", 6);
        MouseFilter = MouseFilterEnum.Stop;

        var ownerColor = OwnerTint(Entity.OwnerEmpireId, ViewerEmpireId);

        // 2px ownership accent.
        var accent = new ColorRect
        {
            Color = ownerColor,
            CustomMinimumSize = new Vector2(2, 0),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        accent.SizeFlagsVertical = SizeFlags.Fill;
        AddChild(accent);

        // 8px ownership dot.
        var dot = new ColorRect
        {
            Color = ownerColor,
            CustomMinimumSize = new Vector2(8, 8),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        dot.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        AddChild(dot);

        // Label — silhouette hides the name.
        bool isForeign = Entity.OwnerEmpireId >= 0 && Entity.OwnerEmpireId != ViewerEmpireId;
        string labelText = (isForeign && Entity.Resolution == ResolutionTier.Silhouette)
            ? "? contact ?"
            : (Entity.Name.Length > 0 ? Entity.Name : Entity.Kind.ToString());
        var label = new Label { Text = labelText, ClipText = true };
        UIFonts.Style(label, UIFonts.Main, 10, UIColors.TextLabel);
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddChild(label);

        // Resolution chip for foreign entities — tracks silhouette/type/id per spec §6.5.
        if (isForeign)
        {
            var chip = new Label { Text = ResolutionChip(Entity.Resolution) };
            UIFonts.Style(chip, UIFonts.Main, 9, UIColors.TextDim);
            AddChild(chip);
        }

        // Chevron affordance.
        var chev = new Label { Text = "▸" };
        UIFonts.Style(chev, UIFonts.Main, 10, UIColors.TextDim);
        AddChild(chev);

        GuiInput += OnGuiInput;
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
