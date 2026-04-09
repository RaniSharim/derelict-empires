using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Settlements;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Panel showing colony details: population, allocation, buildings, queue, happiness.
/// Appears when the player's home colony system is selected.
/// </summary>
public partial class ColonyPanel : PanelContainer
{
    private Label _nameLabel = null!;
    private Label _popLabel = null!;
    private Label _happinessLabel = null!;
    private Label _outputLabel = null!;
    private Label _buildingsLabel = null!;
    private Label _queueLabel = null!;
    private OptionButton _priorityPicker = null!;

    private Colony? _colony;
    private Action<ColonyPriority>? _onPriorityChanged;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(280, 0);
        AnchorsPreset = (int)LayoutPreset.BottomRight;
        GrowHorizontal = GrowDirection.Begin;
        GrowVertical = GrowDirection.Begin;
        OffsetBottom = -10;
        OffsetRight = -10;

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        AddChild(vbox);

        _nameLabel = new Label { Text = "COLONY" };
        _nameLabel.AddThemeFontSizeOverride("font_size", 16);
        vbox.AddChild(_nameLabel);

        vbox.AddChild(new HSeparator());

        _popLabel = new Label();
        _popLabel.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(_popLabel);

        _happinessLabel = new Label();
        _happinessLabel.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(_happinessLabel);

        _outputLabel = new Label();
        _outputLabel.AddThemeFontSizeOverride("font_size", 11);
        _outputLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(_outputLabel);

        // Priority picker
        var prioBox = new HBoxContainer();
        prioBox.AddChild(new Label { Text = "Priority: " });
        _priorityPicker = new OptionButton();
        _priorityPicker.AddItem("Balanced");
        _priorityPicker.AddItem("Production");
        _priorityPicker.AddItem("Research");
        _priorityPicker.AddItem("Growth");
        _priorityPicker.AddItem("Mining");
        _priorityPicker.ItemSelected += OnPrioritySelected;
        prioBox.AddChild(_priorityPicker);
        vbox.AddChild(prioBox);

        vbox.AddChild(new HSeparator());

        _buildingsLabel = new Label { Text = "Buildings: none" };
        _buildingsLabel.AddThemeFontSizeOverride("font_size", 11);
        _buildingsLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(_buildingsLabel);

        _queueLabel = new Label { Text = "Queue: empty" };
        _queueLabel.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(_queueLabel);

        Visible = false;
    }

    public void Show(Colony colony, Action<ColonyPriority>? onPriorityChanged = null)
    {
        _colony = colony;
        _onPriorityChanged = onPriorityChanged;
        _priorityPicker.Selected = (int)colony.Priority;
        Visible = true;
    }

    public void Hide()
    {
        _colony = null;
        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (!Visible || _colony == null) return;

        _nameLabel.Text = _colony.Name;
        _popLabel.Text = $"Pop: {_colony.TotalPopulation}/{_colony.PopCap} | Food surplus: {_colony.FoodSurplus:F1}";
        _happinessLabel.Text = $"Happiness: {_colony.Happiness:F0}% (mod: {_colony.HappinessModifier:F2}x)";

        _outputLabel.Text = $"Output — Prod: {_colony.EffectiveProductionOutput:F1}" +
            $" | Res: {_colony.EffectiveResearchOutput:F1}" +
            $" | Food: {_colony.EffectiveFoodOutput:F1}" +
            $" | Mine: {_colony.EffectiveMiningOutput:F1}";

        if (_colony.Buildings.Count > 0)
        {
            var names = _colony.Buildings.Select(id => BuildingData.FindById(id)?.DisplayName ?? id);
            _buildingsLabel.Text = "Buildings: " + string.Join(", ", names);
        }
        else
        {
            _buildingsLabel.Text = "Buildings: none";
        }

        if (!_colony.Queue.IsEmpty)
        {
            var current = _colony.Queue.Current!;
            _queueLabel.Text = $"Building: {current.Item.DisplayName} ({current.Progress * 100:F0}%) [{_colony.Queue.Count} in queue]";
        }
        else
        {
            _queueLabel.Text = "Queue: empty";
        }
    }

    private void OnPrioritySelected(long index)
    {
        if (_colony == null) return;
        var priority = (ColonyPriority)(int)index;
        _colony.Priority = priority;
        PopAllocationManager.AutoAllocate(_colony);
        _onPriorityChanged?.Invoke(priority);
    }
}
