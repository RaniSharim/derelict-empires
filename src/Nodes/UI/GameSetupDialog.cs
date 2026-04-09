using Godot;
using System;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Temporary setup dialog to pick precursor affinity and origin before starting.
/// Emits a signal when the player confirms their choices.
/// </summary>
public partial class GameSetupDialog : PanelContainer
{
    [Signal]
    public delegate void SetupConfirmedEventHandler(int colorIndex, int originIndex);

    private OptionButton _colorPicker = null!;
    private OptionButton _originPicker = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(400, 300);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        AddChild(vbox);

        // Title
        var title = new Label { Text = "DERELICT EMPIRES" };
        title.AddThemeFontSizeOverride("font_size", 24);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        var subtitle = new Label { Text = "Choose your empire's origins" };
        subtitle.AddThemeFontSizeOverride("font_size", 14);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(subtitle);

        vbox.AddChild(new HSeparator());

        // Precursor Affinity
        vbox.AddChild(new Label { Text = "Precursor Affinity:" });
        _colorPicker = new OptionButton();
        _colorPicker.AddItem("Red — Crimson Forge (Weapons, Industry)");
        _colorPicker.AddItem("Blue — Azure Lattice (Info, Espionage)");
        _colorPicker.AddItem("Green — Verdant Synthesis (Biotech, Terraforming)");
        _colorPicker.AddItem("Gold — Golden Ascendancy (Trade, Logistics)");
        _colorPicker.AddItem("Purple — Obsidian Covenant (Exotic Tech)");
        vbox.AddChild(_colorPicker);

        // Origin
        vbox.AddChild(new Label { Text = "Origin:" });
        _originPicker = new OptionButton();
        _originPicker.AddItem("Warriors — Combat bonuses, extra Fighter");
        _originPicker.AddItem("Servitors — Research speed +10%, extra Salvager");
        _originPicker.AddItem("Haulers — See hidden lanes, extra Scout");
        _originPicker.AddItem("Chroniclers — Color research +15%, extra Scout");
        _originPicker.AddItem("Free Race — No affinity, balanced, extra Builder");
        vbox.AddChild(_originPicker);

        vbox.AddChild(new HSeparator());

        // Start button
        var startBtn = new Button { Text = "Start Game" };
        startBtn.CustomMinimumSize = new Vector2(0, 40);
        startBtn.Pressed += OnStartPressed;
        vbox.AddChild(startBtn);

        // Center on screen
        AnchorsPreset = (int)LayoutPreset.Center;
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;
    }

    private void OnStartPressed()
    {
        EmitSignal(SignalName.SetupConfirmed, _colorPicker.Selected, _originPicker.Selected);
    }

    public PrecursorColor GetSelectedColor() =>
        (PrecursorColor)_colorPicker.Selected;

    public Origin GetSelectedOrigin() =>
        (Origin)_originPicker.Selected;
}
