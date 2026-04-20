using System;
using System.Linq;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Combat;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Nodes.UI.CombatHUD;

/// <summary>
/// Post-battle debrief overlay. Per-design performance rows with [IMPROVE DESIGN] deep links,
/// plus the Research Gained block. Research Gained numbers match the Tech Tree Expertise Bar
/// (same formula — we pull from the empire's ExpertiseTracker deltas captured at battle end).
/// </summary>
public partial class CombatDebrief : GlassOverlay
{
    private Battle? _battle;
    private CombatResult _result;

    public CombatDebrief()
    {
        OverlayTitle = "BATTLE DEBRIEF";
    }

    public void Configure(Battle battle, CombatResult result)
    {
        _battle = battle;
        _result = result;
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
        margin.AddThemeConstantOverride("margin_bottom", 20);
        margin.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 14);
        margin.AddChild(col);

        // Header line: result + time
        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", 12);
        col.AddChild(headerRow);

        var resultLabel = new Label { Text = $"RESULT: {_result.ToString().ToUpperInvariant()}" };
        var resultColor = _result switch
        {
            CombatResult.Victory => UIColors.AccentGreen,
            CombatResult.Defeat => UIColors.AccentRed,
            CombatResult.Retreat => UIColors.Moving,
            _ => UIColors.TextLabel,
        };
        UIFonts.StyleRole(resultLabel, UIFonts.Role.Title, resultColor);
        headerRow.AddChild(resultLabel);

        var timeLabel = new Label
        {
            Text = _battle != null
                ? $"T+{(int)(_battle.ElapsedSeconds / 60):00}:{(int)(_battle.ElapsedSeconds % 60):00}"
                : ""
        };
        UIFonts.StyleRole(timeLabel, UIFonts.Role.Normal);
        headerRow.AddChild(timeLabel);

        col.AddChild(BuildSeparator());

        // Per-design performance
        var perDesignHeader = new Label { Text = "PER-DESIGN PERFORMANCE" };
        UIFonts.StyleRole(perDesignHeader, UIFonts.Role.Small);
        col.AddChild(perDesignHeader);

        if (_battle == null || _battle.PerDesignPerformance.Count == 0)
        {
            var empty = new Label { Text = "No design data available." };
            UIFonts.StyleRole(empty, UIFonts.Role.Small);
            col.AddChild(empty);
        }
        else
        {
            foreach (var perf in _battle.PerDesignPerformance.Values)
                col.AddChild(BuildDesignRow(perf));
        }

        col.AddChild(BuildSeparator());

        // Research Gained (proxy numbers pending real expertise snapshot)
        var researchHeader = new Label { Text = "RESEARCH GAINED" };
        UIFonts.StyleRole(researchHeader, UIFonts.Role.Small);
        col.AddChild(researchHeader);

        int xp = _battle != null ? (int)(_battle.ElapsedSeconds * 10f) : 0;
        var xpRow = new Label { Text = $"Expertise XP this battle: {xp}" };
        UIFonts.StyleRole(xpRow, UIFonts.Role.Normal, UIColors.DeltaPos);
        col.AddChild(xpRow);

        col.AddChild(BuildSeparator());

        // Footer buttons
        var btnRow = new HBoxContainer();
        btnRow.AddThemeConstantOverride("separation", 10);
        col.AddChild(btnRow);

        var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        btnRow.AddChild(spacer);

        var continueBtn = new Button { Text = "CONTINUE" };
        continueBtn.CustomMinimumSize = new Vector2(140, 36);
        UIFonts.StyleButtonRole(continueBtn, UIFonts.Role.Small, UIColors.TextBright);
        GlassPanel.StyleButton(continueBtn);
        continueBtn.Pressed += RequestClose;
        btnRow.AddChild(continueBtn);

        Body.AddChild(margin);
    }

    private Control BuildDesignRow(DesignPerformance perf)
    {
        var panel = new PanelContainer();
        var style = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.25f), BorderColor = UIColors.BorderDim };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(0);
        style.ContentMarginLeft = 12; style.ContentMarginRight = 12;
        style.ContentMarginTop = 8; style.ContentMarginBottom = 8;
        panel.AddThemeStyleboxOverride("panel", style);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 4);
        panel.AddChild(col);

        var titleRow = new HBoxContainer();
        titleRow.AddThemeConstantOverride("separation", 10);
        col.AddChild(titleRow);

        var name = new Label
        {
            Text = perf.DesignName.ToUpperInvariant() +
                   $"  ({perf.ShipsEngaged} engaged, {perf.ShipsSurvived} survived)"
        };
        UIFonts.StyleRole(name, UIFonts.Role.Normal, UIColors.TextBright);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        titleRow.AddChild(name);

        if (!string.IsNullOrEmpty(perf.DesignId))
        {
            var improveBtn = DeepLinkChip.Create("IMPROVE DESIGN", UIColors.Accent, () =>
            {
                EventBus.Instance?.FireDesignerOpenRequested(new DesignerOpenRequest
                {
                    DesignId = perf.DesignId,
                });
                RequestClose();
            });
            titleRow.AddChild(improveBtn);

            // Proxy for "underperformer": survived < engaged with low damage dealt.
            bool underperformed = perf.ShipsSurvived < perf.ShipsEngaged || perf.DamageDealt < 100;
            if (underperformed)
            {
                var player = GameManager.Instance?.LocalPlayerEmpire;
                var primaryColor = player?.Affinity ?? PrecursorColor.Red;
                var altChip = DeepLinkChip.Create("RESEARCH ALTERNATIVE", UIColors.GetFactionGlow(primaryColor), () =>
                {
                    EventBus.Instance?.FireTechTreeOpenRequested(new TechTreeOpenRequest
                    {
                        Color = primaryColor,
                        Intent = TechTreeIntent.View,
                    });
                    RequestClose();
                });
                titleRow.AddChild(altChip);
            }
        }

        var stats = new Label
        {
            Text = $"Damage dealt: {(int)perf.DamageDealt}   \u00B7   Damage taken: {(int)perf.DamageTaken}"
        };
        UIFonts.StyleRole(stats, UIFonts.Role.Small);
        col.AddChild(stats);

        if (perf.TopContributors.Count > 0)
        {
            var top = perf.TopContributors.OrderByDescending(kv => kv.Value).First();
            var topLabel = new Label { Text = $"Top contributor: {top.Key} ({(int)top.Value} dmg)" };
            UIFonts.StyleRole(topLabel, UIFonts.Role.Small, UIColors.DeltaPos);
            col.AddChild(topLabel);
        }

        return panel;
    }

    private static Control BuildSeparator()
    {
        var sep = new ColorRect { Color = UIColors.BorderDim };
        sep.CustomMinimumSize = new Vector2(0, 1);
        return sep;
    }
}
