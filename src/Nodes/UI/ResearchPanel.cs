using Godot;
using System.Linq;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Minimal research panel showing current project, progress, and available subsystems.
/// </summary>
public partial class ResearchPanel : PanelContainer
{
    private Label _titleLabel = null!;
    private Label _currentLabel = null!;
    private Label _progressLabel = null!;
    private Label _availableLabel = null!;
    private Label _completedLabel = null!;

    private EmpireResearchState? _state;
    private TechTreeRegistry? _registry;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(260, 0);
        AnchorsPreset = (int)LayoutPreset.TopLeft;
        OffsetTop = 80;
        OffsetLeft = 10;

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 3);
        AddChild(vbox);

        _titleLabel = new Label { Text = "RESEARCH" };
        _titleLabel.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(_titleLabel);
        vbox.AddChild(new HSeparator());

        _currentLabel = new Label { Text = "Project: none" };
        _currentLabel.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(_currentLabel);

        _progressLabel = new Label { Text = "" };
        _progressLabel.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(_progressLabel);

        vbox.AddChild(new HSeparator());

        _completedLabel = new Label { Text = "Researched: 0" };
        _completedLabel.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(_completedLabel);

        _availableLabel = new Label { Text = "Available: 0" };
        _availableLabel.AddThemeFontSizeOverride("font_size", 11);
        _availableLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(_availableLabel);

        Visible = false;
    }

    public void SetState(EmpireResearchState state, TechTreeRegistry registry)
    {
        _state = state;
        _registry = registry;
        Visible = true;
    }

    public override void _Process(double delta)
    {
        if (!Visible || _state == null || _registry == null) return;

        if (_state.CurrentProject != null)
        {
            var sub = _registry.GetSubsystem(_state.CurrentProject);
            string name = sub?.DisplayName ?? _state.CurrentProject;
            _currentLabel.Text = $"Researching: {name}";

            float cost = sub?.ResearchCost ?? 20;
            float pct = (_state.CurrentProgress / cost) * 100f;
            _progressLabel.Text = $"Progress: {pct:F0}% ({_state.CurrentProgress:F0}/{cost:F0})";
        }
        else
        {
            _currentLabel.Text = "Project: idle";
            _progressLabel.Text = "";
        }

        _completedLabel.Text = $"Researched: {_state.ResearchedSubsystems.Count}";
        _availableLabel.Text = $"Available: {_state.AvailableSubsystems.Count} | Queue: {_state.Queue.Count}";
    }
}
