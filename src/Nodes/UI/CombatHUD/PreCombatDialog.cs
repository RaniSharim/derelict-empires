using System;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.UI.CombatHUD;

/// <summary>
/// Centered glass modal shown when combat is triggered. Summarizes both sides,
/// gates hostile info by scan state (MVP: always "No scan"), and offers
/// [ ENGAGE ] / [ RETREAT ] / [ OPEN HUD ]. The galaxy is paused behind it.
/// </summary>
public partial class PreCombatDialog : GlassOverlay
{
    /// <summary>Raised when the player commits to combat. Owner routes to HUD + starts sim.</summary>
    public event Action? Engaged;

    /// <summary>Raised when the player declines combat. Owner clears pause and dismisses.</summary>
    public event Action? Retreated;

    private FleetData? _own;
    private FleetData? _hostile;
    private int _ownCount;
    private int _hostileCount;
    private string _systemName = "";

    public PreCombatDialog()
    {
        OverlayTitle = "ENGAGEMENT IMMINENT";
    }

    public void Configure(FleetData own, int ownShipCount, FleetData hostile, int hostileShipCount, string systemName)
    {
        _own = own;
        _hostile = hostile;
        _ownCount = ownShipCount;
        _hostileCount = hostileShipCount;
        _systemName = systemName;
    }

    public override void _Ready()
    {
        base._Ready();
        BuildBody();
    }

    private void BuildBody()
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 32);
        margin.AddThemeConstantOverride("margin_right", 32);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 24);
        margin.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 14);
        margin.AddChild(col);

        var system = new Label { Text = $"LOCATION · {_systemName.ToUpperInvariant()}" };
        UIFonts.StyleRole(system, UIFonts.Role.Small, UIColors.TextLabel);
        col.AddChild(system);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 24);
        col.AddChild(row);

        row.AddChild(BuildSideColumn("YOUR FORCES", _own?.Name ?? "Unknown", _ownCount, UIColors.Accent));
        row.AddChild(BuildSideColumn("HOSTILE FORCES", _hostile?.Name ?? "Unknown Fleet", _hostileCount, UIColors.AccentRed));

        // Composition estimate — MVP: "no scan"
        var estimate = new Label
        {
            Text = $"COMPOSITION ESTIMATE\nBased on coarse scan:\n  Est. {_hostileCount} hostiles · mixed size classes"
        };
        UIFonts.StyleRole(estimate, UIFonts.Role.Small);
        col.AddChild(estimate);

        var odds = new Label { Text = "ODDS (estimate): Even" };
        UIFonts.StyleRole(odds, UIFonts.Role.Normal, UIColors.Moving);
        col.AddChild(odds);

        col.AddChild(BuildSeparator());

        var btnRow = new HBoxContainer();
        btnRow.AddThemeConstantOverride("separation", 10);
        col.AddChild(btnRow);

        btnRow.AddChild(MakeButton("ENGAGE", UIColors.AccentRed, () =>
        {
            Engaged?.Invoke();
            base.RequestClose();
        }));
        btnRow.AddChild(MakeButton("RETREAT", UIColors.TextLabel, () =>
        {
            Retreated?.Invoke();
            base.RequestClose();
        }));
        btnRow.AddChild(MakeButton("OPEN HUD", UIColors.Accent, () =>
        {
            Engaged?.Invoke();
            base.RequestClose();
        }));

        Body.AddChild(margin);
    }

    private static Control BuildSideColumn(string header, string fleetName, int shipCount, Color accent)
    {
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 6);
        col.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var h = new Label { Text = header };
        UIFonts.StyleRole(h, UIFonts.Role.Small, accent);
        col.AddChild(h);

        var name = new Label { Text = fleetName.ToUpperInvariant() };
        UIFonts.StyleRole(name, UIFonts.Role.Title);
        col.AddChild(name);

        var count = new Label { Text = $"\u25AE  {shipCount} ship{(shipCount == 1 ? "" : "s")}" };
        UIFonts.StyleRole(count, UIFonts.Role.Normal);
        col.AddChild(count);

        return col;
    }

    private static Control BuildSeparator()
    {
        var sep = new ColorRect { Color = UIColors.BorderDim };
        sep.CustomMinimumSize = new Vector2(0, 1);
        return sep;
    }

    private static Button MakeButton(string text, Color accent, Action onPressed)
    {
        var btn = new Button { Text = text };
        btn.CustomMinimumSize = new Vector2(0, 40);
        btn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        UIFonts.StyleButtonRole(btn, UIFonts.Role.Small, UIColors.TextBright);

        var normal = new StyleBoxFlat
        {
            BgColor = new Color(accent.R, accent.G, accent.B, 0.20f),
            BorderColor = accent,
        };
        normal.SetBorderWidthAll(1);
        normal.SetCornerRadiusAll(2);
        var hover = new StyleBoxFlat
        {
            BgColor = new Color(accent.R, accent.G, accent.B, 0.35f),
            BorderColor = accent,
        };
        hover.SetBorderWidthAll(1);
        hover.SetCornerRadiusAll(2);

        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeStyleboxOverride("focus", normal);
        btn.Pressed += () => onPressed();
        return btn;
    }
}
