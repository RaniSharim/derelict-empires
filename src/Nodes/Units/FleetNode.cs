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

        // Diamond-shaped icon mesh
        _icon = new MeshInstance3D();
        var mesh = new PrismMesh();
        mesh.Size = new Vector3(2.5f, 2.5f, 2.5f);
        _icon.Mesh = mesh;
        _icon.RotationDegrees = new Vector3(0, 0, 0);

        var mat = new StandardMaterial3D();
        mat.AlbedoColor = isPlayerFleet ? PlayerColor : AIColor;
        mat.EmissionEnabled = true;
        mat.Emission = isPlayerFleet ? PlayerColor : AIColor;
        mat.EmissionEnergyMultiplier = 1.5f;
        _icon.MaterialOverride = mat;
        AddChild(_icon);

        // Selection ring (hidden by default)
        _selectionRing = new MeshInstance3D();
        var ring = new TorusMesh();
        ring.InnerRadius = 2.5f;
        ring.OuterRadius = 3.5f;
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

        // Click area
        _clickArea = new Area3D();
        var shape = new SphereShape3D();
        shape.Radius = 4.0f;
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

    public void UpdatePosition(float x, float z)
    {
        Position = new Vector3(x, 1.5f, z);  // Slightly above the star plane
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
            EventBus.Instance?.FireFleetSelected(FleetData.Id);
            GetViewport().SetInputAsHandled();
        }
    }
}
