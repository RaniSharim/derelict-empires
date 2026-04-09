using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Tooltip that appears when hovering over a star system on the galaxy map.
/// Shows system name, arm, POI count, and dominant color.
/// </summary>
public partial class SystemTooltip : PanelContainer
{
    private Label _nameLabel = null!;
    private Label _infoLabel = null!;

    public override void _Ready()
    {
        // Build UI
        var vbox = new VBoxContainer();
        AddChild(vbox);

        _nameLabel = new Label();
        _nameLabel.AddThemeFontSizeOverride("font_size", 16);
        vbox.AddChild(_nameLabel);

        _infoLabel = new Label();
        _infoLabel.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(_infoLabel);

        Visible = false;
        MouseFilter = Control.MouseFilterEnum.Ignore;

        // Subscribe to events
        EventBus.Instance.SystemHovered += OnSystemHovered;
        EventBus.Instance.SystemUnhovered += OnSystemUnhovered;
    }

    public override void _Process(double delta)
    {
        if (Visible)
        {
            // Follow mouse with offset
            var mousePos = GetViewport().GetMousePosition();
            Position = mousePos + new Vector2(15, 15);
        }
    }

    private void OnSystemHovered(StarSystemData system)
    {
        _nameLabel.Text = system.Name;

        string location = system.IsCore ? "Core" : $"Arm {system.ArmIndex + 1}";
        string color = system.DominantColor?.ToString() ?? "Mixed";
        _infoLabel.Text = $"{location} | {color} | {system.POIs.Count} POIs";

        Visible = true;
    }

    private void OnSystemUnhovered()
    {
        Visible = false;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.SystemHovered -= OnSystemHovered;
            EventBus.Instance.SystemUnhovered -= OnSystemUnhovered;
        }
    }
}
