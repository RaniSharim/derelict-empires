using Godot;
using System.Collections.Generic;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Top HUD bar: logo | money+food | research strip | 5 faction resource boxes | exit.
/// All layout authored in <c>scenes/ui/top_bar.tscn</c>; this script binds the glass
/// panel, drives label text, instances faction boxes, and wires the exit button.
/// </summary>
public partial class TopBar : Control
{
    public const int BarHeight = 130;

    [Export] private PanelContainer _background = null!;
    [Export] private Label _title = null!;
    [Export] private Label _subtitle = null!;
    [Export] private Label _moneyAmount = null!;
    [Export] private Label _moneyDelta = null!;
    [Export] private Label _foodAmount = null!;
    [Export] private Label _foodDelta = null!;
    [Export] private ResearchStrip _researchStrip = null!;
    [Export] private HBoxContainer _factionBoxes = null!;
    [Export] private Button _exitButton = null!;

    private readonly Dictionary<PrecursorColor, FactionResourceBox> _factionBoxByColor = new();

    private static readonly PackedScene FactionBoxScene =
        GD.Load<PackedScene>("res://scenes/ui/faction_resource_box.tscn");

    /// <summary>Expose the research strip so MainScene can wire it to its research state.</summary>
    public ResearchStrip ResearchStrip => _researchStrip;

    public override void _Ready()
    {
        GlassPanel.Apply(_background, enableBlur: true);

        UIFonts.Style(_title,       UIFonts.Title, UIFonts.TitleSize, UIColors.TextBright);
        UIFonts.Style(_subtitle,    UIFonts.Main,  UIFonts.SmallSize, UIColors.TextFaint);
        UIFonts.Style(_moneyAmount, UIFonts.Main,  UIFonts.NormalSize, Colors.White);
        UIFonts.Style(_moneyDelta,  UIFonts.Main,  UIFonts.SmallSize,  UIColors.DeltaPosBright);
        UIFonts.Style(_foodAmount,  UIFonts.Main,  UIFonts.NormalSize, Colors.White);
        UIFonts.Style(_foodDelta,   UIFonts.Main,  UIFonts.SmallSize,  UIColors.DeltaPosBright);

        var factions = new[] { PrecursorColor.Red, PrecursorColor.Blue,
                               PrecursorColor.Green, PrecursorColor.Gold,
                               PrecursorColor.Purple };
        foreach (var color in factions)
        {
            var box = FactionBoxScene.Instantiate<FactionResourceBox>();
            box.Name = $"Faction_{color}";
            _factionBoxes.AddChild(box);
            box.Populate(color);
            _factionBoxByColor[color] = box;
        }

        _exitButton.Pressed += () => GetTree().Quit();
    }

    private long _lastCredits = long.MinValue;
    private string? _lastSubtitleSig;

    public override void _Process(double delta)
    {
        var empire = GameManager.Instance?.LocalPlayerEmpire;
        if (empire == null) return;

        if (empire.Credits != _lastCredits)
        {
            _lastCredits = (long)empire.Credits;
            _moneyAmount.Text = empire.Credits.ToString("N0");
        }

        // Subtitle (Affinity/Origin/Name) changes only on empire init/rename — skip allocations otherwise.
        var sig = empire.Name;
        if (!ReferenceEquals(sig, _lastSubtitleSig))
        {
            _lastSubtitleSig = sig;
            _subtitle.Text = $"{empire.Affinity}, {empire.Origin}, {empire.Name}";
        }
    }

    /// <summary>Push income data to all faction boxes.</summary>
    public void UpdateIncome(Dictionary<string, float> income)
    {
        foreach (var box in _factionBoxByColor.Values)
            box.UpdateIncome(income);
    }
}
