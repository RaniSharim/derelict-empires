using Godot;
using System.Linq;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Shows resource deposits at the currently selected star system.
/// Appears as a panel when a system is selected.
/// </summary>
public partial class SystemResourceView : PanelContainer
{
    private VBoxContainer _content = null!;
    private Label _titleLabel = null!;
    private VBoxContainer _depositList = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(280, 0);
        AnchorsPreset = (int)LayoutPreset.BottomLeft;
        GrowVertical = GrowDirection.Begin;
        OffsetBottom = -10;
        OffsetLeft = 10;

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 4);
        AddChild(_content);

        _titleLabel = new Label { Text = "" };
        _titleLabel.AddThemeFontSizeOverride("font_size", 14);
        _content.AddChild(_titleLabel);

        _content.AddChild(new HSeparator());

        _depositList = new VBoxContainer();
        _depositList.AddThemeConstantOverride("separation", 2);
        _content.AddChild(_depositList);

        Visible = false;

        EventBus.Instance.SystemSelected += OnSystemSelected;
        EventBus.Instance.SystemDeselected += OnSystemDeselected;
    }

    private void OnSystemSelected(StarSystemData system)
    {
        _titleLabel.Text = $"{system.Name} — Resources";

        // Clear old content
        foreach (var child in _depositList.GetChildren())
            child.QueueFree();

        var summary = ResourceDistributionHelper.GetSystemResourceSummary(system);

        if (summary.Count == 0)
        {
            var noRes = new Label { Text = "  No resource deposits" };
            noRes.AddThemeFontSizeOverride("font_size", 11);
            _depositList.AddChild(noRes);
        }
        else
        {
            foreach (var (key, (total, remaining, rate)) in summary.OrderBy(kv => kv.Key))
            {
                var def = ResourceDefinition.FindById(key.Replace("_Simple", "_simple_").Replace("_Advanced", "_advanced_"));
                string name = def?.DisplayName ?? key;

                float pct = total > 0 ? remaining / total * 100f : 0f;
                var label = new Label
                {
                    Text = $"  {key}: {remaining:F0}/{total:F0} ({pct:F0}%) @ {rate:F1}/s"
                };
                label.AddThemeFontSizeOverride("font_size", 11);
                _depositList.AddChild(label);
            }
        }

        // Also show POIs
        _depositList.AddChild(new HSeparator());
        var poiHeader = new Label { Text = "Points of Interest:" };
        poiHeader.AddThemeFontSizeOverride("font_size", 12);
        _depositList.AddChild(poiHeader);

        foreach (var poi in system.POIs)
        {
            string terrain = poi.Terrain != Core.Enums.TerrainModifier.None ? $" [{poi.Terrain}]" : "";
            string size = poi.PlanetSize != PlanetSize.None ? $" ({poi.PlanetSize})" : "";
            var poiLabel = new Label
            {
                Text = $"  {poi.Type}: {poi.Name}{size}{terrain}"
            };
            poiLabel.AddThemeFontSizeOverride("font_size", 11);
            _depositList.AddChild(poiLabel);
        }

        Visible = true;
    }

    private void OnSystemDeselected()
    {
        Visible = false;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.SystemSelected -= OnSystemSelected;
            EventBus.Instance.SystemDeselected -= OnSystemDeselected;
        }
    }
}
