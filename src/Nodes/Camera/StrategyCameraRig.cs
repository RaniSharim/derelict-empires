using Godot;
using System;

namespace DerlictEmpires.Nodes.Camera;

/// <summary>
/// Strategy game camera with pan (WASD/middle-drag/edge), zoom (scroll), and rotate (Q/E).
/// The rig is a Node3D that moves on the XZ plane; Camera3D is a child looking down.
/// </summary>
public partial class StrategyCameraRig : Node3D
{
    [Export] public float PanSpeed { get; set; } = 40f;
    [Export] public float ZoomSpeed { get; set; } = 5f;
    [Export] public float MinZoom { get; set; } = 15f;
    [Export] public float MaxZoom { get; set; } = 250f;
    [Export] public float RotateSpeed { get; set; } = 1.5f;
    [Export] public float EdgePanMargin { get; set; } = 20f;
    [Export] public float LerpSpeed { get; set; } = 8f;

    private Camera3D _camera = null!;
    private float _currentZoom = 80f;
    private float _targetZoom = 80f;
    private Vector3 _targetPosition;
    private float _targetRotationY;
    private bool _middleMouseDragging;

    public Camera3D Camera => _camera;

    public override void _Ready()
    {
        _targetPosition = Position;
        _targetRotationY = Rotation.Y;
    }

    private void EnsureCamera()
    {
        if (_camera != null) return;
        _camera = GetNodeOrNull<Camera3D>("Camera3D");
        if (_camera != null)
        {
            _currentZoom = _camera.Position.Y;
            _targetZoom = _currentZoom;
        }
    }

    public override void _Process(double delta)
    {
        EnsureCamera();
        if (_camera == null) return;

        float dt = (float)delta;

        HandleKeyboardPan(dt);
        HandleEdgePan(dt);

        // Smooth interpolation
        Position = Position.Lerp(_targetPosition, dt * LerpSpeed);

        var rot = Rotation;
        rot.Y = Mathf.LerpAngle(rot.Y, _targetRotationY, dt * LerpSpeed);
        Rotation = rot;

        _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, dt * LerpSpeed);
        var camPos = _camera.Position;
        camPos.Y = _currentZoom;
        // Tilt camera based on zoom level — more top-down when zoomed out
        float tiltAngle = Mathf.Lerp(-45f, -80f, (_currentZoom - MinZoom) / (MaxZoom - MinZoom));
        camPos.Z = _currentZoom * 0.3f; // Offset back as we zoom out
        _camera.Position = camPos;
        _camera.RotationDegrees = new Vector3(tiltAngle, 0, 0);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_camera == null) return;
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp)
            {
                _targetZoom = Mathf.Clamp(_targetZoom - ZoomSpeed * (_targetZoom * 0.1f), MinZoom, MaxZoom);
                GetViewport().SetInputAsHandled();
            }
            else if (mb.ButtonIndex == MouseButton.WheelDown)
            {
                _targetZoom = Mathf.Clamp(_targetZoom + ZoomSpeed * (_targetZoom * 0.1f), MinZoom, MaxZoom);
                GetViewport().SetInputAsHandled();
            }
            else if (mb.ButtonIndex == MouseButton.Middle)
            {
                _middleMouseDragging = mb.Pressed;
            }
        }

        if (@event is InputEventMouseMotion mm && _middleMouseDragging)
        {
            // Pan with middle mouse drag — in camera-relative direction
            float scale = _currentZoom * 0.005f;
            var forward = -GlobalTransform.Basis.Z;
            var right = GlobalTransform.Basis.X;
            // Project to XZ plane
            forward.Y = 0;
            right.Y = 0;
            forward = forward.Normalized();
            right = right.Normalized();

            _targetPosition += right * (-mm.Relative.X * scale);
            _targetPosition += forward * (mm.Relative.Y * scale);
            GetViewport().SetInputAsHandled();
        }

        // Q/E rotation
        if (@event is InputEventKey key && key.Pressed)
        {
            if (key.Keycode == Key.Q)
                _targetRotationY += RotateSpeed * 0.5f;
            else if (key.Keycode == Key.E)
                _targetRotationY -= RotateSpeed * 0.5f;
        }
    }

    private void HandleKeyboardPan(float delta)
    {
        var pan = Vector3.Zero;
        if (Input.IsActionPressed("camera_left")) pan.X -= 1;
        if (Input.IsActionPressed("camera_right")) pan.X += 1;
        if (Input.IsActionPressed("camera_up")) pan.Z += 1;
        if (Input.IsActionPressed("camera_down")) pan.Z -= 1;

        if (pan.LengthSquared() > 0)
        {
            pan = pan.Normalized();
            // Transform pan direction by camera rotation
            var forward = -GlobalTransform.Basis.Z;
            var right = GlobalTransform.Basis.X;
            forward.Y = 0;
            right.Y = 0;
            forward = forward.Normalized();
            right = right.Normalized();

            float speed = PanSpeed * (_currentZoom / 50f); // Scale with zoom
            _targetPosition += (right * pan.X + forward * pan.Z) * speed * delta;
        }
    }

    private void HandleEdgePan(float delta)
    {
        // Suppress edge panning while drag-panning
        if (_middleMouseDragging)
            return;

        var mousePos = GetViewport().GetMousePosition();
        var viewSize = GetViewport().GetVisibleRect().Size;
        var pan = Vector3.Zero;

        if (mousePos.X < EdgePanMargin) pan.X -= 1;
        if (mousePos.X > viewSize.X - EdgePanMargin) pan.X += 1;
        if (mousePos.Y < EdgePanMargin) pan.Z += 1;
        if (mousePos.Y > viewSize.Y - EdgePanMargin) pan.Z -= 1;

        if (pan.LengthSquared() > 0)
        {
            var forward = -GlobalTransform.Basis.Z;
            var right = GlobalTransform.Basis.X;
            forward.Y = 0;
            right.Y = 0;
            forward = forward.Normalized();
            right = right.Normalized();

            float speed = PanSpeed * 0.5f * (_currentZoom / 50f);
            _targetPosition += (right * pan.X + forward * pan.Z) * speed * delta;
        }
    }

    /// <summary>Raycast from screen point to the Y=0 plane.</summary>
    public Vector3? ScreenToWorld(Vector2 screenPos)
    {
        if (_camera == null) return null;
        var from = _camera.ProjectRayOrigin(screenPos);
        var dir = _camera.ProjectRayNormal(screenPos);
        if (Mathf.Abs(dir.Y) < 0.0001f) return null;
        float t = -from.Y / dir.Y;
        if (t < 0) return null;
        return from + dir * t;
    }
}
