using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Per-system Area3D for click/hover detection on the galaxy map.
/// Lightweight: the visual rendering is handled by StarRenderer (MultiMesh).
/// This node exists for input handling and as a spatial anchor.
/// </summary>
public partial class StarSystemNode : Area3D
{
    public StarSystemData SystemData { get; private set; } = null!;

    private bool _hovered;
    private static StarSystemNode? _selected;

    public static StarSystemNode? CurrentSelected => _selected;

    public void Initialize(StarSystemData data)
    {
        SystemData = data;
        Name = $"System_{data.Id}";
        Position = new Vector3(data.PositionX, 0, data.PositionZ);

        // Create collision shape for click detection
        var shape = new SphereShape3D();
        shape.Radius = 3.0f;
        var collisionShape = new CollisionShape3D();
        collisionShape.Shape = shape;
        AddChild(collisionShape);

        // Connect input signals
        InputEvent += OnInputEvent;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    private void OnInputEvent(Node camera, InputEvent @event, Vector3 position, Vector3 normal, long shapeIdx)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.DoubleClick)
                    EventBus.Instance?.FireSystemDoubleClicked(SystemData);
                else
                    Select();
                GetViewport().SetInputAsHandled();
            }
            else if (mb.ButtonIndex == MouseButton.Right)
            {
                EventBus.Instance?.FireSystemRightClicked(SystemData);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void OnMouseEntered()
    {
        _hovered = true;
        EventBus.Instance?.FireSystemHovered(SystemData);
    }

    private void OnMouseExited()
    {
        _hovered = false;
        EventBus.Instance?.FireSystemUnhovered();
    }

    public void Select()
    {
        if (_selected == this)
        {
            Deselect();
            return;
        }

        _selected?.Deselect();
        _selected = this;
        EventBus.Instance?.FireSystemSelected(SystemData);
    }

    public void Deselect()
    {
        if (_selected == this)
        {
            _selected = null;
            EventBus.Instance?.FireSystemDeselected();
        }
    }
}
