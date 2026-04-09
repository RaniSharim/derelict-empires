using Godot;
using System;
using System.Collections.Generic;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Expandable panel showing all 20 resources and 10 components.
/// Toggle with a hotkey or button.
/// </summary>
public partial class ResourcePanel : PanelContainer
{
    private VBoxContainer _content = null!;
    private readonly Dictionary<string, Label> _resourceLabels = new();
    private readonly Dictionary<string, Label> _componentLabels = new();

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(300, 0);
        AnchorsPreset = (int)LayoutPreset.CenterRight;
        GrowHorizontal = GrowDirection.Begin;

        var scroll = new ScrollContainer();
        scroll.CustomMinimumSize = new Vector2(300, 400);
        AddChild(scroll);

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 2);
        scroll.AddChild(_content);

        // Title
        var title = new Label { Text = "RESOURCES" };
        title.AddThemeFontSizeOverride("font_size", 16);
        _content.AddChild(title);
        _content.AddChild(new HSeparator());

        // Resources grouped by color
        foreach (var color in Enum.GetValues<PrecursorColor>())
        {
            var colorLabel = new Label { Text = $"--- {color} ---" };
            colorLabel.AddThemeFontSizeOverride("font_size", 12);
            _content.AddChild(colorLabel);

            foreach (var type in Enum.GetValues<ResourceType>())
            {
                var def = ResourceDefinition.Find(color, type);
                if (def == null) continue;

                var label = new Label { Text = $"  {def.DisplayName}: 0" };
                label.AddThemeFontSizeOverride("font_size", 11);
                _content.AddChild(label);
                _resourceLabels[def.Id] = label;
            }
        }

        _content.AddChild(new HSeparator());
        var compTitle = new Label { Text = "COMPONENTS" };
        compTitle.AddThemeFontSizeOverride("font_size", 14);
        _content.AddChild(compTitle);

        foreach (var color in Enum.GetValues<PrecursorColor>())
        foreach (var tier in Enum.GetValues<ComponentTier>())
        {
            var def = ComponentDefinition.Find(color, tier);
            if (def == null) continue;

            var label = new Label { Text = $"  {def.DisplayName}: 0" };
            label.AddThemeFontSizeOverride("font_size", 11);
            _content.AddChild(label);
            _componentLabels[def.Id] = label;
        }

        Visible = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.R && !key.CtrlPressed)
        {
            Visible = !Visible;
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;

        var empire = GameManager.Instance?.LocalPlayerEmpire;
        if (empire == null) return;

        foreach (var def in ResourceDefinition.All)
        {
            float amount = empire.GetResource(def.Color, def.Type);
            if (_resourceLabels.TryGetValue(def.Id, out var label))
                label.Text = $"  {def.DisplayName}: {amount:F1}";
        }

        foreach (var def in ComponentDefinition.All)
        {
            float amount = empire.GetComponent(def.Color, def.Tier);
            if (_componentLabels.TryGetValue(def.Id, out var label))
                label.Text = $"  {def.DisplayName}: {amount:F1}";
        }
    }
}
