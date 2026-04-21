using System.Collections.Generic;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// Horizontal tab strip shown when the Selected POI is shared. Each tab represents one entity
/// at the POI; clicking a tab fires EntitySelected. See design/in_system_design.md §10.
/// Layout uses an HBox with a ScrollContainer for 5+ tabs (horizontal overflow per spec).
/// </summary>
public partial class EntityTabStrip : PanelContainer
{
    private HBoxContainer _row = null!;
    private int _selectedEntityId = -1;

    public override void _Ready()
    {
        var bg = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0),
            BorderColor = UIColors.BorderDim,
            BorderWidthBottom = 1,
        };
        AddThemeStyleboxOverride("panel", bg);

        var scroll = new ScrollContainer();
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Auto;
        scroll.VerticalScrollMode   = ScrollContainer.ScrollMode.Disabled;
        scroll.SizeFlagsHorizontal  = SizeFlags.ExpandFill;
        AddChild(scroll);

        _row = new HBoxContainer();
        _row.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_row);
    }

    public void Populate(IReadOnlyList<POIEntity> entities, int viewerEmpireId, int poiId, int selectedEntityId)
    {
        _selectedEntityId = selectedEntityId;
        foreach (var c in _row.GetChildren()) c.QueueFree();

        foreach (var entity in entities)
        {
            var tab = new Button
            {
                Text = TabLabel(entity, viewerEmpireId),
                Flat = true,
                ToggleMode = true,
                ButtonPressed = entity.Id == selectedEntityId,
            };
            UIFonts.StyleButtonRole(tab, UIFonts.Role.Small,
                entity.OwnerEmpireId == viewerEmpireId ? new Color("#55ccee") : UIColors.AccentRed);

            var captured = entity;
            tab.Pressed += () => EventBus.Instance?.FireEntitySelected(captured.Kind.ToString(), captured.Id, poiId);
            _row.AddChild(tab);
        }
    }

    private static string TabLabel(POIEntity entity, int viewerEmpireId)
    {
        string owner = entity.OwnerEmpireId == viewerEmpireId ? "YOU" : $"EMP{entity.OwnerEmpireId}";
        string suffix = entity.OwnerEmpireId == viewerEmpireId ? "" : " · ID";
        return $"{owner}{suffix}  {entity.Kind}";
    }
}
