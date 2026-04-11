using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Bottom-left minimap showing a simplified galaxy overview.
/// 100×100px, tarnished glass (light variant), absolute bottom-left.
/// </summary>
public partial class Minimap : Control
{
    private MinimapCanvas _canvas = null!;

    public override void _Ready()
    {
        // Anchors: bottom-left, 100×100px, 12px from edges
        AnchorLeft = 0;
        AnchorRight = 0;
        AnchorTop = 1;
        AnchorBottom = 1;
        OffsetLeft = 12;
        OffsetRight = 112; // 100px wide
        OffsetTop = -112;
        OffsetBottom = -12;
        ClipContents = true;
        ZIndex = 50;

        // Background with tarnished glass
        var bg = new PanelContainer { Name = "Bg" };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        GlassPanel.Apply(bg, enableBlur: false);
        AddChild(bg);

        // Drawing canvas
        _canvas = new MinimapCanvas { Name = "MinimapCanvas" };
        _canvas.SetAnchorsPreset(LayoutPreset.FullRect);
        _canvas.OffsetLeft = 4;
        _canvas.OffsetRight = -4;
        _canvas.OffsetTop = 4;
        _canvas.OffsetBottom = -4;
        _canvas.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_canvas);
    }
}

/// <summary>
/// Custom draw control that renders a simplified galaxy overview.
/// Shows system dots colored by ownership and 1px lane lines.
/// </summary>
public partial class MinimapCanvas : Control
{
    // Galaxy bounds for mapping world coords to minimap coords
    private float _minX, _maxX, _minZ, _maxZ;
    private GalaxyData? _galaxy;

    public override void _Process(double delta)
    {
        var galaxy = GameManager.Instance?.Galaxy;
        if (galaxy != _galaxy)
        {
            _galaxy = galaxy;
            if (_galaxy != null)
                ComputeBounds();
            QueueRedraw();
        }
    }

    private void ComputeBounds()
    {
        if (_galaxy == null) return;
        _minX = float.MaxValue;
        _maxX = float.MinValue;
        _minZ = float.MaxValue;
        _maxZ = float.MinValue;

        foreach (var sys in _galaxy.Systems)
        {
            if (sys.PositionX < _minX) _minX = sys.PositionX;
            if (sys.PositionX > _maxX) _maxX = sys.PositionX;
            if (sys.PositionZ < _minZ) _minZ = sys.PositionZ;
            if (sys.PositionZ > _maxZ) _maxZ = sys.PositionZ;
        }

        // Add padding
        float padX = (_maxX - _minX) * 0.05f;
        float padZ = (_maxZ - _minZ) * 0.05f;
        _minX -= padX;
        _maxX += padX;
        _minZ -= padZ;
        _maxZ += padZ;
    }

    private Vector2 WorldToMinimap(float wx, float wz)
    {
        float rangeX = _maxX - _minX;
        float rangeZ = _maxZ - _minZ;
        if (rangeX < 1) rangeX = 1;
        if (rangeZ < 1) rangeZ = 1;

        float nx = (wx - _minX) / rangeX;
        float nz = (wz - _minZ) / rangeZ;

        return new Vector2(nx * Size.X, nz * Size.Y);
    }

    public override void _Draw()
    {
        if (_galaxy == null) return;

        // Draw lanes (1px dim lines)
        var laneColor = new Color(UIColors.TextFaint, 0.3f);
        foreach (var lane in _galaxy.Lanes)
        {
            var sysA = _galaxy.GetSystem(lane.SystemA);
            var sysB = _galaxy.GetSystem(lane.SystemB);
            if (sysA == null || sysB == null) continue;
            var from = WorldToMinimap(sysA.PositionX, sysA.PositionZ);
            var to = WorldToMinimap(sysB.PositionX, sysB.PositionZ);
            DrawLine(from, to, laneColor, 1f);
        }

        // Draw system dots (2px colored dots)
        foreach (var sys in _galaxy.Systems)
        {
            var pos = WorldToMinimap(sys.PositionX, sys.PositionZ);
            var color = GetSystemColor(sys);
            DrawCircle(pos, 2f, color);
        }

        // Viewport rectangle (semi-transparent cyan outline)
        // Simple representation — shows center area
        var viewColor = new Color(UIColors.Accent, 0.4f);
        float viewSize = 20f; // simplified view area
        var center = Size / 2f;
        var viewRect = new Rect2(center.X - viewSize / 2, center.Y - viewSize / 2, viewSize, viewSize);
        DrawRect(viewRect, new Color(1f, 1f, 1f, 0.05f), true);
        DrawRect(viewRect, viewColor, false, 1f);
    }

    private static Color GetSystemColor(StarSystemData sys)
    {
        // Check if system has habitable planets (likely colonized)
        bool hasHabitable = false;
        foreach (var poi in sys.POIs)
        {
            if (poi.Type == Core.Enums.POIType.HabitablePlanet)
            {
                hasHabitable = true;
                break;
            }
        }

        if (hasHabitable)
            return UIColors.GreenGlow;

        // Check for special POIs
        foreach (var poi in sys.POIs)
        {
            if (poi.Type == Core.Enums.POIType.DebrisField ||
                poi.Type == Core.Enums.POIType.AbandonedStation)
                return new Color("#9944dd");
        }

        return new Color(UIColors.Accent, 0.5f); // neutral/uncharted cyan
    }
}
