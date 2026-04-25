using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Custom-drawn galaxy overview: 1px lane lines + 2px system dots colored by ownership,
/// plus a faint cyan viewport rectangle. Driven by <c>GameManager.Instance.Galaxy</c>;
/// redraws when the galaxy reference changes.
/// </summary>
public partial class MinimapCanvas : Control
{
    private float _minX, _maxX, _minZ, _maxZ;
    private GalaxyData? _galaxy;

    public override void _Process(double delta)
    {
        var galaxy = GameManager.Instance?.Galaxy;
        if (galaxy != _galaxy)
        {
            _galaxy = galaxy;
            if (_galaxy != null) ComputeBounds();
            QueueRedraw();
        }
    }

    private void ComputeBounds()
    {
        if (_galaxy == null) return;
        _minX = float.MaxValue; _maxX = float.MinValue;
        _minZ = float.MaxValue; _maxZ = float.MinValue;
        foreach (var sys in _galaxy.Systems)
        {
            if (sys.PositionX < _minX) _minX = sys.PositionX;
            if (sys.PositionX > _maxX) _maxX = sys.PositionX;
            if (sys.PositionZ < _minZ) _minZ = sys.PositionZ;
            if (sys.PositionZ > _maxZ) _maxZ = sys.PositionZ;
        }
        float padX = (_maxX - _minX) * 0.05f;
        float padZ = (_maxZ - _minZ) * 0.05f;
        _minX -= padX; _maxX += padX;
        _minZ -= padZ; _maxZ += padZ;
    }

    private Vector2 WorldToMinimap(float wx, float wz)
    {
        float rangeX = Mathf.Max(_maxX - _minX, 1f);
        float rangeZ = Mathf.Max(_maxZ - _minZ, 1f);
        return new Vector2((wx - _minX) / rangeX * Size.X, (wz - _minZ) / rangeZ * Size.Y);
    }

    public override void _Draw()
    {
        if (_galaxy == null) return;

        var laneColor = new Color(UIColors.TextFaint, 0.3f);
        foreach (var lane in _galaxy.Lanes)
        {
            var sysA = _galaxy.GetSystem(lane.SystemA);
            var sysB = _galaxy.GetSystem(lane.SystemB);
            if (sysA == null || sysB == null) continue;
            DrawLine(
                WorldToMinimap(sysA.PositionX, sysA.PositionZ),
                WorldToMinimap(sysB.PositionX, sysB.PositionZ),
                laneColor, 1f);
        }

        foreach (var sys in _galaxy.Systems)
            DrawCircle(WorldToMinimap(sys.PositionX, sys.PositionZ), 2f, GetSystemColor(sys));

        var viewColor = new Color(UIColors.Accent, 0.4f);
        float viewSize = 20f;
        var center = Size / 2f;
        var viewRect = new Rect2(center.X - viewSize / 2, center.Y - viewSize / 2, viewSize, viewSize);
        DrawRect(viewRect, new Color(1f, 1f, 1f, 0.05f), true);
        DrawRect(viewRect, viewColor, false, 1f);
    }

    private static Color GetSystemColor(StarSystemData sys)
    {
        foreach (var poi in sys.POIs)
            if (poi.Type == Core.Enums.POIType.HabitablePlanet)
                return UIColors.GreenGlow;

        foreach (var poi in sys.POIs)
            if (poi.Type == Core.Enums.POIType.DebrisField || poi.Type == Core.Enums.POIType.AbandonedStation)
                return new Color("#9944dd");

        return new Color(UIColors.Accent, 0.5f);
    }
}
