using Godot;
using System.Collections.Generic;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.Units;

/// <summary>
/// Visual fleet icon on the galaxy map. Contains click area, selection highlight,
/// and label. Updated each frame based on FleetMovementSystem position.
/// </summary>
public partial class FleetNode : Node3D
{
    public FleetData FleetData { get; private set; } = null!;

    private MeshInstance3D _icon = null!;
    private MeshInstance3D _selectionRing = null!;
    private Area3D _clickArea = null!;
    private Label3D _label = null!;
    private bool _selected;

    private static readonly Color PlayerColor = new(0.2f, 0.8f, 1.0f);
    private static readonly Color AIColor = new(0.8f, 0.3f, 0.3f);
    private static readonly Color SelectedColor = new(1.0f, 1.0f, 0.3f);

    public void Initialize(FleetData data, bool isPlayerFleet)
    {
        FleetData = data;
        Name = $"Fleet_{data.Id}";

        // Colored capsule placeholder — elongated along X so it reads as a ship from above.
        // CapsuleMesh is natively oriented along Y; rotate 90° on Z to lay it flat.
        _icon = new MeshInstance3D();
        _icon.Mesh = new CapsuleMesh { Radius = 0.5f, Height = 2.5f };
        _icon.RotationDegrees = new Vector3(0f, 0f, 90f);

        var mat = new StandardMaterial3D
        {
            AlbedoColor = isPlayerFleet ? PlayerColor : AIColor,
            EmissionEnabled = true,
            Emission = isPlayerFleet ? PlayerColor : AIColor,
            EmissionEnergyMultiplier = 1.5f,
        };
        _icon.MaterialOverride = mat;
        AddChild(_icon);

        // Selection ring (hidden by default)
        _selectionRing = new MeshInstance3D();
        var ring = new TorusMesh();
        ring.InnerRadius = 1.8f;
        ring.OuterRadius = 2.5f;
        ring.Rings = 16;
        ring.RingSegments = 24;
        _selectionRing.Mesh = ring;
        _selectionRing.RotationDegrees = new Vector3(0, 0, 0);

        var ringMat = new StandardMaterial3D();
        ringMat.AlbedoColor = SelectedColor;
        ringMat.EmissionEnabled = true;
        ringMat.Emission = SelectedColor;
        ringMat.EmissionEnergyMultiplier = 2f;
        ringMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        ringMat.AlbedoColor = new Color(SelectedColor, 0.7f);
        _selectionRing.MaterialOverride = ringMat;
        _selectionRing.Visible = false;
        AddChild(_selectionRing);

        // Click area — radius tuned so it doesn't overlap the star's 3.0 collision sphere
        // once the fleet is offset from the star center (see UpdatePosition).
        _clickArea = new Area3D();
        var shape = new SphereShape3D();
        shape.Radius = 1.5f;
        var col = new CollisionShape3D();
        col.Shape = shape;
        _clickArea.AddChild(col);
        _clickArea.InputEvent += OnInputEvent;
        AddChild(_clickArea);

        // Label
        _label = new Label3D();
        _label.Text = data.Name;
        _label.FontSize = 32;
        _label.Position = new Vector3(0, 4f, 0);
        _label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        _label.Modulate = new Color(1, 1, 1, 0.8f);
        AddChild(_label);
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        _selectionRing.Visible = selected;
    }

    /// <summary>World-space offset from a system's center for a docked fleet, so the
    /// fleet icon and the star underneath are both individually clickable.</summary>
    private static readonly Vector3 DockedOffset = new(3f, 1.5f, -3f);

    public void UpdatePosition(float x, float z)
    {
        // Docked fleets have CurrentSystemId >= 0; in-transit fleets have -1 and ride
        // the lane interpolation without any offset.
        var basePos = new Vector3(x, 1.5f, z);
        Position = FleetData.CurrentSystemId >= 0 ? basePos + new Vector3(DockedOffset.X, 0, DockedOffset.Z) : basePos;
    }

    /// <summary>Update label to show ship count.</summary>
    public void UpdateLabel()
    {
        _label.Text = $"{FleetData.Name} ({FleetData.ShipIds.Count})";
    }

    private void OnInputEvent(Node camera, InputEvent @event, Vector3 pos, Vector3 normal, long shapeIdx)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.CtrlPressed)
                EventBus.Instance?.FireFleetSelectionToggled(FleetData.Id);
            else
                EventBus.Instance?.FireFleetSelected(FleetData.Id);
            GetViewport().SetInputAsHandled();
        }
    }

}
