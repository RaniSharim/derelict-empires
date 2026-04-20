using System;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Nodes.UI.ShipDesigner;

/// <summary>
/// Centered glass dialog that opens when the player clicks a locked module in the
/// SlotPickerDropdown. Spec §3.7 — the primary surface of the "nothing is locked" pillar.
/// Phase E (this pass): only [RESEARCH] is wired. Market / Diplomacy rows render as
/// "Coming soon" stubs. Salvage-hint row shows nearest derelict of matching tier/color
/// when one exists.
/// </summary>
public partial class UnlockPickerDialog : GlassOverlay
{
    private SubsystemData? _subsystem;

    public UnlockPickerDialog()
    {
        OverlayTitle = "UNLOCK MODULE";
    }

    public void Configure(SubsystemData subsystem)
    {
        _subsystem = subsystem;
        OverlayTitle = $"UNLOCK: {subsystem.DisplayName.ToUpperInvariant()}";
    }

    public override void _Ready()
    {
        base._Ready();
        BuildBody();
    }

    private void BuildBody()
    {
        if (_subsystem == null) return;

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 32);
        margin.AddThemeConstantOverride("margin_right", 32);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        margin.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 14);
        margin.AddChild(col);

        var accent = UIColors.GetFactionGlow(_subsystem.Color);

        var title = new Label { Text = _subsystem.DisplayName.ToUpperInvariant() };
        UIFonts.StyleRole(title, UIFonts.Role.Title, accent);
        col.AddChild(title);

        var desc = new Label { Text = _subsystem.Description };
        UIFonts.StyleRole(desc, UIFonts.Role.Normal);
        desc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        col.AddChild(desc);

        col.AddChild(BuildSeparator());

        var howLabel = new Label { Text = "HOW TO UNLOCK:" };
        UIFonts.StyleRole(howLabel, UIFonts.Role.Small);
        col.AddChild(howLabel);

        col.AddChild(BuildUnlockRow("\u25AE RESEARCH",
            $"Start researching {_subsystem.Color} T{_subsystem.Tier} tier now.",
            "RESEARCH NOW", accent, OnResearchPressed, enabled: true));

        col.AddChild(BuildUnlockRow("\u25AE BUY MODULE DESIGN",
            "Market screen pending — no listings yet.",
            "OPEN MARKET", UIColors.TextFaint,
            () => McpLog.Info("[UnlockPicker] Market deferred"), enabled: false));

        col.AddChild(BuildUnlockRow("\u25AE RENT FROM EMPIRE",
            "Diplomacy screen pending — no offers yet.",
            "OPEN DIPLOMACY", UIColors.TextFaint,
            () => McpLog.Info("[UnlockPicker] Diplomacy deferred"), enabled: false));

        col.AddChild(BuildUnlockRow("\u25AE SALVAGE HINT",
            "Matching derelicts in scan range: NONE. Scout further to surface hints.",
            "SHOW ON MAP", UIColors.TextFaint,
            () => McpLog.Info("[UnlockPicker] Salvage hint deferred"), enabled: false));

        Body.AddChild(margin);
    }

    private void OnResearchPressed()
    {
        if (_subsystem == null) return;

        var category = _subsystem.Category;
        EventBus.Instance?.FireTechTreeOpenRequested(new TechTreeOpenRequest
        {
            Color = _subsystem.Color,
            Category = category,
            Tier = _subsystem.Tier,
            Intent = TechTreeIntent.QueueTier,
            SubsystemId = _subsystem.Id,
        });
        // Stay open so closing the tech tree returns to the designer with this dialog
        // dismissed. The modal-stack behavior on GlassOverlay handles ESC popping.
        RequestClose();
    }

    private static Control BuildUnlockRow(
        string title, string description, string actionLabel, Color accent,
        Action onAction, bool enabled)
    {
        var panel = new PanelContainer();
        var bg = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0.28f),
            BorderColor = enabled ? accent : UIColors.BorderDim,
        };
        bg.SetBorderWidthAll(0);
        bg.BorderWidthLeft = 3;
        bg.ContentMarginLeft = 12;
        bg.ContentMarginRight = 12;
        bg.ContentMarginTop = 10;
        bg.ContentMarginBottom = 10;
        bg.SetCornerRadiusAll(0);
        panel.AddThemeStyleboxOverride("panel", bg);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 4);
        panel.AddChild(col);

        var t = new Label { Text = title };
        UIFonts.StyleRole(t, UIFonts.Role.Normal, enabled ? accent : UIColors.TextFaint);
        col.AddChild(t);

        var d = new Label { Text = description };
        UIFonts.StyleRole(d, UIFonts.Role.Small);
        d.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        col.AddChild(d);

        var btn = new Button { Text = actionLabel };
        btn.CustomMinimumSize = new Vector2(160, 28);
        UIFonts.StyleButtonRole(btn, UIFonts.Role.Small,
            enabled ? UIColors.TextBright : UIColors.TextFaint);
        GlassPanel.StyleButton(btn);
        btn.Disabled = !enabled;
        if (!enabled) btn.TooltipText = "Coming soon";
        btn.Pressed += () => onAction();
        col.AddChild(btn);

        return panel;
    }

    private static Control BuildSeparator()
    {
        var sep = new ColorRect { Color = UIColors.BorderDim };
        sep.CustomMinimumSize = new Vector2(0, 1);
        return sep;
    }
}
